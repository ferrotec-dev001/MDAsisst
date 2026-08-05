using System.Windows.Media;
using MDAsisst.Core.Settings;

namespace MDAsisst.Rendering;

/// <summary>
/// プレビュー描画に使う配色・フォント。ユーザー設定（AppearanceSettings）から生成する。
/// Brush は Freeze してスレッド跨ぎと GC 負荷を抑える（NFR-02）。
/// </summary>
public sealed class MarkdownTheme
{
    public Brush Foreground { get; init; } = Brushes.White;
    public Brush CodeForeground { get; init; } = Brushes.Gold;
    public Brush CodeBackground { get; init; } = new SolidColorBrush(Color.FromArgb(48, 0, 0, 0));
    public Brush QuoteBar { get; init; } = Brushes.Gray;
    public Brush LinkForeground { get; init; } = Brushes.SkyBlue;
    public Brush RuleBrush { get; init; } = Brushes.Gray;
    public FontFamily BodyFont { get; init; } = new("Yu Gothic UI");
    public FontFamily CodeFont { get; init; } = new("Consolas");
    public double BaseFontSize { get; init; } = 14.0;

    /// <summary>アピアランス設定からテーマを構築する。色文字列が不正な場合は既定色にフォールバックする。</summary>
    public static MarkdownTheme FromSettings(AppearanceSettings a)
    {
        ArgumentNullException.ThrowIfNull(a);
        var fg = ToBrush(a.ForegroundColor, Colors.White);
        return new MarkdownTheme
        {
            Foreground = fg,
            LinkForeground = ToBrush("#4FC3F7", Colors.SkyBlue),
            CodeForeground = ToBrush("#FFD54F", Colors.Gold),
            CodeBackground = Freeze(new SolidColorBrush(Color.FromArgb(48, 0, 0, 0))),
            QuoteBar = Freeze(new SolidColorBrush(Color.FromArgb(160, 128, 128, 128))),
            RuleBrush = Freeze(new SolidColorBrush(Color.FromArgb(120, 128, 128, 128))),
            BodyFont = new FontFamily(a.PreviewFontFamily),
            CodeFont = new FontFamily(a.EditorFontFamily),
            BaseFontSize = a.PreviewFontSize
        }.Frozen();
    }

    public MarkdownTheme Frozen()
    {
        foreach (var b in new[] { Foreground, CodeForeground, CodeBackground, QuoteBar, LinkForeground, RuleBrush })
            Freeze(b);
        return this;
    }

    private static Brush Freeze(Brush b)
    {
        if (b.CanFreeze && !b.IsFrozen) b.Freeze();
        return b;
    }

    private static Brush ToBrush(string? hex, Color fallback)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex) &&
                ColorConverter.ConvertFromString(hex) is Color c)
                return Freeze(new SolidColorBrush(c));
        }
        catch (FormatException)
        {
            // 設定ファイルを人手編集した場合に起こり得る。既定色で継続する。
        }
        return Freeze(new SolidColorBrush(fallback));
    }
}
