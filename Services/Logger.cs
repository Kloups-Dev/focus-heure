using System.IO;

namespace FocusHeure.Services;

public static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FocusHeure",
        "log.txt");

    private static readonly object Sync = new();

    public static void Log(string message)
    {
        lock (Sync)
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir is not null)
                Directory.CreateDirectory(dir);

            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
