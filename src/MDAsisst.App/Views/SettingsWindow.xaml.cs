using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MDAsisst.App.Services;
using MDAsisst.Core.Settings;

namespace MDAsisst.App.Views;

/// <summary>アピアランス・動作・アップデートの設定画面（FR-WN-04〜13, FR-ST-03〜09）。</summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly App _app;

    public SettingsWindow(AppSettings settings, App app)
    {
        InitializeComponent();
        _settings = settings;
        _app = app;

        var fonts = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
        EditorFontBox.ItemsSource = fonts;
        PreviewFontBox.ItemsSource = fonts;

        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var a = _settings.Appearance;
        OpacitySlider.Value = a.Opacity;
        OpacityValue.Text = a.Opacity.ToString("P0", CultureInfo.CurrentCulture);
        WindowColorBox.Text = a.WindowColor;
        ForegroundColorBox.Text = a.ForegroundColor;
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

    private void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValue is not null)
            OpacityValue.Text = e.NewValue.ToString("P0", CultureInfo.CurrentCulture);
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var a = _settings.Appearance;
        a.Opacity = OpacitySlider.Value;
        a.WindowColor = SettingsValidator.NormalizeColor(WindowColorBox.Text, "#1E1E1E");
        a.ForegroundColor = SettingsValidator.NormalizeColor(ForegroundColorBox.Text, "#FFFFFF");
        a.EditorFontFamily = string.IsNullOrWhiteSpace(EditorFontBox.Text) ? a.EditorFontFamily : EditorFontBox.Text;
        a.EditorFontSize = ParseDouble(EditorFontSizeBox.Text, a.EditorFontSize);
        a.PreviewFontFamily = string.IsNullOrWhiteSpace(PreviewFontBox.Text) ? a.PreviewFontFamily : PreviewFontBox.Text;
        a.PreviewFontSize = ParseDouble(PreviewFontSizeBox.Text, a.PreviewFontSize);
        a.EnableAnimation = AnimationCheck.IsChecked == true;
        a.Theme = ThemePreset.Custom;

        var b = _settings.Behavior;
        b.Topmost = TopmostCheck.IsChecked == true;
        b.MinimizeToTrayOnClose = TrayOnCloseCheck.IsChecked == true;
        b.AutoExpandDelaySeconds = ParseInt(AutoExpandBox.Text, b.AutoExpandDelaySeconds);
        b.AutoMinimizeDelaySeconds = ParseInt(AutoMinimizeBox.Text, b.AutoMinimizeDelaySeconds);
        b.MinimizedCorner = (ScreenCorner)Math.Max(0, CornerBox.SelectedIndex);
        b.PreviewDebounceMs = ParseInt(DebounceBox.Text, b.PreviewDebounceMs);
        b.AutoSaveEnabled = AutoSaveCheck.IsChecked == true;
        b.AutoSaveIntervalSeconds = ParseInt(AutoSaveIntervalBox.Text, b.AutoSaveIntervalSeconds);

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
        DialogResult = true;
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

            if (MessageBox.Show(this, $"新しいバージョン v{result.Version} があります。今すぐ更新しますか？",
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

    private static double ParseDouble(string? text, double fallback)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private static int ParseInt(string? text, int fallback)
        => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
}
