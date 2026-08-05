using System.Text;

namespace MDAsisst.Core.Logging;

/// <summary>日付ごとのテキストログ。保持期間を過ぎたファイルは起動時に削除する（NFR-09）。</summary>
public sealed class FileLogSink : ILogSink
{
    private readonly string _directory;
    private readonly int _retentionDays;
    private readonly object _gate = new();

    public FileLogSink(string directory, int retentionDays = 7)
    {
        _directory = directory;
        _retentionDays = retentionDays;
        Directory.CreateDirectory(_directory);
        Cleanup();
    }

    public void Info(string message) => Write("INFO", message, null);
    public void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);
    public void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private void Write(string level, string message, Exception? ex)
    {
        try
        {
            var sb = new StringBuilder()
                .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
                .Append('\t').Append(level)
                .Append('\t').Append(message);
            if (ex is not null) sb.Append('\t').Append(ex.GetType().Name).Append(": ").Append(ex.Message);

            var path = Path.Combine(_directory, $"mdasisst-{DateTime.Now:yyyyMMdd}.log");
            lock (_gate) File.AppendAllText(path, sb.AppendLine().ToString(), Encoding.UTF8);
        }
        catch
        {
            // ログ出力の失敗でアプリを止めない。
        }
    }

    private void Cleanup()
    {
        try
        {
            var limit = DateTime.Now.AddDays(-_retentionDays);
            foreach (var f in Directory.EnumerateFiles(_directory, "mdasisst-*.log"))
                if (File.GetLastWriteTime(f) < limit) File.Delete(f);
        }
        catch
        {
            // 掃除の失敗は無視してよい。
        }
    }
}
