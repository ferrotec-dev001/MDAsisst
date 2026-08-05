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
/// </summary>
public interface IUpdateService
{
    /// <summary>Velopack 管理下でインストールされているか。開発実行時は false。</summary>
    bool IsInstalled { get; }

    /// <summary>現在のバージョン表記。</summary>
    string CurrentVersion { get; }

    Task<UpdateCheckResult?> CheckAsync(CancellationToken ct = default);

    Task<bool> DownloadAsync(UpdateCheckResult update, IProgress<int>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// ダウンロード済み更新を今すぐ適用して再起動する。
    /// ADR-0009: 更新の適用は必ずユーザーの同意（確認ダイアログ）を得た直後にこのメソッドを
    /// 呼び出す形で行う。無人・バックグラウンドでの自動適用（旧 ApplyOnExit）は提供しない。
    /// </summary>
    bool ApplyAndRestart(UpdateCheckResult update);
}
