using System.Windows.Threading;

namespace MDAsisst.App.Services;

/// <summary>連続入力中の再描画を抑えるデバウンス（FR-PV-01, NFR-04）。</summary>
public sealed class DebounceDispatcher : IDisposable
{
    private readonly DispatcherTimer _timer;
    private Action? _action;

    public DebounceDispatcher(Dispatcher dispatcher)
        => _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher);

    public void Debounce(int milliseconds, Action action)
    {
        _action = action;
        _timer.Stop();

        if (milliseconds <= 0)
        {
            action();       // 0ms は「即時反映」を意味する（境界値）
            return;
        }

        _timer.Interval = TimeSpan.FromMilliseconds(milliseconds);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        _action?.Invoke();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }
}
