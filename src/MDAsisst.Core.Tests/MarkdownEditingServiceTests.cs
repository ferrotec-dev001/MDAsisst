using MDAsisst.Core.Editing;
using Xunit;

namespace MDAsisst.Core.Tests;

public class MarkdownEditingServiceTests
{
    private static readonly string NL = Environment.NewLine;

    [Fact]
    public void 箇条書き行でEnterを押すとマーカーが継続する()
    {
        var text = "- 項目1";
        var r = MarkdownEditingService.HandleEnter(text, text.Length);

        Assert.True(r.Handled);
        Assert.Equal("- 項目1" + NL + "- ", r.Text);
        Assert.Equal(r.Text.Length, r.SelectionStart);
    }

    [Fact]
    public void 番号付きリストは連番が進む()
    {
        var text = "3. 三番目";
        var r = MarkdownEditingService.HandleEnter(text, text.Length);
        Assert.Equal("3. 三番目" + NL + "4. ", r.Text);
    }

    [Fact]
    public void タスクリストは未チェック状態で継続する()
    {
        var text = "- [x] 完了した作業";
        var r = MarkdownEditingService.HandleEnter(text, text.Length);
        Assert.Equal("- [x] 完了した作業" + NL + "- [ ] ", r.Text);
    }

    [Fact]
    public void インデント付きリストはインデントを維持する()
    {
        var text = "    - 子項目";
        var r = MarkdownEditingService.HandleEnter(text, text.Length);
        Assert.Equal("    - 子項目" + NL + "    - ", r.Text);
    }

    [Fact]
    public void 空のリスト項目でEnterを押すとリストが解除される()
    {
        var text = "- 項目1\n- ";
        var r = MarkdownEditingService.HandleEnter(text, text.Length);

        Assert.True(r.Handled);
        Assert.Equal("- 項目1\n", r.Text);
        Assert.Equal(r.Text.Length, r.SelectionStart);
    }

    [Fact]
    public void リストでない行では処理しない()
    {
        var text = "ただの文章";
        var r = MarkdownEditingService.HandleEnter(text, text.Length);
        Assert.False(r.Handled);
        Assert.Equal(text, r.Text);
    }

    [Fact]
    public void 選択範囲を太字で囲める()
    {
        var r = MarkdownEditingService.ToggleWrap("これは重要です", 3, 2, "**");
        Assert.Equal("これは**重要**です", r.Text);
        Assert.Equal(5, r.SelectionStart);
        Assert.Equal(2, r.SelectionLength);
    }

    [Fact]
    public void 既に太字なら解除される_選択が内側()
    {
        var r = MarkdownEditingService.ToggleWrap("これは**重要**です", 5, 2, "**");
        Assert.Equal("これは重要です", r.Text);
    }

    [Fact]
    public void 既に太字なら解除される_選択がマーカーを含む()
    {
        var r = MarkdownEditingService.ToggleWrap("これは**重要**です", 3, 6, "**");
        Assert.Equal("これは重要です", r.Text);
    }

    [Fact]
    public void 選択なしで装飾するとマーカーだけ挿入されカーソルが中に入る()
    {
        var r = MarkdownEditingService.ToggleWrap("abc", 3, 0, "**");
        Assert.Equal("abc****", r.Text);
        Assert.Equal(5, r.SelectionStart);
    }

    [Fact]
    public void スニペット挿入で選択文字列とカーソル位置が展開される()
    {
        var r = MarkdownEditingService.InsertSnippet("参照リンク", 0, 5, "[$SEL]($0)");
        Assert.Equal("[参照リンク]()", r.Text);
        Assert.Equal("[参照リンク](".Length, r.SelectionStart);
    }

    [Fact]
    public void カーソル記号のないスニペットは末尾にキャレットが来る()
    {
        var r = MarkdownEditingService.InsertSnippet("", 0, 0, "---");
        Assert.Equal("---", r.Text);
        Assert.Equal(3, r.SelectionStart);
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]   // 上限クランプされても例外にならない
    public void 表の雛形は行数と列数ぶん生成される(int rows, int cols)
    {
        var table = MarkdownEditingService.CreateTable(rows, cols);
        var lines = table.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var expectedRows = Math.Clamp(rows, 1, 50);
        Assert.Equal(expectedRows + 2, lines.Length);   // ヘッダ + 区切り + データ行
        Assert.StartsWith("|", lines[0]);
        Assert.Contains("---", lines[1]);
    }

    [Theory]
    [InlineData("#", 1, "#")]
    [InlineData("## ", 2, "##")]
    [InlineData("文中の#記号", 5, "")]      // 文章中の記号は補完対象外
    [InlineData("", 0, "")]
    [InlineData("- [", 3, "[")]     // 空白で分断された記号は連結しない
    public void トリガー検出は行頭や空白直後のみ有効(string text, int caret, string expected)
        => Assert.Equal(expected, MarkdownEditingService.DetectTrigger(text, caret));

    [Fact]
    public void キャレット位置が範囲外でも例外にならない()
    {
        var r = MarkdownEditingService.HandleEnter("abc", 999);
        Assert.False(r.Handled);
        var t = MarkdownEditingService.ToggleWrap("abc", -5, 100, "*");
        Assert.Equal("*abc*", t.Text);
    }
}
