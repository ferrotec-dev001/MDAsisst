using MDAsisst.Core.Logging;
using Velopack;
using Velopack.Sources;

namespace MDAsisst.Updating;

/// <summary>
/// Velopack + GitHub Releases による更新（ADR-0003）。
/// 例外はすべてログに残したうえで握り、アプリ本体の動作へ波及させない（FR-ST-08 / NFR-05）。
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
    public const string DefaultRepositoryUrl = "https://github.com/ferrotec-dev001/MDAsisst";

    private readonly UpdateManager _manager;
    private readonly ILogSink _log;
    private readonly TimeSpan _timeout;

    public VelopackUpdateService(ILogSink? log = null, string repositoryUrl = DefaultRepositoryUrl, TimeSpan? timeout = null)
    {
        _log = log ?? NullLogSink.Instance;
        _timeout = timeout ?? TimeSpan.FromSeconds(15);
        _manager = new UpdateManager(
            new GithubSource(repositoryUrl, accessToken: null, prerelease: false),
            new UpdateOptions { AllowVersionDowngrade = false });
    }

    public bool IsInstalled => _manager.IsInstalled;

    public string CurrentVersion => _manager.CurrentVersion?.ToString() ?? "dev";

    public async Task<UpdateCheckResult?> CheckAsync(CancellationToken ct = default)
    {
        // 開発実行・ポータブル配置では更新機構が無い。ここを通さないと NotInstalledException になる。
        if (!_manager.IsInstalled) return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeout);

            var info = await Task.Run(() => _manager.CheckForUpdatesAsync(), cts.Token).ConfigureAwait(false);
            if (info is null) return null;

            var version = info.TargetFullRelease.Version.ToString();
            return new UpdateCheckResult(version, $"{DefaultRepositoryUrl}/releases/tag/v{version}")
            {
                Payload = info
            };
        }
        catch (Exception ex)
        {
            // オフライン・プロキシ・レート制限などは想定内。警告としてのみ記録する。
            _log.Warn("更新確認に失敗しました（オフラインの可能性）。", ex);
            return null;
        }
    }

    // ADR-0011: DownloadAsync / ApplyAndRestart は撤去した。
    // アプリ自身が Program Files 配下の DLL を自己書き換えする経路（Velopack の
    // ApplyUpdatesAndRestart）が、ADR-0009 の per-machine 化後も EDR に検知される
    // 事例が確認されたため、更新の「適用」はユーザー／IT部門による MSI の手動
    // 再インストールに一本化する。本サービスは「新バージョンの有無を確認する」
    // （CheckAsync）機能のみを提供する。
}
