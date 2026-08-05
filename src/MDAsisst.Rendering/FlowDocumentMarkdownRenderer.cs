using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdTable = Markdig.Extensions.Tables.Table;
using MdTableRow = Markdig.Extensions.Tables.TableRow;
using MdTableCell = Markdig.Extensions.Tables.TableCell;
using MdTableAlign = Markdig.Extensions.Tables.TableColumnAlign;
using WpfBlock = System.Windows.Documents.Block;
using WpfInline = System.Windows.Documents.Inline;
using WpfList = System.Windows.Documents.List;
using WpfTable = System.Windows.Documents.Table;

namespace MDAsisst.Rendering;

/// <summary>
/// Markdig の AST を WPF の FlowDocument へ変換する（ADR-0002）。
/// WebView2 を使わないため、追加ランタイム不要・完全オフラインで動作する。
/// </summary>
public sealed class FlowDocumentMarkdownRenderer
{
    // MarkdownPipeline はイミュータブルなので使い回す（毎回 Build するとコストが高い）。
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseTaskLists()
        .UseAutoLinks()
        .Build();

    private readonly MarkdownTheme _theme;

    /// <summary>リンククリック時の遷移先。UI 層が既定ブラウザ起動などを行う（FR-PV-05）。</summary>
    public event EventHandler<Uri>? LinkNavigated;

    /// <summary>相対パス画像を解決するための基準ディレクトリ（FR-PV-06）。</summary>
    public string? BaseDirectory { get; set; }

    public FlowDocumentMarkdownRenderer(MarkdownTheme theme)
        => _theme = (theme ?? throw new ArgumentNullException(nameof(theme))).Frozen();

    public static MarkdownDocument Parse(string markdown)
        => Markdown.Parse(markdown ?? string.Empty, Pipeline);

    /// <summary>Markdown 文字列を FlowDocument へ変換する。</summary>
    public FlowDocument Render(string markdown)
    {
        var doc = CreateDocument();
        foreach (var block in Parse(markdown))
        {
            var converted = ConvertBlock(block);
            if (converted is not null) doc.Blocks.Add(converted);
        }
        return doc;
    }

    private FlowDocument CreateDocument() => new()
    {
        FontFamily = _theme.BodyFont,
        FontSize = _theme.BaseFontSize,
        Foreground = _theme.Foreground,
        Background = Brushes.Transparent,        // 半透明ウィンドウ上に載せるため明示する
        PagePadding = new Thickness(12),
        ColumnWidth = double.PositiveInfinity,   // 既定のままだと勝手に段組みされる
        IsOptimalParagraphEnabled = false,       // 性能優先（NFR-04）
        IsHyphenationEnabled = false,
        TextAlignment = TextAlignment.Left       // 日本語では Justify だと不自然な間延びが出る
    };

    private WpfBlock? ConvertBlock(Markdig.Syntax.Block block) => block switch
    {
        HeadingBlock h => Heading(h),
        ParagraphBlock p => Paragraph(p),
        QuoteBlock q => Quote(q),
        ListBlock l => List(l),
        CodeBlock c => Code(c),
        ThematicBreakBlock => Rule(),
        MdTable t => Table(t),
        HtmlBlock html => HtmlFallback(html),
        _ => null
    };

    private WpfBlock Heading(HeadingBlock h)
    {
        double[] scale = { 1.9, 1.55, 1.32, 1.18, 1.08, 1.0 };
        var p = new Paragraph
        {
            FontSize = _theme.BaseFontSize * scale[Math.Clamp(h.Level - 1, 0, 5)],
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, h.Level <= 2 ? 14 : 10, 0, 6)
        };
        if (h.Inline is not null) AppendInlines(p.Inlines, h.Inline);
        if (h.Level <= 2)
        {
            p.BorderBrush = _theme.RuleBrush;
            p.BorderThickness = new Thickness(0, 0, 0, 1);
            p.Padding = new Thickness(0, 0, 0, 4);
        }
        return p;
    }

    private Paragraph Paragraph(ParagraphBlock pb)
    {
        var p = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
        if (pb.Inline is not null) AppendInlines(p.Inlines, pb.Inline);
        return p;
    }

    private Section Quote(QuoteBlock q)
    {
        var s = new Section
        {
            BorderBrush = _theme.QuoteBar,
            BorderThickness = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(10, 2, 0, 2),
            Margin = new Thickness(0, 4, 0, 8)
        };
        foreach (var child in q)
        {
            var b = ConvertBlock(child);
            if (b is not null) s.Blocks.Add(b);
        }
        if (s.Blocks.Count == 0) s.Blocks.Add(new Paragraph());
        return s;
    }

    private WpfList List(ListBlock lb)
    {
        var list = new WpfList
        {
            MarkerStyle = lb.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(22, 0, 0, 0)
        };
        if (lb.IsOrdered && int.TryParse(lb.OrderedStart, out var start) && start > 0)
            list.StartIndex = start;

        foreach (var child in lb)
        {
            if (child is not ListItemBlock item) continue;
            var li = new ListItem();
            foreach (var sub in item)
            {
                var b = ConvertBlock(sub);
                if (b is null) continue;
                b.Margin = new Thickness(0, 0, 0, 2);
                li.Blocks.Add(b);
            }
            if (li.Blocks.Count == 0) li.Blocks.Add(new Paragraph());
            list.ListItems.Add(li);
        }
        return list;
    }

    private WpfBlock Code(CodeBlock cb)
    {
        var text = ExtractCodeText(cb);
        var language = (cb as FencedCodeBlock)?.Info;

        var p = new Paragraph
        {
            FontFamily = _theme.CodeFont,
            FontSize = _theme.BaseFontSize * 0.92,
            Foreground = _theme.CodeForeground,
            Background = _theme.CodeBackground,
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 4, 0, 10),
            TextAlignment = TextAlignment.Left
        };
        if (!string.IsNullOrWhiteSpace(language))
        {
            p.Inlines.Add(new Run(language + Environment.NewLine)
            {
                FontSize = _theme.BaseFontSize * 0.75,
                Foreground = _theme.QuoteBar
            });
        }
        p.Inlines.Add(new Run(text));
        return p;
    }

    private static string ExtractCodeText(CodeBlock cb)
    {
        var sb = new StringBuilder();
        var lines = cb.Lines.Lines;
        for (int i = 0; i < cb.Lines.Count; i++)
            sb.AppendLine(lines[i].Slice.ToString());
        return sb.ToString().TrimEnd('\r', '\n');
    }

    private Paragraph Rule() => new()
    {
        Margin = new Thickness(0, 10, 0, 10),
        BorderBrush = _theme.RuleBrush,
        BorderThickness = new Thickness(0, 1, 0, 0)
    };

    private WpfTable Table(MdTable mdTable)
    {
        var table = new WpfTable { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
        var columnCount = mdTable.OfType<MdTableRow>().Select(r => r.Count).DefaultIfEmpty(1).Max();
        for (int i = 0; i < columnCount; i++) table.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        foreach (var rowObj in mdTable)
        {
            if (rowObj is not MdTableRow mdRow) continue;
            var row = new TableRow();
            int columnIndex = 0;

            foreach (var cellObj in mdRow)
            {
                if (cellObj is not MdTableCell mdCell) continue;
                var cell = new TableCell
                {
                    BorderBrush = _theme.RuleBrush,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(6, 3, 6, 3),
                    ColumnSpan = Math.Max(1, mdCell.ColumnSpan),
                    RowSpan = Math.Max(1, mdCell.RowSpan)
                };

                var align = columnIndex < mdTable.ColumnDefinitions.Count
                    ? mdTable.ColumnDefinitions[columnIndex].Alignment
                    : null;
                var textAlign = align switch
                {
                    MdTableAlign.Center => TextAlignment.Center,
                    MdTableAlign.Right => TextAlignment.Right,
                    _ => TextAlignment.Left
                };

                foreach (var sub in mdCell)
                {
                    var b = ConvertBlock(sub);
                    if (b is null) continue;
                    b.Margin = new Thickness(0);
                    b.TextAlignment = textAlign;
                    cell.Blocks.Add(b);
                }
                if (cell.Blocks.Count == 0) cell.Blocks.Add(new Paragraph());
                if (mdRow.IsHeader) cell.FontWeight = FontWeights.Bold;

                row.Cells.Add(cell);
                columnIndex += cell.ColumnSpan;
            }
            group.Rows.Add(row);
        }
        return table;
    }

    /// <summary>生 HTML は解釈せずコードとして見せる（サニタイズ不要にして安全側に倒す）。</summary>
    private Paragraph HtmlFallback(HtmlBlock html)
    {
        var sb = new StringBuilder();
        var lines = html.Lines.Lines;
        for (int i = 0; i < html.Lines.Count; i++)
            sb.AppendLine(lines[i].Slice.ToString());

        return new Paragraph(new Run(sb.ToString().TrimEnd()))
        {
            FontFamily = _theme.CodeFont,
            Foreground = _theme.CodeForeground,
            Margin = new Thickness(0, 0, 0, 8)
        };
    }

    private void AppendInlines(InlineCollection target, ContainerInline container)
    {
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    target.Add(new Run(literal.Content.ToString()));
                    break;

                case EmphasisInline em:
                {
                    var span = new Span();
                    switch (em.DelimiterChar)
                    {
                        case '*' or '_':
                            if (em.DelimiterCount >= 2) span.FontWeight = FontWeights.Bold;
                            else span.FontStyle = FontStyles.Italic;
                            break;
                        case '~':
                            span.TextDecorations = TextDecorations.Strikethrough;
                            break;
                        case '=':
                            span.Background = _theme.CodeBackground;
                            break;
                    }
                    AppendInlines(span.Inlines, em);
                    target.Add(span);
                    break;
                }

                case CodeInline code:
                    target.Add(new Run(code.Content)
                    {
                        FontFamily = _theme.CodeFont,
                        Foreground = _theme.CodeForeground,
                        Background = _theme.CodeBackground
                    });
                    break;

                case TaskList task:
                    target.Add(new Run(task.Checked ? "\u2611 " : "\u2610 "));
                    break;

                case LinkInline { IsImage: true } image:
                    target.Add(ImageInline(image));
                    break;

                case LinkInline link:
                    target.Add(HyperlinkInline(link));
                    break;

                case AutolinkInline auto:
                {
                    var h = new Hyperlink(new Run(auto.Url)) { Foreground = _theme.LinkForeground };
                    AttachNavigation(h, auto.Url);
                    target.Add(h);
                    break;
                }

                case LineBreakInline lb:
                    if (lb.IsHard) target.Add(new LineBreak());
                    else target.Add(new Run(" "));
                    break;

                case HtmlEntityInline entity:
                    target.Add(new Run(entity.Transcoded.ToString()));
                    break;

                case HtmlInline:
                    // 生タグは表示しない（描画しても意味を持たないため）。
                    break;

                case ContainerInline nested:
                    AppendInlines(target, nested);
                    break;

                default:
                    // 未対応ノードでも本文が消えないよう、素のテキストとして残す。
                    var text = inline.ToString();
                    if (!string.IsNullOrEmpty(text)) target.Add(new Run(text));
                    break;
            }
        }
    }

    private WpfInline HyperlinkInline(LinkInline link)
    {
        var h = new Hyperlink { Foreground = _theme.LinkForeground, ToolTip = link.Url };
        AppendInlines(h.Inlines, link);
        if (h.Inlines.Count == 0) h.Inlines.Add(new Run(link.Url ?? string.Empty));
        AttachNavigation(h, link.Url);
        return h;
    }

    private void AttachNavigation(Hyperlink h, string? url)
    {
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)) return;
        h.NavigateUri = uri;
        h.RequestNavigate += (_, e) =>
        {
            e.Handled = true;
            LinkNavigated?.Invoke(this, e.Uri);
        };
    }

    /// <summary>ローカル画像のみ表示する。解決できない場合は代替テキストにフォールバックする。</summary>
    [SuppressMessage("Design", "CA1031", Justification = "画像1枚の失敗でプレビュー全体を止めない")]
    private WpfInline ImageInline(LinkInline image)
    {
        var alt = image.FirstChild?.ToString() ?? image.Url ?? "image";
        try
        {
            var path = ResolveImagePath(image.Url);
            if (path is null) return new Run($"[画像: {alt}]") { Foreground = _theme.CodeForeground };

            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path);
            bmp.DecodePixelWidth = 640;   // 原寸デコードを避けてメモリを抑える（NFR-02）
            bmp.EndInit();
            bmp.Freeze();

            return new InlineUIContainer(new System.Windows.Controls.Image
            {
                Source = bmp,
                MaxWidth = 640,
                Stretch = Stretch.Uniform
            });
        }
        catch (Exception)
        {
            return new Run($"[画像を表示できません: {alt}]") { Foreground = _theme.CodeForeground };
        }
    }

    private string? ResolveImagePath(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
            return abs.IsFile && File.Exists(abs.LocalPath) ? abs.LocalPath : null;   // http(s) は取得しない（オフライン方針）

        if (string.IsNullOrEmpty(BaseDirectory)) return null;
        var full = Path.GetFullPath(Path.Combine(BaseDirectory, url));
        return File.Exists(full) ? full : null;
    }
}
