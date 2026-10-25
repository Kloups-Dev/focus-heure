namespace FocusHeure.Models;

public sealed class RestrictionSchedule
{
    private readonly Func<AppSettings> _getSettings;

    public RestrictionSchedule(Func<AppSettings> getSettings) => _getSettings = getSettings;

    public bool IsRestricted(DateTime now)
    {
        var settings = _getSettings();

        if (settings.SkipUntil is { } skip && now < skip)
            return false;

        if (settings.WeekendExempt && now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        return IsRestricted(TimeOnly.FromDateTime(now), settings);
    }

    public bool IsRestricted(TimeOnly time, AppSettings? settings = null)
    {
        settings ??= _getSettings();

        foreach (var window in settings.Windows)
        {
            if (!window.Enabled || window.StartTime is null || window.EndTime is null)
                continue;

            var start = window.StartTime.Value;
            var end = window.EndTime.Value;

            if (time >= start && time < end)
                return true;
        }

        return false;
    }

    public (TimeOnly Start, TimeOnly End)? GetCurrentWindow(TimeOnly time, AppSettings? settings = null)
    {
        settings ??= _getSettings();

        foreach (var window in settings.Windows)
        {
            if (!window.Enabled || window.StartTime is null || window.EndTime is null)
                continue;

            var start = window.StartTime.Value;
            var end = window.EndTime.Value;

            if (time >= start && time < end)
                return (start, end);
        }

        return null;
    }

    public string? CurrentBlockLabel(DateTime now)
    {
        var w = GetCurrentWindow(TimeOnly.FromDateTime(now));
        return w is null ? null : $"{w.Value.Start:HH\\:mm} – {w.Value.End:HH\\:mm}";
    }

    public TimeSpan? GetRemainingInBlock(DateTime now)
    {
        var w = GetCurrentWindow(TimeOnly.FromDateTime(now));
        if (w is null)
            return null;

        var endToday = DateOnly.FromDateTime(now).ToDateTime(w.Value.End);
        var remaining = endToday - now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public DateTime? GetNextBlockStart(DateTime now)
    {
        var settings = _getSettings();
        var today = DateOnly.FromDateTime(now);
        var candidates = new List<DateTime>();

        for (var offset = 0; offset < 14; offset++)
        {
            var day = today.AddDays(offset);
            if (settings.WeekendExempt && day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            foreach (var window in settings.Windows)
            {
                if (!window.Enabled || window.StartTime is null || window.EndTime is null)
                    continue;

                var start = window.StartTime.Value;

                var dt = day.ToDateTime(start);
                if (dt > now && !WouldSkip(settings, dt))
                    candidates.Add(dt);
            }
        }

        return candidates.OrderBy(t => t).FirstOrDefault();
    }

    public DateTime GetNextTransition(DateTime now)
    {
        var settings = _getSettings();
        var today = DateOnly.FromDateTime(now);
        var candidates = new List<DateTime>();

        void AddDayTransitions(DateOnly day)
        {
            if (settings.WeekendExempt && day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return;

            foreach (var window in settings.Windows)
            {
                if (!window.Enabled || window.StartTime is null || window.EndTime is null)
                    continue;

                var start = window.StartTime.Value;
                var end = window.EndTime.Value;

                var startDt = day.ToDateTime(start);
                var endDt = day.ToDateTime(end);
                if (!WouldSkip(settings, startDt))
                    candidates.Add(startDt);
                if (!WouldSkip(settings, endDt))
                    candidates.Add(endDt);
            }
        }

        AddDayTransitions(today);
        AddDayTransitions(today.AddDays(1));

        if (settings.SkipUntil is { } skip && skip > now)
            candidates.Add(skip);

        var next = candidates.Where(t => t > now).OrderBy(t => t).FirstOrDefault();
        return next == default ? now.AddMinutes(1) : next;
    }

    public DateTime? GetNextNotifyTime(DateTime now)
    {
        var settings = _getSettings();
        if (settings.NotifyBeforeMinutes <= 0)
            return null;

        var nextStart = GetNextBlockStart(now);
        if (nextStart is null)
            return null;

        return nextStart.Value.AddMinutes(-settings.NotifyBeforeMinutes);
    }

    private static bool WouldSkip(AppSettings settings, DateTime moment)
    {
        if (settings.SkipUntil is { } skip && moment < skip)
            return true;

        if (settings.WeekendExempt && moment.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return true;

        return false;
    }
}
