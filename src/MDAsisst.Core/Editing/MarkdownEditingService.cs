using System.Globalization;
using System.Text;
using MDAsisst.Core.Snippets;

namespace MDAsisst.Core.Editing;

/// <summary>
/// Markdown 入力支援のドメインロジック（FR-IA-01〜04）。
/// UI（TextBox）に依存しないため単体テストが可能（NFR-07）。
/// </summary>
public static class MarkdownEditingService
{
    /// <summary>補完トリガーとして扱う文字（FR-IA-01）。</summary>
    public static readonly char[] TriggerChars = { '#', '-', '>', '`', '[', '|', '*', '~', '!' };

    /// <summary>
    /// Enter 押下時のリスト自動継続（FR-IA-02）。
    /// 空項目で Enter を押した場合はマーカーを取り除いて解除する。
    /// </summary>
    public static EditResult HandleEnter(string text, int caret)
    {
        text ??= string.Empty;
        caret = Math.Clamp(caret, 0, text.Length);

        var lineStart = LineStart(text, caret);
        var line = text.Substring(lineStart, caret - lineStart);
        var marker = ParseListMarker(line);
        if (marker is null) return EditResult.NotHandled(text, caret);

        var (indent, prefix, content) = marker.Value;

        if (content.Length == 0)
        {
            // 空項目 → リストを解除する（行頭からマーカーを削除）。
            var newText = text.Remove(lineStart, caret - lineStart);
            return new EditResult(newText, lineStart, 0, true);
        }

        var next = indent + NextPrefix(prefix);
        var inserted = Environment.NewLine + next;
        return new EditResult(text.Insert(caret, inserted), caret + inserted.Length, 0, true);
    }

    /// <summary>選択範囲に対する装飾トグル（FR-IA-03）。既に囲まれていれば外す。</summary>
    public static EditResult ToggleWrap(string text, int selectionStart, int selectionLength, string marker)
    {
        text ??= string.Empty;
        if (string.IsNullOrEmpty(marker)) return EditResult.NotHandled(text, selectionStart);

        selectionStart = Math.Clamp(selectionStart, 0, text.Length);
        selectionLength = Math.Clamp(selectionLength, 0, text.Length - selectionStart);

        var selected = text.Substring(selectionStart, selectionLength);

        // ケース1: 選択文字列自体が marker で囲まれている
        if (selected.Length >= marker.Length * 2 &&
            selected.StartsWith(marker, StringComparison.Ordinal) &&
            selected.EndsWith(marker, StringComparison.Ordinal))
        {
            var inner = selected.Substring(marker.Length, selected.Length - marker.Length * 2);
            var t = text.Remove(selectionStart, selectionLength).Insert(selectionStart, inner);
            return new EditResult(t, selectionStart, inner.Length, true);
        }

        // ケース2: 選択の外側が marker で囲まれている
        var before = selectionStart - marker.Length;
        var after = selectionStart + selectionLength;
        if (before >= 0 && after + marker.Length <= text.Length &&
            string.CompareOrdinal(text, before, marker, 0, marker.Length) == 0 &&
            string.CompareOrdinal(text, after, marker, 0, marker.Length) == 0)
        {
            var t = text.Remove(after, marker.Length).Remove(before, marker.Length);
            return new EditResult(t, before, selectionLength, true);
        }

        // ケース3: 囲む
        var wrapped = marker + selected + marker;
        var newText = text.Remove(selectionStart, selectionLength).Insert(selectionStart, wrapped);
        return new EditResult(newText, selectionStart + marker.Length, selectionLength, true);
    }

    /// <summary>
    /// スニペットを挿入する。$SEL は選択文字列、$0 は挿入後のキャレット位置を表す。
    /// </summary>
    public static EditResult InsertSnippet(string text, int selectionStart, int selectionLength, SnippetItem snippet)
    {
        ArgumentNullException.ThrowIfNull(snippet);
        return InsertSnippet(text, selectionStart, selectionLength, snippet.InsertText);
    }

    public static EditResult InsertSnippet(string text, int selectionStart, int selectionLength, string insertText)
    {
        text ??= string.Empty;
        insertText ??= string.Empty;
        selectionStart = Math.Clamp(selectionStart, 0, text.Length);
        selectionLength = Math.Clamp(selectionLength, 0, text.Length - selectionStart);

        var selected = text.Substring(selectionStart, selectionLength);
        var body = insertText.Replace("$SEL", selected, StringComparison.Ordinal);

        var caretMark = body.IndexOf("$0", StringComparison.Ordinal);
        if (caretMark >= 0) body = body.Remove(caretMark, 2);

        var newText = text.Remove(selectionStart, selectionLength).Insert(selectionStart, body);
        var caret = selectionStart + (caretMark >= 0 ? caretMark : body.Length);
        return new EditResult(newText, caret, 0, true);
    }

    /// <summary>表の雛形を生成する（FR-IA-04）。</summary>
    public static string CreateTable(int rows, int columns)
    {
        rows = Math.Clamp(rows, 1, 50);
        columns = Math.Clamp(columns, 1, 20);

        var sb = new StringBuilder();
        sb.Append('|');
        for (int c = 0; c < columns; c++) sb.Append(" 見出し").Append(c + 1).Append(" |");
        sb.AppendLine();
        sb.Append('|');
        for (int c = 0; c < columns; c++) sb.Append(" --- |");
        sb.AppendLine();
        for (int r = 0; r < rows; r++)
        {
            sb.Append('|');
            for (int c = 0; c < columns; c++) sb.Append("  |");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// キャレット直前の文字列から補完トリガーを取り出す（FR-IA-01）。
    /// 見つからない場合は空文字を返す。
    /// </summary>
    public static string DetectTrigger(string text, int caret)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        caret = Math.Clamp(caret, 0, text.Length);

        var start = caret;
        while (start > 0 && TriggerChars.Contains(text[start - 1])) start--;
        if (start == caret) return string.Empty;

        // 直前が空白・行頭でなければ、文章中の記号なので補完対象にしない。
        if (start > 0 && !char.IsWhiteSpace(text[start - 1])) return string.Empty;
        return text[start..caret];
    }

    private static int LineStart(string text, int caret)
    {
        var i = caret - 1;
        while (i >= 0 && text[i] is not ('\n' or '\r')) i--;
        return i + 1;
    }

    /// <summary>行頭のリストマーカーを解析する。戻り値は (インデント, マーカー, 本文)。</summary>
    private static (string Indent, string Prefix, string Content)? ParseListMarker(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
        var indent = line[..i];
        var rest = line[i..];

        // 箇条書き: -, *, + （タスクリスト "- [ ] " を含む）
        if (rest.Length >= 2 && (rest[0] is '-' or '*' or '+') && rest[1] == ' ')
        {
            var afterBullet = rest[2..];
            if (afterBullet.StartsWith("[ ] ", StringComparison.Ordinal) ||
                afterBullet.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase))
            {
                return (indent, rest[..2] + afterBullet[..4], afterBullet[4..].Trim());
            }
            return (indent, rest[..2], afterBullet.Trim());
        }

        // 番号付き: 1. / 1)
        int d = 0;
        while (d < rest.Length && char.IsAsciiDigit(rest[d])) d++;
        if (d > 0 && d + 1 < rest.Length && (rest[d] is '.' or ')') && rest[d + 1] == ' ')
            return (indent, rest[..(d + 2)], rest[(d + 2)..].Trim());

        // 引用
        if (rest.StartsWith("> ", StringComparison.Ordinal))
            return (indent, "> ", rest[2..].Trim());

        return null;
    }

    /// <summary>次の行に置くマーカー。番号付きリストは連番を進める。</summary>
    private static string NextPrefix(string prefix)
    {
        int d = 0;
        while (d < prefix.Length && char.IsAsciiDigit(prefix[d])) d++;
        if (d > 0 && int.TryParse(prefix[..d], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return (n + 1).ToString(CultureInfo.InvariantCulture) + prefix[d..];

        // タスクリストは常に未チェックで継続する。
        if (prefix.Length >= 6 && prefix[2] == '[')
            return prefix[..2] + "[ ] ";

        return prefix;
    }
}
