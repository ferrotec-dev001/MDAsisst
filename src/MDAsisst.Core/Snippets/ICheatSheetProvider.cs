namespace MDAsisst.Core.Snippets;

/// <summary>チートシート定義の供給元。</summary>
public interface ICheatSheetProvider
{
    IReadOnlyList<CheatSheetCategory> GetCategories();

    /// <summary>キーワード検索（FR-CS-04）。空文字なら全件。</summary>
    IReadOnlyList<SnippetItem> Search(string? keyword);

    /// <summary>入力中のトリガー文字列に一致する補完候補を返す（FR-IA-01）。</summary>
    IReadOnlyList<SnippetItem> GetCompletions(string trigger);
}
