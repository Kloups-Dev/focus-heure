using System.IO;
using System.Text.Json;
using FocusHeure.Models;

namespace FocusHeure.Services;

public sealed class StatsService
{
    public static StatsService Instance { get; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string StatsPath =>
        Path.Combine(SettingsService.Instance.SettingsDirectory, "stats.json");

    public FocusStats Data { get; private set; } = new();

    private DateTime? _sessionStart;

    public void Load()
    {
        try
        {
            if (!File.Exists(StatsPath))
            {
                Data = new FocusStats();
                return;
            }

            Data = JsonSerializer.Deserialize<FocusStats>(File.ReadAllText(StatsPath), JsonOptions)
                   ?? new FocusStats();
        }
        catch
        {
            var backup = StatsPath + ".bak";
            try
            {
                Directory.CreateDirectory(SettingsService.Instance.SettingsDirectory);
                if (File.Exists(StatsPath))
                {
                    File.Delete(backup);
                    File.Move(StatsPath, backup);
                }
            }
            catch { }

            Logger.Log($"Stats corrompues ; backup vers {backup}");
            Data = new FocusStats();
        }
    }

    public void BeginSession()
    {
        _sessionStart ??= DateTime.Now;
    }

    public void EndSession()
    {
        if (_sessionStart is null)
            return;

        var end = DateTime.Now;
        var minutes = Math.Max(1, (int)Math.Round((end - _sessionStart.Value).TotalMinutes));
        Data.Sessions.Add(new FocusSessionEntry
        {
            StartedAt = _sessionStart.Value,
            EndedAt = end,
            Minutes = minutes,
        });
        Data.TotalFocusMinutes += minutes;

        while (Data.Sessions.Count > 500)
            Data.Sessions.RemoveAt(0);

        _sessionStart = null;
        Save();
    }

    private void Save()
    {
        Directory.CreateDirectory(SettingsService.Instance.SettingsDirectory);
        File.WriteAllText(StatsPath, JsonSerializer.Serialize(Data, JsonOptions));
    }
}
