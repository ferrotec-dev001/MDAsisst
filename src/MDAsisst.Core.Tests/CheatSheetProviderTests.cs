using MDAsisst.Core.Snippets;
using Xunit;

namespace MDAsisst.Core.Tests;

public class CheatSheetProviderTests
{
    private static readonly EmbeddedCheatSheetProvider Provider = new();

    [Fact]
    public void 内蔵チートシートが読み込める()
    {
        var categories = Provider.GetCategories();
        Assert.NotEmpty(categories);
        Assert.All(categories, c => Assert.NotEmpty(c.Items));
    }

    [Fact]
    public void 主要な記法が網羅されている()
    {
        var titles = Provider.Search(null).Select(i => i.Title).ToList();
        foreach (var expected in new[] { "見出し1", "太字", "箇条書き", "リンク", "コードブロック", "表(2列)", "引用" })
            Assert.Contains(expected, titles);
    }

    [Fact]
    public void すべての項目に記述例と挿入テキストがある()
    {
        foreach (var item in Provider.Search(null))
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Title));
            Assert.False(string.IsNullOrWhiteSpace(item.Example));
            Assert.False(string.IsNullOrWhiteSpace(item.InsertText));
        }
    }

    [Theory]
    [InlineData("太字")]
    [InlineData("bold")]
    [InlineData("TABLE")]
    public void キーワード検索が効く(string keyword)
        => Assert.NotEmpty(Provider.Search(keyword));

    [Fact]
    public void 該当なしの検索は空を返す()
        => Assert.Empty(Provider.Search("該当しないキーワードzzz"));

    [Fact]
    public void 空キーワードは全件返す()
        => Assert.Equal(Provider.Search(null).Count, Provider.Search("  ").Count);

    [Fact]
    public void トリガーから補完候補を取得できる()
    {
        var completions = Provider.GetCompletions("#");
        Assert.Contains(completions, i => i.Title == "見出し1");
        Assert.Contains(completions, i => i.Title == "見出し2");
        Assert.Empty(Provider.GetCompletions(""));
    }
}
