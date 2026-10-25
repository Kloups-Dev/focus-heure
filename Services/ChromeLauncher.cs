using System.Diagnostics;
using System.IO;
using FocusHeure.Models;

namespace FocusHeure.Services;

public static class ChromeLauncher
{
    private static readonly string[] DefaultPaths =
    [
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Google\Chrome\Application\chrome.exe"),
    ];

    public static bool TryLaunch(AppSettings settings, out Process? process)
    {
        process = null;
        if (!settings.ChromeEnabled)
            return false;

        var path = string.IsNullOrWhiteSpace(settings.ChromePath)
            ? DefaultPaths.FirstOrDefault(File.Exists)
            : settings.ChromePath;

        if (path is null || !File.Exists(path))
            return false;

        process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        return process is not null;
    }

    public static bool IsChromeRunning() =>
        Process.GetProcessesByName("chrome").Length > 0;
}
