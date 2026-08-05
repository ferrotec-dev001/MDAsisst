namespace MDAsisst.Core.Snippets;

/// <summary>チートシート／補完候補の 1 項目。</summary>
public sealed class SnippetItem
{
    /// <summary>一覧に表示する名称（例: 見出し1）。</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>アイコンボタンに表示する短い記法表記（例: "#", "**B**"）。FR-CS-05 対応（Issue #5）。</summary>
    public string Glyph { get; set; } = string.Empty;
    /// <summary>Markdown の記述例（例: # 見出し）。効果表示にも使う（FR-CS-02）。</summary>
    public string Example { get; set; } = string.Empty;
    /// <summary>挿入するテキスト。$0 はカーソル位置、$SEL は選択文字列を表す。</summary>
    public string InsertText { get; set; } = string.Empty;
    /// <summary>補完のトリガー文字列（例: "#"）。空なら補完対象外。</summary>
    public string Trigger { get; set; } = string.Empty;
    /// <summary>検索・説明用の補足。</summary>
    public string Description { get; set; } = string.Empty;
    public string[] Keywords { get; set; } = Array.Empty<string>();

    public override string ToString() => Title;
}

/// <summary>チートシートのカテゴリ（FR-CS-01）。</summary>
public sealed class CheatSheetCategory
{
    public string Name { get; set; } = string.Empty;
    public List<SnippetItem> Items { get; set; } = new();
}
