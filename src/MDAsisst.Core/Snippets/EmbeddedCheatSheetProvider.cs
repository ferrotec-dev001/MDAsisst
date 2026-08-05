using System.Reflection;
using System.Text.Json;
using MDAsisst.Core.Logging;

namespace MDAsisst.Core.Snippets;

/// <summary>
/// アプリに埋め込んだ JSON からチートシートを読み込む（FR-CS-05: 完全オフライン）。
/// ユーザー定義スニペット（%APPDATA%\MDAsisst\snippets.json）があれば併合する（FR-CS-06）。
/// </summary>
public sealed class EmbeddedCheatSheetProvider : ICheatSheetProvider
{
    private const string ResourceName = "MDAsisst.Core.Snippets.Resources.cheatsheet.json";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly List<CheatSheetCategory> _categories;
    private readonly List<SnippetItem> _all;

    public EmbeddedCheatSheetProvider(string? userSnippetsPath = null, ILogSink? log = null)
    {
        var logSink = log ?? NullLogSink.Instance;
        _categories = LoadBuiltIn(logSink);

        if (!string.IsNullOrWhiteSpace(userSnippetsPath) && File.Exists(userSnippetsPath))
        {
            try
            {
                var userItems = JsonSerializer.Deserialize<List<SnippetItem>>(
                    File.ReadAllText(userSnippetsPath), Options);
                if (userItems is { Count: > 0 })
                    _categories.Add(new CheatSheetCategory { Name = "ユーザー定義", Items = userItems });
            }
            catch (Exception ex)
            {
                logSink.Warn("ユーザー定義スニペットの読み込みに失敗しました。", ex);
            }
        }

        _all = _categories.SelectMany(c => c.Items).ToList();
    }

    public IReadOnlyList<CheatSheetCategory> GetCategories() => _categories;

    public IReadOnlyList<SnippetItem> Search(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _all;
        var k = keyword.Trim();
        return _all.Where(i =>
            i.Title.Contains(k, StringComparison.OrdinalIgnoreCase) ||
            i.Example.Contains(k, StringComparison.OrdinalIgnoreCase) ||
            i.Description.Contains(k, StringComparison.OrdinalIgnoreCase) ||
            i.Keywords.Any(w => w.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public IReadOnlyList<SnippetItem> GetCompletions(string trigger)
    {
        if (string.IsNullOrEmpty(trigger)) return Array.Empty<SnippetItem>();
        return _all.Where(i => !string.IsNullOrEmpty(i.Trigger) &&
                               i.Trigger.StartsWith(trigger, StringComparison.Ordinal))
                   .ToList();
    }

    private static List<CheatSheetCategory> LoadBuiltIn(ILogSink log)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"埋め込みリソース {ResourceName} が見つかりません。");
            var list = JsonSerializer.Deserialize<List<CheatSheetCategory>>(stream, Options);
            return list ?? throw new InvalidOperationException("チートシート定義が空です。");
        }
        catch (Exception ex)
        {
            // チートシートは中核機能。欠落を黙って正常扱いせず、ログに残したうえで空を返す。
            log.Error("内蔵チートシートの読み込みに失敗しました。", ex);
            return new List<CheatSheetCategory>();
        }
    }
}
