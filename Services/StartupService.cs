using System.Diagnostics;

namespace FocusHeure.Services;

public static class StartupService
{
    private const string TaskName = "FocusHeure";

    public static bool IsEnabled
    {
        get
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("schtasks.exe", $"/Query /TN {TaskName}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
    }

    public static void SetEnabled(bool enabled, string executablePath)
    {
        var arguments = enabled
            ? $"/Create /TN {TaskName} /TR \"{executablePath}\" /SC ONLOGON /RL HIGHEST /IT /F"
            : $"/Delete /TN {TaskName} /F";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("schtasks.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Impossible de {(enabled ? "créer" : "supprimer")} la tâche de démarrage.");
    }
}
