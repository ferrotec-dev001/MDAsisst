using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MDAsisst.App.Interop;
using MDAsisst.App.Services;
using MDAsisst.App.ViewModels;
using MDAsisst.Core.Editing;
using MDAsisst.Core.Settings;
using MDAsisst.Core.Snippets;
using MDAsisst.Core.WindowState;
using MDAsisst.Rendering;
using Microsoft.Win32;

namespace MDAsisst.App.Views;

public partial class MainWindow : Window
{
    private readonly App _app = App.Current;
    private readonly MainViewModel _vm;
    private readonly DebounceDispatcher _debounce;
    private readonly AutoVisibilityStateMachine _visibility;
    private readonly DispatcherTimer _visibilityTimer;
    private readonly DispatcherTimer _autoSaveTimer;
    private FlowDocumentMarkdownRenderer _renderer;
    private bool _minimizedIconMode;
    private Rect _expandedPlacement;
    private bool _exiting;

    /// <summary>
    /// Issue #3 対応: ファイルを開く／新規作成時に Editor.Text をプログラムから設定すると
    /// TextChanged が発火し MainViewModel.OnTextChanged が IsDirty=true を立ててしまう
    /// （読み込んだ直後なのに「変更あり」扱いになり、終了時に不要な保存確認が出る）。
    /// この間だけ変更検知を止めることで、実際にユーザーが編集した場合のみ IsDirty を立てる。
    /// </summary>
    private bool _suppressChangeTracking;

    private AppSettings Settings => _app.Settings.Current;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel(Settings, _app.CheatSheet, _app.Documents, _app.Log);
        DataContext = _vm;
        CheatCategoryList.ItemsSource = _vm.Categories;

        _debounce = new DebounceDispatcher(Dispatcher);
        _renderer = CreateRenderer();

        _visibility = new AutoVisibilityStateMachine(Settings.Behavior);
        _visibilityTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _visibilityTimer.Tick += VisibilityTimer_Tick;

        _autoSaveTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
        _autoSaveTimer.Tick += AutoSaveTimer_Tick;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private FlowDocumentMarkdownRenderer CreateRenderer()
    {
        var renderer = new FlowDocumentMarkdownRenderer(MarkdownTheme.FromSettings(Settings.Appearance))
        {
            BaseDirectory = _vm?.BaseDirectory
        };
        renderer.LinkNavigated += OnLinkNavigated;
        return renderer;
    }

    // ---------- 初期化・終了 ----------

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RestorePlacement();
        ApplyAppearance();
        ApplyLayout(Settings.Behavior.LayoutMode);
        WindowEffects.HideFromAltTab(this);
        WindowEffects.ApplyRoundedCorners(this);
        Topmost = Settings.Behavior.Topmost;
        _visibilityTimer.Start();
        ConfigureAutoSave();
        UpdatePreview();
        _ = RunUpdateFlowAsync();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_exiting && Settings.Behavior.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        if (!ConfirmDiscardChanges()) { e.Cancel = true; return; }
        SavePlacement();
        _app.Settings.Save(Settings);
    }

    private void RestorePlacement()
    {
        var w = Settings.Window;
        Left = w.Left; Top = w.Top; Width = w.Width; Height = w.Height;
        WindowEffects.EnsureVisible(this);
        _expandedPlacement = new Rect(Left, Top, Width, Height);
    }

    /// <summary>
    /// Issue #14: <see cref="Window.WindowState"/> が Minimized の間、Left/Top（一部環境では
    /// Width/Height も）は Windows が返す "-32000" 付近の疑似アイコン座標になり、これを
    /// そのまま保存すると次回復帰時にウィンドウ大きさ・位置が壊れる。Minimized 中は必ず
    /// <see cref="Window.RestoreBounds"/>（最小化前の実座標）を使う。
    /// </summary>
    private Rect CurrentBounds()
        => WindowState == System.Windows.WindowState.Minimized
            ? RestoreBounds
            : new Rect(Left, Top, Width, Height);

    private void SavePlacement()
    {
        var p = _minimizedIconMode ? _expandedPlacement : CurrentBounds();
        Settings.Window.Left = p.X;
        Settings.Window.Top = p.Y;
        Settings.Window.Width = p.Width;
        Settings.Window.Height = p.Height;
    }

    /// <summary>アピアランス設定を画面へ反映する（FR-WN-04〜07, 17 / Issue #1 ライブプレビューからも呼ばれる）。</summary>
    public void ApplyAppearance()
    {
        var a = Settings.Appearance;
        WindowEffects.SetOpacity(this, a.Opacity);

        Background = ToBrush(a.WindowColor, Color.FromRgb(0x1E, 0x1E, 0x1E));
        var fg = ToBrush(a.ForegroundColor, Colors.White);
        Foreground = fg;

        // Issue #12: 以前はエディタ／プレビュー領域にしか反映されなかった。
        // タイトルバーとチートシート領域も同じフォント色設定へ追従させる。
        TitleText.Foreground = fg;
        CheatCategoryList.Foreground = fg;

        Editor.Background = Brushes.Transparent;
        Editor.Foreground = fg;
        Editor.CaretBrush = fg;
        Editor.FontFamily = new FontFamily(a.EditorFontFamily);
        Editor.FontSize = a.EditorFontSize;

        Preview.Foreground = fg;
        _renderer = CreateRenderer();
        Topmost = Settings.Behavior.Topmost;
        _visibility.UpdateSettings(Settings.Behavior);
        ApplyLayout(Settings.Behavior.LayoutMode);
        ConfigureAutoSave();
        UpdatePreview();
    }

    private static SolidColorBrush ToBrush(string hex, Color fallback)
    {
        try
        {
            if (ColorConverter.ConvertFromString(hex) is Color c) return new SolidColorBrush(c);
        }
        catch (FormatException) { /* 設定の手編集ミス。既定色で継続する。 */ }
        return new SolidColorBrush(fallback);
    }

    /// <summary>
    /// Issue #8: 左側は「上=エディタ／下=プレビュー」の上下分割に変更し、
    /// 右側のチートシート領域を広く確保する。レイアウト切替は行の高さで行う。
    /// </summary>
    private void ApplyLayout(LayoutMode mode)
    {
        switch (mode)
        {
            case LayoutMode.EditorOnly:
                EditorRow.Height = new GridLength(1, GridUnitType.Star);
                PreviewRow.Height = new GridLength(0);
                EditorPreviewSplitterRow.Height = new GridLength(0);
                break;
            case LayoutMode.PreviewOnly:
                EditorRow.Height = new GridLength(0);
                PreviewRow.Height = new GridLength(1, GridUnitType.Star);
                EditorPreviewSplitterRow.Height = new GridLength(0);
                break;
            default:
                EditorRow.Height = new GridLength(1, GridUnitType.Star);
                PreviewRow.Height = new GridLength(1, GridUnitType.Star);
                EditorPreviewSplitterRow.Height = new GridLength(4);
                break;
        }
    }

    /// <summary>Issue #6: レイアウト切替はあまり使わないため、アイコン1つでモードを巡回させる。</summary>
    private void Layout_Click(object sender, RoutedEventArgs e)
    {
        var next = (LayoutMode)(((int)Settings.Behavior.LayoutMode + 1) % 3);
        Settings.Behavior.LayoutMode = next;
        ApplyLayout(next);
        UpdatePreview();
        LayoutButton.ToolTip = "表示レイアウト: " + next switch
        {
            LayoutMode.EditorOnly => "エディタのみ",
            LayoutMode.PreviewOnly => "プレビューのみ",
            _ => "上下分割"
        };
    }

    private void ConfigureAutoSave()
    {
        _autoSaveTimer.Stop();
        if (!Settings.Behavior.AutoSaveEnabled) return;
        _autoSaveTimer.Interval = TimeSpan.FromSeconds(Math.Max(10, Settings.Behavior.AutoSaveIntervalSeconds));
        _autoSaveTimer.Start();
    }

    // ---------- プレビュー ----------

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressChangeTracking) return;

        _vm.Text = Editor.Text;
        _visibility.NotifyActivity();
        RestoreFromMinimizedIcon();
        _debounce.Debounce(Settings.Behavior.PreviewDebounceMs, UpdatePreview);
        UpdateTitle();
    }

    /// <summary>
    /// ISS-006 / ADR-0005: 5,000行超は自動プレビューを一時停止し手動更新に切替える。
    /// 想定最大文書規模は数百行のため、この閾値は恒久仕様として確定している（ADR-0005追記）。
    /// </summary>
    private const int LargeDocumentLineThreshold = 5000;
    private bool _previewSuspendedForLargeDocument;

    private void UpdatePreview()
    {
        if (Settings.Behavior.LayoutMode == LayoutMode.EditorOnly) return;

        var lineCount = Editor.LineCount;
        if (lineCount > LargeDocumentLineThreshold)
        {
            if (!_previewSuspendedForLargeDocument)
            {
                _previewSuspendedForLargeDocument = true;
                SetStatus($"文書が大きいため（{lineCount}行）自動プレビューを一時停止しました。手動更新: Ctrl+Shift+P");
            }
            return;
        }
        _previewSuspendedForLargeDocument = false;

        _renderer.BaseDirectory = _vm.BaseDirectory;
        Preview.Document = _renderer.Render(Editor.Text);
    }

    /// <summary>大規模文書で自動プレビューを止めた場合に、明示操作で1回だけ再描画する。</summary>
    private void ForceUpdatePreview()
    {
        _renderer.BaseDirectory = _vm.BaseDirectory;
        Preview.Document = _renderer.Render(Editor.Text);
        SetStatus("プレビューを更新しました。");
    }

    private void UpdateTitle()
    {
        TitleText.Text = _vm.Title;
    }

    /// <summary>Issue #7: ステータスバーは廃止し、タイトルのツールチップへ状態を出す。</summary>
    private void SetStatus(string message)
    {
        _vm.StatusMessage = message;
        TitleText.ToolTip = message;
    }

    private void OnLinkNavigated(object? sender, Uri uri)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // 既定ブラウザ未設定・オフラインでもアプリは落とさない（NFR-05）。
            _app.Log.Warn("リンクを開けませんでした。", ex);
        }
    }

    // ---------- 入力支援 ----------

    private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        _visibility.NotifyActivity();

        // IME 変換中は補完・整形に介入しない（FR-IA-05）。
        if (e.Key == Key.ImeProcessed) return;

        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

        if (ctrl && e.Key == Key.B) { ApplyEdit(MarkdownEditingService.ToggleWrap(Editor.Text, Editor.SelectionStart, Editor.SelectionLength, "**")); e.Handled = true; return; }
        if (ctrl && e.Key == Key.I) { ApplyEdit(MarkdownEditingService.ToggleWrap(Editor.Text, Editor.SelectionStart, Editor.SelectionLength, "*")); e.Handled = true; return; }
        if (ctrl && e.Key == Key.K) { InsertSnippetText("[$SEL$0](URL)"); e.Handled = true; return; }
        // Issue #11: 表挿入ボタンをタイトルバーから削除したため、Ctrl+Shift+T をショートカットとして維持する。
        if (ctrl && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && e.Key == Key.T) { InsertSnippetText(MarkdownEditingService.CreateTable(2, 3)); e.Handled = true; return; }
        if (ctrl && e.Key == Key.S) { SaveDocument(saveAs: false); e.Handled = true; return; }
        if (ctrl && e.Key == Key.O) { OpenDocument(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.N) { NewDocument(); e.Handled = true; return; }
        if (ctrl && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && e.Key == Key.P) { ForceUpdatePreview(); e.Handled = true; return; }

        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            var result = MarkdownEditingService.HandleEnter(Editor.Text, Editor.SelectionStart);
            if (result.Handled) { ApplyEdit(result); e.Handled = true; }
        }
    }

    private void ApplyEdit(EditResult result)
    {
        if (!result.Handled) return;
        Editor.Text = result.Text;
        Editor.SelectionStart = Math.Clamp(result.SelectionStart, 0, Editor.Text.Length);
        Editor.SelectionLength = Math.Clamp(result.SelectionLength, 0, Editor.Text.Length - Editor.SelectionStart);
        Editor.Focus();
    }

    private void InsertSnippetText(string insertText)
        => ApplyEdit(MarkdownEditingService.InsertSnippet(Editor.Text, Editor.SelectionStart, Editor.SelectionLength, insertText));

    /// <summary>Issue #5: チートシートは検索前提をやめ、常時表示のアイコンボタンから挿入する。</summary>
    private void CheatButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SnippetItem item }) InsertSnippetText(item.InsertText);
    }

    // Issue #11: Bold/Italic/Link/Table のタイトルバーボタンは削除。
    // 対応するキーボードショートカット (Ctrl+B / Ctrl+I / Ctrl+K / Ctrl+Shift+T) は
    // Editor_PreviewKeyDown に残しているため、機能自体は引き続き利用できる。

    // ---------- ファイル操作 ----------

    private void New_Click(object sender, RoutedEventArgs e) => NewDocument();
    private void Open_Click(object sender, RoutedEventArgs e) => OpenDocument();
    private void Save_Click(object sender, RoutedEventArgs e) => SaveDocument(saveAs: false);

    private void NewDocument()
    {
        if (!ConfirmDiscardChanges()) return;
        _vm.NewDocument();

        _suppressChangeTracking = true;
        Editor.Text = string.Empty;
        _suppressChangeTracking = false;

        UpdateTitle();
        UpdatePreview();
    }

    private void OpenDocument()
    {
        if (!ConfirmDiscardChanges()) return;
        var dialog = new OpenFileDialog
        {
            Filter = "Markdown ファイル (*.md;*.markdown)|*.md;*.markdown|すべてのファイル (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;

        if (_vm.Open(dialog.FileName))
        {
            // Issue #3: 読み込み直後の Editor.Text 同期で IsDirty が誤って立たないようにする。
            _suppressChangeTracking = true;
            Editor.Text = _vm.Text;
            _suppressChangeTracking = false;

            UpdateTitle();
            UpdatePreview();
        }
        else
        {
            MessageBox.Show(this, "ファイルを開けませんでした。ログを確認してください。", "MDAsisst",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool SaveDocument(bool saveAs)
    {
        var path = _vm.FilePath;
        if (saveAs || string.IsNullOrEmpty(path))
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Markdown ファイル (*.md)|*.md|すべてのファイル (*.*)|*.*",
                DefaultExt = ".md",
                FileName = Path.GetFileName(path) ?? "無題.md"
            };
            if (dialog.ShowDialog(this) != true) return false;
            path = dialog.FileName;
        }

        _vm.Text = Editor.Text;
        var ok = _vm.Save(path);
        if (!ok)
            MessageBox.Show(this, "保存に失敗しました。書き込み権限とディスク空き容量を確認してください。",
                "MDAsisst", MessageBoxButton.OK, MessageBoxImage.Warning);
        else
            SetStatus(_vm.StatusMessage);
        UpdateTitle();
        return ok;
    }

    private void AutoSaveTimer_Tick(object? sender, EventArgs e)
    {
        if (!_vm.IsDirty || string.IsNullOrEmpty(_vm.FilePath)) return;
        _vm.Text = Editor.Text;
        _vm.Save();
        UpdateTitle();
    }

    /// <summary>未保存の変更がある場合に確認する（FR-ED-02 / Issue #3 で誤検知を修正済み）。続行してよいなら true。</summary>
    private bool ConfirmDiscardChanges()
    {
        if (!_vm.IsDirty) return true;
        var answer = MessageBox.Show(this, "保存されていない変更があります。保存しますか？", "MDAsisst",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return answer switch
        {
            MessageBoxResult.Yes => SaveDocument(saveAs: false),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    // ---------- 自動表示・最小化 ----------

    private void VisibilityTimer_Tick(object? sender, EventArgs e)
    {
        if (!_visibility.Tick(out var state)) return;
        switch (state)
        {
            case VisibilityState.Minimized: MinimizeToCornerIcon(); break;
            case VisibilityState.Expanded: RestoreFromMinimizedIcon(); break;
        }
    }

    /// <summary>
    /// Issue #9: 画面隅の小アイコン状態は、復帰導線として最低限のクリック領域が確保できれば十分。
    /// 帯状の180x34ではなく、アイコンのみの44x44正方形へ縮小する。
    /// </summary>
    private const double MinimizedIconSize = 44;

    private void MinimizeToCornerIcon()
    {
        // Issue #14: OS タスクバーへ最小化中（WindowState.Minimized）は、そもそも画面に何も
        // 見えていないため角アイコン化する意味がなく、かつ Left/Top が疑似座標になり
        // _expandedPlacement を汚染する。OS 最小化中は角アイコン化しない。
        if (_minimizedIconMode || !IsVisible || WindowState == System.Windows.WindowState.Minimized) return;
        _expandedPlacement = CurrentBounds();
        _minimizedIconMode = true;

        TitleBarGrid.Visibility = Visibility.Collapsed;
        ContentGrid.Visibility = Visibility.Collapsed;
        MinimizedIconOverlay.Visibility = Visibility.Visible;

        Width = MinimizedIconSize; Height = MinimizedIconSize;
        WindowEffects.SnapToCorner(this, Settings.Behavior.MinimizedCorner, Width, Height, margin: 8);
    }

    private void RestoreFromMinimizedIcon()
    {
        if (!_minimizedIconMode) return;
        _minimizedIconMode = false;

        // Issue #14: タスクバーアイコンクリック等で WindowState が Minimized のまま
        // 復帰処理に入ることがある。Left/Top/Width/Height への代入は WindowState が
        // Normal でないと正しく反映されないため、先に戻しておく。
        if (WindowState == System.Windows.WindowState.Minimized)
            WindowState = System.Windows.WindowState.Normal;

        MinimizedIconOverlay.Visibility = Visibility.Collapsed;
        TitleBarGrid.Visibility = Visibility.Visible;
        ContentGrid.Visibility = Visibility.Visible;

        Left = _expandedPlacement.X; Top = _expandedPlacement.Y;
        Width = _expandedPlacement.Width; Height = _expandedPlacement.Height;
        WindowEffects.EnsureVisible(this);
        UpdateTitle();
    }

    private void MinimizedIconOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _visibility.NotifyActivity();
        RestoreFromMinimizedIcon();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        _visibility.NotifyActivity();
        RestoreFromMinimizedIcon();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        _visibility.NotifyActivity();
        RestoreFromMinimizedIcon();
    }

    // ---------- トレイ ----------

    private void HideToTray()
    {
        SavePlacement();
        _visibility.HideToTray();
        Hide();
        TrayIcon.ShowBalloonTip("MDAsisst", "トレイに常駐しました。", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
    }

    private void ShowFromTray()
    {
        _visibility.ShowFromTray();
        Show();
        WindowState = System.Windows.WindowState.Normal;
        RestoreFromMinimizedIcon();
        Activate();
    }

    private void Tray_DoubleClick(object sender, RoutedEventArgs e) => ShowFromTray();
    private void Tray_Show(object sender, RoutedEventArgs e) => ShowFromTray();
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = System.Windows.WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => HideToTray();

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _exiting = true;
        Close();
        Application.Current.Shutdown();
    }

    /// <summary>Issue #1: 設定画面にライブプレビュー用コールバックを渡し、操作の都度即時反映する。</summary>
    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(Settings, _app, ApplyAppearance) { Owner = IsVisible ? this : null };
        if (dialog.ShowDialog() == true)
        {
            _app.Settings.Save(Settings);
        }
        ApplyAppearance();
    }

    // ---------- 更新 ----------

    private async Task RunUpdateFlowAsync()
    {
        try
        {
            var mode = Settings.Update.Mode;
            if (mode == UpdateMode.Disabled) return;                 // 通信を行わない（FR-ST-07）
            if (mode == UpdateMode.Manual) return;                   // 手動確認のみ（FR-ST-06）

            // GitHub API のレート制限を避けるため 1 日 1 回に間引く。
            var last = Settings.Update.LastCheckedUtc;
            if (last is not null && (DateTimeOffset.UtcNow - last.Value).TotalHours < 24) return;

            await Task.Delay(TimeSpan.FromSeconds(60));
            var update = await _app.UpdateService.CheckAsync();
            Settings.Update.LastCheckedUtc = DateTimeOffset.UtcNow;
            _app.Settings.Save(Settings);
            if (update is null) return;

            // ADR-0009: 「自動」はバックグラウンドでの確認のみを意味する。ダウンロード・適用は
            // 必ずユーザーの同意を得てから行い、無人で自動適用しない（インストール先が
            // Program Files のため、適用時は管理者権限の確認（UAC）も表示される）。
            SetStatus($"v{update.Version} が利用可能です。");
            var answer = MessageBox.Show(this,
                $"新しいバージョン v{update.Version} があります。今すぐ更新しますか？\n" +
                "（インストール先が Program Files のため、管理者権限の確認が表示されます）",
                "MDAsisst", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            if (!ConfirmDiscardChanges()) return;

            var progress = new Progress<int>(p => SetStatus($"ダウンロード中... {p}%"));
            if (await _app.UpdateService.DownloadAsync(update, progress))
                _app.UpdateService.ApplyAndRestart(update);
            else
                SetStatus("ダウンロードに失敗しました。ネットワークを確認してください。");
        }
        catch (Exception ex)
        {
            // 更新まわりの例外は決して外へ出さない（NFR-05, FR-ST-08）。
            _app.Log.Warn("自動更新処理で想定外のエラーが発生しました。", ex);
        }
    }
}
