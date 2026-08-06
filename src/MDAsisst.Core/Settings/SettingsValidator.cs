namespace MDAsisst.Core.Settings;

/// <summary>
/// 設定値を許容範囲へ丸める。設定ファイルは人手で編集可能（FR-ST-01）なため、
/// 範囲外の値が入り得ることを前提に、読み込み時に必ず通す。
/// </summary>
public static class SettingsValidator
{
    public const double MinOpacity = 0.2;
    public const double MaxOpacity = 1.0;
    public const double MinFontSize = 8.0;
    public const double MaxFontSize = 48.0;
    public const int MinDebounceMs = 0;
    public const int MaxDebounceMs = 2000;
    public const int MaxDelaySeconds = 3600;
    public const double MinWindowSize = 240;
    /// <summary>Windows が最小化中の座標として返す -32000 付近の値を弾くための下限（ISS-014）。</summary>
    public const double MinCoordinate = -10000;
    public const double MaxCoordinate = 10000;

    public static AppSettings Normalize(AppSettings s)
    {
        var a = s.Appearance;
        a.Opacity = Clamp(a.Opacity, MinOpacity, MaxOpacity);
        a.EditorFontSize = Clamp(a.EditorFontSize, MinFontSize, MaxFontSize);
        a.PreviewFontSize = Clamp(a.PreviewFontSize, MinFontSize, MaxFontSize);
        a.WindowColor = NormalizeColor(a.WindowColor, "#1E1E1E");
        a.ForegroundColor = NormalizeColor(a.ForegroundColor, "#FFFFFF");
        if (string.IsNullOrWhiteSpace(a.EditorFontFamily)) a.EditorFontFamily = "Consolas";
        if (string.IsNullOrWhiteSpace(a.PreviewFontFamily)) a.PreviewFontFamily = "Yu Gothic UI";

        var b = s.Behavior;
        b.PreviewDebounceMs = (int)Clamp(b.PreviewDebounceMs, MinDebounceMs, MaxDebounceMs);
        b.AutoExpandDelaySeconds = (int)Clamp(b.AutoExpandDelaySeconds, 0, MaxDelaySeconds);
        b.AutoMinimizeDelaySeconds = (int)Clamp(b.AutoMinimizeDelaySeconds, 0, MaxDelaySeconds);
        b.AutoSaveIntervalSeconds = (int)Clamp(b.AutoSaveIntervalSeconds, 10, MaxDelaySeconds);

        var w = s.Window;
        if (!IsFinite(w.Width) || w.Width < MinWindowSize) w.Width = 900;
        if (!IsFinite(w.Height) || w.Height < MinWindowSize) w.Height = 600;
        // Issue #14: OS 最小化中に Left/Top を読むと Windows が "-32000" 付近の
        // アイコン化位置を返すことがある。これが誤って保存されると次回起動時に
        // ウィンドウが画面外・不正なサイズ相当の位置で復元される。範囲外は既定値へ戻す。
        if (!IsFinite(w.Left) || w.Left <= MinCoordinate || w.Left >= MaxCoordinate) w.Left = 120;
        if (!IsFinite(w.Top) || w.Top <= MinCoordinate || w.Top >= MaxCoordinate) w.Top = 120;

        s.RecentFiles.RemoveAll(string.IsNullOrWhiteSpace);
        if (s.RecentFiles.Count > AppSettings.MaxRecentFiles)
            s.RecentFiles.RemoveRange(AppSettings.MaxRecentFiles, s.RecentFiles.Count - AppSettings.MaxRecentFiles);
        return s;
    }

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

    private static double Clamp(double v, double min, double max)
        => !IsFinite(v) ? min : Math.Min(Math.Max(v, min), max);

    /// <summary>#RGB / #RRGGBB / #AARRGGBB のみ許容し、それ以外は既定値へ戻す。</summary>
    public static string NormalizeColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var v = value.Trim();
        if (v[0] != '#') return fallback;
        if (v.Length is not (4 or 7 or 9)) return fallback;
        for (int i = 1; i < v.Length; i++)
            if (!Uri.IsHexDigit(v[i])) return fallback;
        return v.ToUpperInvariant();
    }
}
