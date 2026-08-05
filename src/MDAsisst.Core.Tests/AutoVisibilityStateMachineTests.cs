using MDAsisst.Core.Settings;
using MDAsisst.Core.WindowState;
using Xunit;

namespace MDAsisst.Core.Tests;

/// <summary>テスト用に時間を任意に進められる時計。</summary>
internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = new(2026, 8, 5, 9, 0, 0, TimeSpan.Zero);
    public void Advance(int seconds) => UtcNow = UtcNow.AddSeconds(seconds);
}

public class AutoVisibilityStateMachineTests
{
    private static (AutoVisibilityStateMachine Machine, FakeClock Clock, BehaviorSettings Settings) Create(
        int minimizeAfter = 30, int expandAfter = 0)
    {
        var settings = new BehaviorSettings
        {
            AutoMinimizeDelaySeconds = minimizeAfter,
            AutoExpandDelaySeconds = expandAfter
        };
        var clock = new FakeClock();
        return (new AutoVisibilityStateMachine(settings, clock), clock, settings);
    }

    [Fact]
    public void 初期状態は展開()
    {
        var (m, _, _) = Create();
        Assert.Equal(VisibilityState.Expanded, m.State);
    }

    [Fact]
    public void 無操作が設定秒数に達すると最小化する()
    {
        var (m, clock, _) = Create(minimizeAfter: 30);

        clock.Advance(29);
        Assert.False(m.Tick(out var s1));
        Assert.Equal(VisibilityState.Expanded, s1);

        clock.Advance(1);   // ちょうど 30 秒（境界値）
        Assert.True(m.Tick(out var s2));
        Assert.Equal(VisibilityState.Minimized, s2);
    }

    [Fact]
    public void 遅延0秒なら自動最小化しない()
    {
        var (m, clock, _) = Create(minimizeAfter: 0);
        clock.Advance(100000);
        Assert.False(m.Tick(out var s));
        Assert.Equal(VisibilityState.Expanded, s);
    }

    [Fact]
    public void 操作すると最小化タイマーがリセットされる()
    {
        var (m, clock, _) = Create(minimizeAfter: 30);
        clock.Advance(29);
        m.NotifyActivity();
        clock.Advance(29);
        Assert.False(m.Tick(out _));
        Assert.Equal(VisibilityState.Expanded, m.State);
    }

    [Fact]
    public void 最小化中に入力すると即時展開する()
    {
        var (m, clock, _) = Create(minimizeAfter: 10, expandAfter: 0);
        clock.Advance(10);
        m.Tick(out _);
        Assert.Equal(VisibilityState.Minimized, m.State);

        m.NotifyActivity();
        Assert.True(m.Tick(out var s));
        Assert.Equal(VisibilityState.Expanded, s);
    }

    [Fact]
    public void 展開遅延が設定されている場合は経過後に展開する()
    {
        var (m, clock, _) = Create(minimizeAfter: 10, expandAfter: 3);
        clock.Advance(10);
        m.Tick(out _);

        m.NotifyActivity();
        clock.Advance(2);
        Assert.False(m.Tick(out _));
        Assert.Equal(VisibilityState.Minimized, m.State);

        clock.Advance(1);
        Assert.True(m.Tick(out var s));
        Assert.Equal(VisibilityState.Expanded, s);
    }

    [Fact]
    public void トレイ格納中は自動遷移しない()
    {
        var (m, clock, _) = Create(minimizeAfter: 5);
        m.HideToTray();
        clock.Advance(600);
        m.NotifyActivity();
        Assert.False(m.Tick(out var s));
        Assert.Equal(VisibilityState.TrayHidden, s);
    }

    [Fact]
    public void トレイから復帰すると展開状態になる()
    {
        var (m, _, _) = Create();
        m.HideToTray();
        m.ShowFromTray();
        Assert.Equal(VisibilityState.Expanded, m.State);
    }

    [Fact]
    public void 設定変更は即座に反映される()
    {
        var (m, clock, settings) = Create(minimizeAfter: 0);
        settings.AutoMinimizeDelaySeconds = 5;
        m.UpdateSettings(settings);

        clock.Advance(5);
        Assert.True(m.Tick(out var s));
        Assert.Equal(VisibilityState.Minimized, s);
    }
}
