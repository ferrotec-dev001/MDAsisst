using System.IO;
using System.Text;
using MDAsisst.Core.Logging;

namespace MDAsisst.App.Services;

/// <summary>読み込んだファイルの書式情報。保存時に元の形式を保つために使う（FR-ED-07）。</summary>
public sealed class DocumentFormat
{
    public bool HasBom { get; init; }
    public string NewLine { get; init; } = Environment.NewLine;

    public static readonly DocumentFormat Default = new();
}

/// <summary>Markdown ファイルの入出力。UTF-8（BOM 無し）を既定とし、元の BOM / 改行を保持する。</summary>
public sealed class DocumentService
{
    private readonly ILogSink _log;

    public DocumentService(ILogSink? log = null) => _log = log ?? NullLogSink.Instance;

    public (string Text, DocumentFormat Format) Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var text = new UTF8Encoding(false).GetString(hasBom ? bytes[3..] : bytes);
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

        // 編集中は LF に正規化し、保存時に元へ戻す（差分ノイズを避けるため）。
        return (text.Replace("\r\n", "\n"), new DocumentFormat { HasBom = hasBom, NewLine = newLine });
    }

    public bool Save(string path, string text, DocumentFormat format)
    {
        try
        {
            var body = text.Replace("\r\n", "\n");
            if (format.NewLine == "\r\n") body = body.Replace("\n", "\r\n");

            var encoding = new UTF8Encoding(format.HasBom);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, body, encoding);
            File.Move(tmp, path, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            // 保存失敗を成功扱いにしない。UI 側で必ずユーザーへ通知する。
            _log.Error($"ファイルの保存に失敗しました: {Path.GetFileName(path)}", ex);
            return false;
        }
    }
}
