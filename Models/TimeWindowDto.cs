using System.Text.Json.Serialization;

namespace FocusHeure.Models;

public sealed class TimeWindowDto
{
    private string _start = "00:00";
    private string _end = "08:00";

    public string Start
    {
        get => _start;
        set
        {
            _start = value;
            Parse();
        }
    }

    public string End
    {
        get => _end;
        set
        {
            _end = value;
            Parse();
        }
    }

    public bool Enabled { get; set; } = true;

    [JsonIgnore]
    public TimeOnly? StartTime { get; private set; }

    [JsonIgnore]
    public TimeOnly? EndTime { get; private set; }

    public TimeWindowDto() => Parse();

    private void Parse()
    {
        StartTime = TimeOnly.TryParse(_start, out var s) ? s : null;
        EndTime = TimeOnly.TryParse(_end, out var e) ? e : null;
    }

    public bool IsValid() => StartTime is not null && EndTime is not null;
}
