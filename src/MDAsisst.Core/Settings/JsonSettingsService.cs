using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MDAsisst.Core.Logging;

namespace MDAsisst.Core.Settings;

/// <summary>
/// settings.json による設定永続化。
/// 保存先はアプリフォルダではなくユーザープロファイル配下とする
/// （Velopack の更新でアプリディレクトリが差し替わるため。ADR-0003 参照）。
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly ILogSink _log;

    public AppSettings Current { get; private set; } = new();

    public JsonSettingsService(string filePath, ILogSink? log = null)
    {
        _filePath = filePath;
        _log = log ?? NullLogSink.Instance;
    }

    /// <summary>既定の保存先 %APPDATA%\MDAsisst\settings.json を用いる。</summary>
    public static JsonSettingsService CreateDefault(ILogSink? log = null)
        => new(Path.Combine(AppPaths.UserDataDirectory, "settings.json"), log);

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                Current = SettingsValidator.Normalize(new AppSettings());
                Save(Current);
                return Current;
            }

            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, Options);
            if (loaded is null) throw new JsonException("設定の逆シリアル化結果が null です。");
            Current = SettingsValidator.Normalize(loaded);
            return Current;
        }
        catch (Exception ex)
        {
            // 破損は「正常」として扱わない。退避して原因を残し、既定値で継続する（FR-ST-02）。
            _log.Warn("設定ファイルの読み込みに失敗したため既定値で起動します。", ex);
            QuarantineCorruptFile();
            Current = SettingsValidator.Normalize(new AppSettings());
            return Current;
        }
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            SettingsValidator.Normalize(settings);
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            // 書き込み途中の電源断で設定を失わないよう、一時ファイル経由で差し替える。
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(settings, Options));
            File.Move(tmp, _filePath, overwrite: true);
            Current = settings;
            return true;
        }
        catch (Exception ex)
        {
            _log.Warn("設定ファイルの保存に失敗しました。", ex);
            return false;
        }
    }

    public AppSettings ResetToDefaults()
    {
        var defaults = SettingsValidator.Normalize(new AppSettings());
        Save(defaults);
        Current = defaults;
        return defaults;
    }

    private void QuarantineCorruptFile()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var dest = Path.Combine(
                Path.GetDirectoryName(_filePath) ?? ".",
                $"settings.corrupt.{stamp}.json");
            File.Move(_filePath, dest, overwrite: true);
            _log.Info($"破損した設定ファイルを退避しました: {Path.GetFileName(dest)}");
        }
        catch (Exception ex)
        {
            _log.Warn("破損設定ファイルの退避に失敗しました。", ex);
        }
    }
}
