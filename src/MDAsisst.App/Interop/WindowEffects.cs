using System.Windows;
using System.Windows.Interop;
using MDAsisst.Core.Settings;

namespace MDAsisst.App.Interop;

/// <summary>
/// 半透明表示とウィンドウ配置。
/// ADR-0004 のとおり AllowsTransparency は使わず、WS_EX_LAYERED + LWA_ALPHA で
/// ClearType と入力性能を維持したまま透過度を可変にする。
/// </summary>
internal static class WindowEffects
{
    /// <summary>ウィンドウ全体の不透明度を設定する（0.0〜1.0）。</summary>
    public static void SetOpacity(Window window, double opacity)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;   // SourceInitialized 前は何もしない

        var ex = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        if ((ex & NativeMethods.WS_EX_LAYERED) == 0)
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, ex | NativeMethods.WS_EX_LAYERED);

        var alpha = (byte)Math.Clamp(opacity * 255.0, 0, 255);
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, alpha, NativeMethods.LWA_ALPHA);
    }

    /// <summary>Alt+Tab の一覧に出さない（常駐ツールとしての作法）。</summary>
    public static void HideFromAltTab(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;
        var ex = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, ex | NativeMethods.WS_EX_TOOLWINDOW);
    }

    /// <summary>
    /// ウィンドウが載っているモニタの作業領域を DIP で返す。
    /// SystemParameters.WorkArea はプライマリのみのためマルチモニタで破綻する（FR-WN-16）。
    /// </summary>
    public static Rect GetWorkAreaDip(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return new Rect(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Top,
                            SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);

        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var mi = new NativeMethods.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref mi))
            return new Rect(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Top,
                            SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);

        var dpi = NativeMethods.GetDpiForWindow(hwnd);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;

        return new Rect(
            mi.rcWork.Left / scale,
            mi.rcWork.Top / scale,
            (mi.rcWork.Right - mi.rcWork.Left) / scale,
            (mi.rcWork.Bottom - mi.rcWork.Top) / scale);
    }

    /// <summary>指定した隅へウィンドウを寄せる（FR-WN-12）。</summary>
    public static void SnapToCorner(Window window, ScreenCorner corner, double width, double height, double margin = 12)
    {
        var area = GetWorkAreaDip(window);
        var (left, top) = corner switch
        {
            ScreenCorner.TopLeft => (area.Left + margin, area.Top + margin),
            ScreenCorner.TopRight => (area.Right - width - margin, area.Top + margin),
            ScreenCorner.BottomLeft => (area.Left + margin, area.Bottom - height - margin),
            _ => (area.Right - width - margin, area.Bottom - height - margin)
        };
        window.Left = left;
        window.Top = top;
    }

    /// <summary>保存された位置が画面外だった場合に作業領域内へ引き戻す（FR-WN-15）。</summary>
    public static void EnsureVisible(Window window)
    {
        var area = GetWorkAreaDip(window);
        if (window.Width > area.Width) window.Width = area.Width;
        if (window.Height > area.Height) window.Height = area.Height;
        if (window.Left < area.Left || window.Left + window.Width > area.Right)
            window.Left = Math.Max(area.Left, area.Right - window.Width);
        if (window.Top < area.Top || window.Top + window.Height > area.Bottom)
            window.Top = Math.Max(area.Top, area.Bottom - window.Height);
    }
}
