using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace FocusHeure.Helpers;

public static class MonitorHelper
{
    private enum MonitorDpiType { Effective = 0 }

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public static void PlaceFullscreen(Window window, Screen screen)
    {
        var bounds = screen.Bounds;
        var scale = GetDpiScale(screen);

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.Topmost = true;
        window.ShowInTaskbar = false;
        window.Left = bounds.Left / scale;
        window.Top = bounds.Top / scale;
        window.Width = bounds.Width / scale;
        window.Height = bounds.Height / scale;
    }

    private static double GetDpiScale(Screen screen)
    {
        var center = new POINT
        {
            X = screen.Bounds.Left + screen.Bounds.Width / 2,
            Y = screen.Bounds.Top + screen.Bounds.Height / 2,
        };

        var monitor = MonitorFromPoint(center, 2 /* MONITOR_DEFAULTTONEAREST */);
        if (monitor != IntPtr.Zero &&
            GetDpiForMonitor(monitor, MonitorDpiType.Effective, out var dpiX, out _) == 0 &&
            dpiX > 0)
        {
            return dpiX / 96.0;
        }

        return 1.0;
    }
}
