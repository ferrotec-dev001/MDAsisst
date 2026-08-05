namespace MDAsisst.Core.Editing;

/// <summary>編集操作の結果。UI 非依存にするため、テキストと選択範囲だけを返す。</summary>
/// <param name="Text">操作後の全文。</param>
/// <param name="SelectionStart">操作後のキャレット位置（選択開始位置）。</param>
/// <param name="SelectionLength">操作後の選択長。0 ならキャレットのみ。</param>
/// <param name="Handled">操作を行ったか。false の場合、呼び出し側は既定動作を続行する。</param>
public readonly record struct EditResult(string Text, int SelectionStart, int SelectionLength, bool Handled)
{
    public static EditResult NotHandled(string text, int caret) => new(text, caret, 0, false);
}
