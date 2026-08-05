namespace MDAsisst.Core.Logging;

/// <summary>最小限のログ出力口。編集中の本文は決して渡さないこと（NFR-08/09）。</summary>
public interface ILogSink
{
    void Info(string message);
    void Warn(string message, Exception? ex = null);
    void Error(string message, Exception? ex = null);
}

/// <summary>ログを捨てる実装（テスト・既定値用）。</summary>
public sealed class NullLogSink : ILogSink
{
    public static readonly NullLogSink Instance = new();
    private NullLogSink() { }
    public void Info(string message) { }
    public void Warn(string message, Exception? ex = null) { }
    public void Error(string message, Exception? ex = null) { }
}
