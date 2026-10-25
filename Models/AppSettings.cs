namespace FocusHeure.Models;

public sealed class AppSettings
{
    public List<TimeWindowDto> Windows { get; set; } =
    [
        new() { Start = "00:00", End = "08:00" },
        new() { Start = "12:00", End = "13:00" },
        new() { Start = "17:00", End = "18:00" },
        new() { Start = "19:30", End = "20:30" },
    ];

    public int PauseMinutes { get; set; } = 15;
    public int NotifyBeforeMinutes { get; set; } = 2;
    public bool WeekendExempt { get; set; }
    public bool ShowCountdown { get; set; } = true;
    public bool ShowChromeButton { get; set; } = true;
    public bool ChromeEnabled { get; set; } = true;
    public string? ChromePath { get; set; }
    public List<string> AllowedProcesses { get; set; } = ["chrome"];
    public string CustomMessage { get; set; } = "Mode concentration";
    public string? PinHash { get; set; }
    public bool RunAtStartup { get; set; } = true;
    public DateTime? SkipUntil { get; set; }

    public TimeSpan PauseDuration => TimeSpan.FromMinutes(Math.Clamp(PauseMinutes, 1, 180));

    public bool HasPin => !string.IsNullOrWhiteSpace(PinHash);
}
