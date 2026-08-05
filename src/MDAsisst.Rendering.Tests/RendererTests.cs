using System.Diagnostics;
using System.Text;
using System.Windows.Documents;
using MDAsisst.Rendering;
using Xunit;

namespace MDAsisst.Rendering.Tests;

/// <summary>
/// FlowDocument レンダラの構造検証。WPF 型を使うため Windows 上でのみ実行される。
/// </summary>
public class RendererTests
{
    private static FlowDocument Render(string markdown)
        => new FlowDocumentMarkdownRenderer(new MarkdownTheme()).Render(markdown);

    [Fact]
    public void 見出しは太字の段落になる()
    {
        var doc = Render("# タイトル");
        var p = Assert.IsType<Paragraph>(doc.Blocks.FirstBlock);
        Assert.Equal(System.Windows.FontWeights.Bold, p.FontWeight);
        Assert.Contains("タイトル", new TextRange(p.ContentStart, p.ContentEnd).Text);
    }

    [Theory]
    [InlineData("# h1")]
    [InlineData("###### h6")]
    [InlineData("**bold**")]
    [InlineData("*italic*")]
    [InlineData("~~strike~~")]
    [InlineData("`code`")]
    [InlineData("> quote")]
    [InlineData("- item")]
    [InlineData("1. item")]
    [InlineData("- [ ] task")]
    [InlineData("---")]
    [InlineData("[link](https://example.com)")]
    [InlineData("| a | b |\n| --- | --- |\n| 1 | 2 |")]
    [InlineData("```csharp\nvar x = 1;\n```")]
    public void 主要記法がブロックを生成する(string markdown)
    {
        var doc = Render(markdown);
        Assert.NotEmpty(doc.Blocks);
    }

    [Fact]
    public void リストは項目数ぶんのListItemになる()
    {
        var doc = Render("- 一\n- 二\n- 三");
        var list = Assert.IsType<List>(doc.Blocks.FirstBlock);
        Assert.Equal(3, list.ListItems.Count);
    }

    [Fact]
    public void 表はヘッダを含む行数分生成される()
    {
        var doc = Render("| A | B |\n| --- | --- |\n| 1 | 2 |\n| 3 | 4 |");
        var table = Assert.IsType<Table>(doc.Blocks.FirstBlock);
        Assert.Equal(2, table.Columns.Count);
        Assert.Equal(3, table.RowGroups[0].Rows.Count);
    }

    [Fact]
    public void コードブロックの本文が保持される()
    {
        var doc = Render("```\nline1\nline2\n```");
        var text = new TextRange(doc.ContentStart, doc.ContentEnd).Text;
        Assert.Contains("line1", text);
        Assert.Contains("line2", text);
    }

    [Fact]
    public void 未対応記法でも本文が欠落しない()
    {
        var doc = Render("$$ x^2 $$ という数式");
        var text = new TextRange(doc.ContentStart, doc.ContentEnd).Text;
        Assert.Contains("という数式", text);
    }

    [Fact]
    public void 空文字でも例外にならない()
    {
        Assert.Empty(Render(string.Empty).Blocks);
        Assert.Empty(Render("   ").Blocks);
    }

    private static string BuildDocument(int lines)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < lines; i++)
            sb.AppendLine(i % 10 == 0 ? $"## 見出し {i}" : $"- 項目 {i} と **強調** と `code`");
        return sb.ToString();
    }

    /// <summary>
    /// NFR-04（改訂, ADR-0005）: ライブプレビュー対象は
    /// <see cref="MDAsisst.App.Views.MainWindow"/> の LargeDocumentLineThreshold（5,000行）以下。
    /// 実測（CI: windows-latest, ウォームアップ後）で 10,000 行は約 890ms かかり
    /// 500ms を満たせないことが判明したため、ADR-0005 で 5,000行超は自動プレビューを
    /// 一時停止する仕様に変更した。本テストは改訂後の対象範囲で性能を保証する。
    /// </summary>
    [Fact]
    public void 五千行の文書を継続入力中の再描画で500ミリ秒以内に描画できる()
    {
        var markdown = BuildDocument(5000);
        _ = Render(markdown);   // ウォームアップ（JIT・初回アロケーション）

        var sw = Stopwatch.StartNew();
        var doc = Render(markdown);
        sw.Stop();

        Assert.NotEmpty(doc.Blocks);
        Assert.True(sw.ElapsedMilliseconds < 500, $"NFR-04 違反（ウォームアップ後）: {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// ADR-0005 の自動プレビュー一時停止の対象となる規模（10,000行）でも、
    /// 手動更新（Ctrl+Shift+P）でUIが長時間フリーズしないことを上限値で確認する。
    /// </summary>
    [Fact]
    public void 一万行の文書の手動更新描画が3秒以内に完了する()
    {
        var markdown = BuildDocument(10000);
        _ = Render(markdown);   // ウォームアップ

        var sw = Stopwatch.StartNew();
        var doc = Render(markdown);
        sw.Stop();

        Assert.NotEmpty(doc.Blocks);
        Assert.True(sw.ElapsedMilliseconds < 3000, $"ISS-006 悪化: {sw.ElapsedMilliseconds}ms");
    }
}
