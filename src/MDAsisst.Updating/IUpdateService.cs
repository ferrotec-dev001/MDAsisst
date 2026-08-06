namespace MDAsisst.Updating;

/// <summary>更新確認の結果。</summary>
/// <param name="Version">利用可能な新バージョン。</param>
/// <param name="ReleaseNotesUrl">リリースノートの URL（無い場合は null）。</param>
public sealed record UpdateCheckResult(string Version, string? ReleaseNotesUrl)
{
    /// <summary>Velopack の UpdateInfo など、適用時に必要な実体を保持する。</summary>
    public object? Payload { get; init; }
}

/// <summary>
/// 更新機能の抽象。オフライン・API 制限・未インストール実行など、
/// 失敗しても例外を投げず null / false を返す契約とする（FR-ST-08）。
///
/// ADR-0011: v0.3.1 のオンライン自動更新（ダウンロード・アプリ自身による
/// Program Files 配下の DLL 自己書き換え）が、Program Files への配置に
/// 移行済み（ADR-0009）にもかかわらず EDR に検知される事例が判明した。
/// これを受け、アプリ自身が更新ファイルを取得・適用する経路を廃止し、
/// 「新バージョンの有無を確認して通知するだけ」の機能に縮小する。
/// 実際の更新はユーザー（またはIT部門）が配布された MSI を手動で
/// 再インストールする運用に統一する（DownloadAsync / ApplyAndRestart は撤去）。
/// </summary>
public interface IUpdateService
{
    /// <summary>Velopack 管理下でインストールされているか。開発実行時は false。</summary>
    bool IsInstalled { get; }

    /// <summary>現在のバージョン表記。</summary>
    string CurrentVersion { get; }

    Task<UpdateCheckResult?> CheckAsync(CancellationToken ct = default);
}
