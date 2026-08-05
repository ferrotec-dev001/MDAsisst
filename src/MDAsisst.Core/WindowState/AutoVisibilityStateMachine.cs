using MDAsisst.Core.Settings;

namespace MDAsisst.Core.WindowState;

/// <summary>ウィンドウの表示状態。</summary>
public enum VisibilityState
{
    /// <summary>通常表示（半透明ウィンドウ）。</summary>
    Expanded,
    /// <summary>画面隅の最小アイコン表示。</summary>
    Minimized,
    /// <summary>タスクトレイへ格納済み（画面上に何も出さない）。</summary>
    TrayHidden
}

/// <summary>時刻取得の抽象化。テストで時間を進められるようにする。</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();
    private SystemClock() { }
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// 編集時の自動展開／非編集時の自動最小化を判定する状態機械（FR-WN-11〜13）。
/// UI に依存せず、Tick() の戻り値として遷移後の状態を返すだけにしてある。
/// </summary>
public sealed class AutoVisibilityStateMachine
{
    private readonly IClock _clock;
    private BehaviorSettings _settings;
    private DateTimeOffset _lastActivityUtc;
    private DateTimeOffset? _expandRequestedUtc;

    public VisibilityState State { get; private set; }

    public AutoVisibilityStateMachine(BehaviorSettings settings, IClock? clock = null,
        VisibilityState initialState = VisibilityState.Expanded)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _clock = clock ?? SystemClock.Instance;
        State = initialState;
        _lastActivityUtc = _clock.UtcNow;
    }

    /// <summary>設定変更を反映する（設定画面での即時反映用）。</summary>
    public void UpdateSettings(BehaviorSettings settings)
        => _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    /// <summary>キー入力・マウス操作など「編集中」を示す操作を通知する。</summary>
    public void NotifyActivity()
    {
        _lastActivityUtc = _clock.UtcNow;

        if (State == VisibilityState.Minimized)
            _expandRequestedUtc ??= _lastActivityUtc;
    }

    /// <summary>トレイへ格納する。自動遷移の対象外になる。</summary>
    public void HideToTray()
    {
        State = VisibilityState.TrayHidden;
        _expandRequestedUtc = null;
    }

    /// <summary>トレイやユーザー操作から明示的に復帰する。</summary>
    public void ShowFromTray()
    {
        State = VisibilityState.Expanded;
        _expandRequestedUtc = null;
        _lastActivityUtc = _clock.UtcNow;
    }

    /// <summary>周期呼び出し。遷移が起きた場合のみ true を返す。</summary>
    public bool Tick(out VisibilityState state)
    {
        var previous = State;
        var now = _clock.UtcNow;

        switch (State)
        {
            case VisibilityState.Expanded:
                // 0 秒は「自動最小化しない」を意味する（境界値: 0 と負値）。
                var minimizeAfter = _settings.AutoMinimizeDelaySeconds;
                if (minimizeAfter > 0 && (now - _lastActivityUtc).TotalSeconds >= minimizeAfter)
                    State = VisibilityState.Minimized;
                break;

            case VisibilityState.Minimized:
                if (_expandRequestedUtc is { } requested)
                {
                    var expandAfter = Math.Max(0, _settings.AutoExpandDelaySeconds);
                    if ((now - requested).TotalSeconds >= expandAfter)
                    {
                        State = VisibilityState.Expanded;
                        _expandRequestedUtc = null;
                        _lastActivityUtc = now;
                    }
                }
                break;

            case VisibilityState.TrayHidden:
                // トレイ格納中は自動遷移しない。
                break;
        }

        state = State;
        return previous != State;
    }
}
