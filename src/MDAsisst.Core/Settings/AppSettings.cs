namespace MDAsisst.Core.Settings;

/// <summary>アプリケーション設定のルート。settings.json と 1:1 で対応する。</summary>
public sealed class AppSettings
{
    /// <summary>設定スキーマの版数。移行処理の判定に使う。</summary>
    public int SchemaVersion { get; set; } = 1;

    public AppearanceSettings Appearance { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
    public UpdateSettings Update { get; set; } = new();
    public WindowPlacement Window { get; set; } = new();

    /// <summary>最近開いたファイル（新しい順、最大 <see cref="MaxRecentFiles"/> 件）。</summary>
    public List<string> RecentFiles { get; set; } = new();

    public const int MaxRecentFiles = 10;

    /// <summary>最近使ったファイル一覧の先頭に追加する（重複除去・件数制限つき）。</summary>
    public void AddRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        RecentFiles.Insert(0, path);
        if (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);
    }

    public AppSettings Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Appearance = Appearance.Clone(),
        Behavior = Behavior.Clone(),
        Update = new UpdateSettings { Mode = Update.Mode, LastCheckedUtc = Update.LastCheckedUtc },
        Window = new WindowPlacement { Left = Window.Left, Top = Window.Top, Width = Window.Width, Height = Window.Height },
        RecentFiles = new List<string>(RecentFiles)
    };
}

/// <summary>見た目に関する設定（FR-WN-04〜07, 17）。</summary>
public sealed class AppearanceSettings
{
    /// <summary>ウィンドウ不透明度（0.2〜1.0）。</summary>
    public double Opacity { get; set; } = 0.85;
    /// <summary>ウィンドウ背景色（#RRGGBB）。</summary>
    public string WindowColor { get; set; } = "#1E1E1E";
    /// <summary>文字色（#RRGGBB）。</summary>
    public string ForegroundColor { get; set; } = "#FFFFFF";
    public string EditorFontFamily { get; set; } = "Consolas";
    public double EditorFontSize { get; set; } = 14.0;
    public string PreviewFontFamily { get; set; } = "Yu Gothic UI";
    public double PreviewFontSize { get; set; } = 14.0;
    public ThemePreset Theme { get; set; } = ThemePreset.Dark;
    /// <summary>フェード等のアニメーションを行うか（FR-WN-14）。</summary>
    public bool EnableAnimation { get; set; } = true;

    public AppearanceSettings Clone() => (AppearanceSettings)MemberwiseClone();
}

/// <summary>挙動に関する設定（FR-WN-08〜13, FR-PV-01, FR-ED-06）。</summary>
public sealed class BehaviorSettings
{
    public bool Topmost { get; set; } = true;
    /// <summary>Windows ログオン時に自動起動する（常駐設定, FR-WN-08）。</summary>
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public LayoutMode LayoutMode { get; set; } = LayoutMode.Split;
    /// <summary>編集検知から自動展開するまでの秒数。0 は即時。</summary>
    public int AutoExpandDelaySeconds { get; set; }
    /// <summary>無操作から自動最小化するまでの秒数。0 は自動最小化しない。</summary>
    public int AutoMinimizeDelaySeconds { get; set; } = 30;
    public ScreenCorner MinimizedCorner { get; set; } = ScreenCorner.BottomRight;
    /// <summary>プレビュー更新のデバウンス時間（ミリ秒, FR-PV-01）。</summary>
    public int PreviewDebounceMs { get; set; } = 250;
    public bool AutoSaveEnabled { get; set; }
    public int AutoSaveIntervalSeconds { get; set; } = 60;

    public BehaviorSettings Clone() => (BehaviorSettings)MemberwiseClone();
}

/// <summary>アップデートに関する設定（FR-ST-04〜09）。</summary>
public sealed class UpdateSettings
{
    public UpdateMode Mode { get; set; } = UpdateMode.Manual;
    /// <summary>最終確認日時（UTC）。GitHub API のレート制限対策で 1 日 1 回に間引くために使う。</summary>
    public DateTimeOffset? LastCheckedUtc { get; set; }
}

/// <summary>ウィンドウ位置・サイズ（FR-WN-15）。</summary>
public sealed class WindowPlacement
{
    public double Left { get; set; } = 120;
    public double Top { get; set; } = 120;
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 600;
}
