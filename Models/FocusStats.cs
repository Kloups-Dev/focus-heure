namespace FocusHeure.Models;

public sealed class FocusStats
{
    public long TotalFocusMinutes { get; set; }
    public List<FocusSessionEntry> Sessions { get; set; } = [];

    public int SessionCount => Sessions.Count;
}

public sealed class FocusSessionEntry
{
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int Minutes { get; set; }
}
