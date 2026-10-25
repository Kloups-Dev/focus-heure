using FocusHeure.Models;
using FocusHeure.Services;
using Xunit;

namespace FocusHeure.Tests;

public sealed class RestrictionScheduleTests
{
    [Fact]
    public void OvernightWindowIncludesTheTimeAfterMidnight()
    {
        var settings = new AppSettings
        {
            Windows = [new TimeWindowDto { Start = "22:00", End = "02:00" }],
        };
        var schedule = new RestrictionSchedule(() => settings);

        Assert.True(schedule.IsRestricted(new DateTime(2026, 1, 2, 23, 30, 0)));
        Assert.True(schedule.IsRestricted(new DateTime(2026, 1, 3, 1, 30, 0)));
        Assert.False(schedule.IsRestricted(new DateTime(2026, 1, 3, 2, 0, 0)));
    }

    [Fact]
    public void OvernightWindowReportsRemainingTimeUntilTheNextDay()
    {
        var settings = new AppSettings
        {
            Windows = [new TimeWindowDto { Start = "22:00", End = "02:00" }],
        };
        var schedule = new RestrictionSchedule(() => settings);

        var remaining = schedule.GetRemainingInBlock(new DateTime(2026, 1, 3, 1, 30, 0));

        Assert.Equal(TimeSpan.FromMinutes(30), remaining);
    }

    [Fact]
    public void EqualStartAndEndIsRejected()
    {
        var window = new TimeWindowDto { Start = "10:00", End = "10:00" };

        Assert.False(window.IsValid());
    }
}
