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

    public async Task<bool> DownloadAsync(UpdateCheckResult update, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (update?.Payload is not UpdateInfo info) return false;
        try
        {
            await _manager.DownloadUpdatesAsync(info, p => progress?.Report(p), ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _log.Warn("更新のダウンロードに失敗しました。", ex);
            return false;
        }
    }

    public bool ApplyOnExit(UpdateCheckResult update)
    {
        if (update?.Payload is not UpdateInfo info) return false;
        try
        {
            // ADR-0008: restart は必ず true にする。false にすると、Velopack がバックグラウンドで
            // current フォルダへ新バージョンを展開している最中にユーザーが手動で再起動した場合、
            // 展開途中の MDAsisst.dll をロードして FileLoadException（アクセス拒否）で
            // 起動不能になる（ISS-007）。restart: true にすることで、展開が完全に終わった後
            // にのみ Velopack 自身が再起動するため、このレースコンディションが構造的になくなる。
            _manager.WaitExitThenApplyUpdates(info.TargetFullRelease, silent: true, restart: true);
            return true;
        }
        catch (Exception ex)
        {
            _log.Warn("終了時更新の予約に失敗しました。", ex);
            return false;
        }
    }

    public bool ApplyAndRestart(UpdateCheckResult update)
    {
        if (update?.Payload is not UpdateInfo info) return false;
        try
        {
            _manager.ApplyUpdatesAndRestart(info.TargetFullRelease);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error("更新の適用に失敗しました。", ex);
            return false;
        }
    }
}
