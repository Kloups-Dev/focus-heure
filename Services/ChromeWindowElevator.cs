using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FocusHeure.Services;

/// <summary>
/// Garde l'overlay focus visible et place uniquement les fenêtres Chrome au-dessus.
/// Repousse les autres applications sous l'overlay.
/// </summary>
public static class ChromeWindowElevator
{
    private const uint SwpNomove = 0x0002;
    private const uint SwpNosize = 0x0001;
    private const uint SwpNoactivate = 0x0010;
    private const uint SwpShowwindow = 0x0040;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNotTopmost = new(-2);

    private static int _ourProcessId = Process.GetCurrentProcess().Id;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    private const uint GwOwner = 4;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static void Apply(IReadOnlyList<Window> overlayWindows, IReadOnlyList<string> allowedProcessNames)
    {
        var overlayHandles = GetOverlayHandles(overlayWindows);
        if (overlayHandles.Count == 0)
            return;

        var allowedPids = allowedProcessNames
            .SelectMany(name => Process.GetProcessesByName(name).Select(p => p.Id))
            .ToHashSet();

        if (allowedPids.Count == 0)
            return;

        var overlaySet = overlayHandles.ToHashSet();
        var allowedWindows = new List<IntPtr>();

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || overlaySet.Contains(hwnd))
                return true;

            if (GetWindow(hwnd, GwOwner) != IntPtr.Zero)
                return true;

            GetWindowThreadProcessId(hwnd, out var pid);
            if (allowedPids.Contains((int)pid))
                allowedWindows.Add(hwnd);

            return true;
        }, IntPtr.Zero);

        foreach (var overlay in overlayHandles)
            SetTopmost(overlay);

        foreach (var window in allowedWindows)
            SetTopmost(window);

        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
            return;

        GetWindowThreadProcessId(foreground, out var fgPid);
        var fgIsAllowed = allowedPids.Contains((int)fgPid);
        var fgIsUs = (int)fgPid == _ourProcessId || overlaySet.Contains(foreground);

        if (!fgIsAllowed && !fgIsUs)
        {
            SetWindowPos(foreground, HwndNotTopmost, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpNoactivate);

            foreach (var overlay in overlayHandles)
                SetTopmost(overlay);

            foreach (var window in allowedWindows)
                SetTopmost(window);
        }
    }

    private static void SetTopmost(IntPtr hwnd) =>
        SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpNoactivate | SwpShowwindow);

    private static List<IntPtr> GetOverlayHandles(IReadOnlyList<Window> windows)
    {
        var list = new List<IntPtr>();
        foreach (var w in windows)
        {
            var handle = new WindowInteropHelper(w).Handle;
            if (handle != IntPtr.Zero)
                list.Add(handle);
        }

        return list;
    }
}
