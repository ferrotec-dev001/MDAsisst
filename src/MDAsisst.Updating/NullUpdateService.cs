namespace MDAsisst.Updating;

/// <summary>
/// 更新モード「不要」で注入する実装。通信経路そのものを持たないことで
/// 「一切ネットワークアクセスを行わない」（FR-ST-07）をコード構造で保証する。
/// </summary>
public sealed class NullUpdateService : IUpdateService
{
    private readonly string _version;

    public NullUpdateService(string? currentVersion = null)
        => _version = string.IsNullOrWhiteSpace(currentVersion) ? "dev" : currentVersion;

    public bool IsInstalled => false;
    public string CurrentVersion => _version;

    public Task<UpdateCheckResult?> CheckAsync(CancellationToken ct = default)
        => Task.FromResult<UpdateCheckResult?>(null);

    public Task<bool> DownloadAsync(UpdateCheckResult update, IProgress<int>? progress = null, CancellationToken ct = default)
        => Task.FromResult(false);

    public bool ApplyOnExit(UpdateCheckResult update) => false;
    public bool ApplyAndRestart(UpdateCheckResult update) => false;
}
