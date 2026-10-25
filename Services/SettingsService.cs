using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FocusHeure.Models;

namespace FocusHeure.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static SettingsService Instance { get; } = new();

    public string SettingsDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusHeure");

    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public AppSettings Current { get; private set; } = new();

    public event Action? SettingsChanged;

    private SettingsService() { }

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                Current = new AppSettings();
                Save();
                return;
            }

            var json = File.ReadAllText(SettingsPath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            var backup = SettingsPath + ".bak";
            try
            {
                if (File.Exists(SettingsPath))
                {
                    File.Delete(backup);
                    File.Move(SettingsPath, backup);
                }
            }
            catch { }

            Logger.Log($"Paramètres corrompus ; backup vers {backup}");
            Current = new AppSettings();
            Save();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(SettingsPath, json);
        SettingsChanged?.Invoke();
    }

    public void Update(Action<AppSettings> mutate)
    {
        mutate(Current);
        Save();
    }

    public static string HashPin(string pin)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pin.Trim()));
        return Convert.ToHexString(bytes);
    }

    public static bool VerifyPin(string pin, string? hash) =>
        !string.IsNullOrWhiteSpace(hash) &&
        HashPin(pin).Equals(hash, StringComparison.OrdinalIgnoreCase);
}
