using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MDAsisst.App.Services;
using MDAsisst.Core.Settings;

namespace MDAsisst.App.Views;

/// <summary>
/// アピアランス・動作・アップデートの設定画面（FR-WN-04〜13, FR-ST-03〜09）。
/// Issue #1: 変更操作の都度 <see cref="_onLivePreview"/> を呼び、メイン画面へ即時反映する。
/// キャンセル時は開いた時点のスナップショットへ復元する。
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly App _app;
    private readonly Action? _onLivePreview;
    private readonly AppSettings _snapshot;
    private bool _isLoading = true;
    private bool _accepted;

    public SettingsWindow(AppSettings settings, App app, Action? onLivePreview = null)
    {
        InitializeComponent();
        _settings = settings;
        _app = app;
        _onLivePreview = onLivePreview;
        _snapshot = settings.Clone();   // Issue #1: キャンセル時に復元するための開いた時点の状態

        var fonts = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
        EditorFontBox.ItemsSource = fonts;
        PreviewFontBox.ItemsSource = fonts;

        LoadFromSettings();
        _isLoading = false;

        Closed += (_, _) =>
        {
            // OK 以外（キャンセル・×・Escape）で閉じた場合は必ず元の状態へ戻す。
            if (!_accepted) RevertToSnapshot();
        };
    }

    private void LoadFromSettings()
    {
        var a = _settings.Appearance;
        OpacitySlider.Value = a.Opacity;
        OpacityValue.Text = a.Opacity.ToString("P0", CultureInfo.CurrentCulture);
        WindowColorBox.Text = a.WindowColor;
        ForegroundColorBox.Text = a.ForegroundColor;
        UpdateSwatch(WindowColorSwatch, a.WindowColor);
        UpdateSwatch(ForegroundColorSwatch, a.ForegroundColor);
        EditorFontBox.Text = a.EditorFontFamily;
        EditorFontSizeBox.Text = a.EditorFontSize.ToString(CultureInfo.InvariantCulture);
        PreviewFontBox.Text = a.PreviewFontFamily;
        PreviewFontSizeBox.Text = a.PreviewFontSize.ToString(CultureInfo.InvariantCulture);
        AnimationCheck.IsChecked = a.EnableAnimation;

        var b = _settings.Behavior;
        TopmostCheck.IsChecked = b.Topmost;
        StartupCheck.IsChecked = StartupRegistration.IsEnabled(_app.Log);
        TrayOnCloseCheck.IsChecked = b.MinimizeToTrayOnClose;
        AutoExpandBox.Text = b.AutoExpandDelaySeconds.ToString(CultureInfo.InvariantCulture);
        AutoMinimizeBox.Text = b.AutoMinimizeDelaySeconds.ToString(CultureInfo.InvariantCulture);
        CornerBox.SelectedIndex = (int)b.MinimizedCorner;
        DebounceBox.Text = b.PreviewDebounceMs.ToString(CultureInfo.InvariantCulture);
        AutoSaveCheck.IsChecked = b.AutoSaveEnabled;
        AutoSaveIntervalBox.Text = b.AutoSaveIntervalSeconds.ToString(CultureInfo.InvariantCulture);

        UpdateAuto.IsChecked = _settings.Update.Mode == UpdateMode.Auto;
        UpdateManual.IsChecked = _settings.Update.Mode == UpdateMode.Manual;
        UpdateDisabled.IsChecked = _settings.Update.Mode == UpdateMode.Disabled;
        VersionLabel.Text = $"現在のバージョン: v{_app.UpdateService.CurrentVersion}";
    }

    private static void UpdateSwatch(System.Windows.Controls.Border swatch, string hex)
    {
        try
        {
            if (ColorConverter.ConvertFromString(hex) is Color c) { swatch.Background = new SolidColorBrush(c); return; }
        }
        catch (FormatException) { /* 無効な値は既定の外観のまま */ }
        swatch.Background = Brushes.Gray;
    }

    // ---------- Issue #1: ライブプレビュー ----------

    private void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValue is not null)
            OpacityValue.Text = e.NewValue.ToString("P0", CultureInfo.CurrentCulture);
        if (_isLoading) return;

        _settings.Appearance.Opacity = e.NewValue;
        _onLivePreview?.Invoke();
    }

    private void WindowColorBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => ApplyColorLive(WindowColorBox.Text, WindowColorSwatch, isWindowColor: true);

    private void ForegroundColorBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => ApplyColorLive(ForegroundColorBox.Text, ForegroundColorSwatch, isWindowColor: false);

    private void ApplyColorLive(string text, System.Windows.Controls.Border swatch, bool isWindowColor)
    {
        if (_isLoading) return;
        // Issue #2: 不正な途中入力（例: "#12"）ではプレビューを崩さず、確定した値のときだけ反映する。
        if (string.IsNullOrWhiteSpace(text) || text[0] != '#' || text.Length is not (4 or 7 or 9)) return;

        try
        {
            if (ColorConverter.ConvertFromString(text) is not Color c) return;
            swatch.Background = new SolidColorBrush(c);
            if (isWindowColor) _settings.Appearance.WindowColor = text;
            else _settings.Appearance.ForegroundColor = text;
            _onLivePreview?.Invoke();
        }
        catch (FormatException)
        {
            // 入力途中の不正な文字列。確定するまで無視する。
        }
    }

    /// <summary>Issue #2: カラーコード手入力の代わりに Windows 標準の色選択ダイアログを使う。</summary>
    private void PickWindowColor_Click(object sender, RoutedEventArgs e) => PickColor(WindowColorBox, isWindowColor: true);
    private void PickForegroundColor_Click(object sender, RoutedEventArgs e) => PickColor(ForegroundColorBox, isWindowColor: false);

    private void PickColor(System.Windows.Controls.TextBox targetBox, bool isWindowColor)
    {
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
        try
        {
            if (ColorConverter.ConvertFromString(targetBox.Text) is Color current)
                dialog.Color = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B);
        }
        catch (FormatException) { /* 現在値が不正なら既定色から選択させる */ }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        var hex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        targetBox.Text = hex;   // TextChanged 経由でスウォッチ更新とライブプレビューが走る
    }

    private void Font_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        if (!string.IsNullOrWhiteSpace(EditorFontBox.Text)) _settings.Appearance.EditorFontFamily = EditorFontBox.Text;
        if (!string.IsNullOrWhiteSpace(PreviewFontBox.Text)) _settings.Appearance.PreviewFontFamily = PreviewFontBox.Text;
        if (double.TryParse(EditorFontSizeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var es))
            _settings.Appearance.EditorFontSize = es;
        if (double.TryParse(PreviewFontSizeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var ps))
            _settings.Appearance.PreviewFontSize = ps;

        _onLivePreview?.Invoke();
    }

    // ---------- 確定・取消 ----------

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var b = _settings.Behavior;
        b.Topmost = TopmostCheck.IsChecked == true;
        b.MinimizeToTrayOnClose = TrayOnCloseCheck.IsChecked == true;
        b.AutoExpandDelaySeconds = ParseInt(AutoExpandBox.Text, b.AutoExpandDelaySeconds);
        b.AutoMinimizeDelaySeconds = ParseInt(AutoMinimizeBox.Text, b.AutoMinimizeDelaySeconds);
        b.MinimizedCorner = (ScreenCorner)Math.Max(0, CornerBox.SelectedIndex);
        b.PreviewDebounceMs = ParseInt(DebounceBox.Text, b.PreviewDebounceMs);
        b.AutoSaveEnabled = AutoSaveCheck.IsChecked == true;
        b.AutoSaveIntervalSeconds = ParseInt(AutoSaveIntervalBox.Text, b.AutoSaveIntervalSeconds);
        _settings.Appearance.EnableAnimation = AnimationCheck.IsChecked == true;
        _settings.Appearance.Theme = ThemePreset.Custom;

        var startup = StartupCheck.IsChecked == true;
        if (startup != StartupRegistration.IsEnabled(_app.Log))
        {
            if (!StartupRegistration.SetEnabled(startup, _app.Log))
                MessageBox.Show(this, "自動起動の設定変更に失敗しました。", "MDAsisst",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        b.StartWithWindows = StartupRegistration.IsEnabled(_app.Log);

        _settings.Update.Mode = UpdateAuto.IsChecked == true ? UpdateMode.Auto
                              : UpdateDisabled.IsChecked == true ? UpdateMode.Disabled
                              : UpdateMode.Manual;

        SettingsValidator.Normalize(_settings);
        _accepted = true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Closed イベントで RevertToSnapshot が走る（_accepted のまま false）。
        DialogResult = false;
    }

    private void TitleBarClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void RevertToSnapshot()
    {
        _settings.Appearance = _snapshot.Appearance;
        _settings.Behavior = _snapshot.Behavior;
        _settings.Update = _snapshot.Update;
        _onLivePreview?.Invoke();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "すべての設定を既定値に戻します。よろしいですか？", "MDAsisst",
            MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;

        var defaults = new AppSettings();
        _settings.Appearance = defaults.Appearance;
        _settings.Behavior = defaults.Behavior;
        _settings.Update = defaults.Update;
        LoadFromSettings();
        _onLivePreview?.Invoke();
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (UpdateDisabled.IsChecked == true)
        {
            UpdateStatus.Text = "更新方式が「不要」のため確認しません。";
            return;
        }

        CheckUpdateButton.IsEnabled = false;
        UpdateStatus.Text = "更新を確認しています...";
        try
        {
            var result = await _app.UpdateService.CheckAsync();
            if (result is null)
            {
                UpdateStatus.Text = _app.UpdateService.IsInstalled
                    ? "最新版です（またはネットワークに接続できません）。"
                    : "インストーラー経由でインストールされていないため更新できません。";
                return;
            }

            if (MessageBox.Show(this,
                    $"新しいバージョン v{result.Version} があります。今すぐ更新しますか？\n" +
                    "（インストール先が Program Files のため、管理者権限の確認が表示されます）",
                    "MDAsisst", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                UpdateStatus.Text = $"v{result.Version} が利用可能です。";
                return;
            }

            var progress = new Progress<int>(p => UpdateStatus.Text = $"ダウンロード中... {p}%");
            if (await _app.UpdateService.DownloadAsync(result, progress))
                _app.UpdateService.ApplyAndRestart(result);
            else
                UpdateStatus.Text = "ダウンロードに失敗しました。ネットワークを確認してください。";
        }
        catch (Exception ex)
        {
            _app.Log.Warn("手動更新確認でエラーが発生しました。", ex);
            UpdateStatus.Text = "更新の確認に失敗しました。";
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
            _settings.Update.LastCheckedUtc = DateTimeOffset.UtcNow;
        }
    }

    private static int ParseInt(string? text, int fallback)
        => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
}
