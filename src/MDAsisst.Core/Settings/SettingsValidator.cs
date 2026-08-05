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
        if (!IsFinite(w.Left)) w.Left = 120;
        if (!IsFinite(w.Top)) w.Top = 120;

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
