using System.Windows.Threading;
using FocusHeure.Models;
using Microsoft.Win32;

namespace FocusHeure.Services;

public sealed class FocusSessionController : IDisposable
{
    public static readonly TimeSpan ChromeElevateInterval = TimeSpan.FromMilliseconds(350);
    public static readonly TimeSpan TestOverlayDuration = TimeSpan.FromSeconds(30);

    private readonly RestrictionSchedule _schedule;
    private readonly OverlayManager _overlays = new();
    private readonly PauseHotkeyService _pauseHotkey = new();
    private readonly DispatcherTimer _scheduleTimer = new();
    private readonly DispatcherTimer _clockTimer = new();
    private readonly DispatcherTimer _pauseTimer = new();
    private readonly DispatcherTimer _chromeTimer = new();

    private bool _paused;
    private bool _chromeElevated;
    private bool _forceLock;
    private DateTime? _testModeUntil;
    private DateTime? _notifiedForBlockStart;
    private EventHandler? _displayChangedHandler;

    public event Action<string>? StatusChanged;
    public event Action<bool>? LockStateChanged;
    public event Action<string>? NotifyRequested;

    public bool IsOverlayVisible => _overlays.IsVisible;
    public bool IsInRestriction => ShouldBeRestricted(DateTime.Now);
    public bool IsChromeElevated => _chromeElevated;

    public FocusSessionController()
    {
        _schedule = new RestrictionSchedule(() => SettingsService.Instance.Current);

        _scheduleTimer.Tick += (_, _) => EvaluateSchedule();
        _clockTimer.Interval = TimeSpan.FromSeconds(1);
        _clockTimer.Tick += (_, _) => UpdateClocks();

        _pauseTimer.Tick += (_, _) => EndPause();
        _chromeTimer.Tick += (_, _) => OnChromeTimerTick();

        _pauseHotkey.PauseRequested += TogglePause;
        _overlays.OnChromeClick = ToggleChromeElevation;

        SettingsService.Instance.SettingsChanged += OnSettingsChanged;

        _displayChangedHandler = (_, _) =>
        {
            if (_overlays.IsVisible)
                _overlays.RefreshMonitors();
        };
        SystemEvents.DisplaySettingsChanged += _displayChangedHandler;
    }

    public void Start()
    {
        Logger.Log("FocusSessionController démarré");
        ScheduleNextCheck();
        EvaluateSchedule();
    }

    public void ReloadSettings() => EvaluateSchedule();

    public void TestOverlay()
    {
        _testModeUntil = DateTime.Now.Add(TestOverlayDuration);
        ShowOverlays();
        StatusChanged?.Invoke("Test écran (30 s)");
        ScheduleNextCheck();
    }

    public void ForceLockNow()
    {
        _forceLock = true;
        EvaluateSchedule();
    }

    public void SkipToday()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.Now).AddDays(1).ToDateTime(TimeOnly.MinValue);
        SettingsService.Instance.Update(s => s.SkipUntil = tomorrow);
        _forceLock = false;
        EndPause();
        StopChromeElevation();
        StopLockUi();
        StatusChanged?.Invoke("Jour libre jusqu'à demain");
        ScheduleNextCheck();
    }

    public string GetStatusDetail()
    {
        var now = DateTime.Now;
        if (_testModeUntil > now)
            return "Test écran actif";

        if (_overlays.IsVisible && _chromeElevated)
            return "Mode focus actif — Chrome au-dessus";

        if (_overlays.IsVisible)
            return "Mode concentration actif";

        if (_paused)
            return "En pause";

        if (_schedule.IsRestricted(now))
            return "Créneau actif (pause)";

        var next = _schedule.GetNextBlockStart(now);
        if (next is null)
            return "Inactif — aucun créneau";

        var delta = next.Value - now;
        if (delta.TotalHours >= 1)
            return $"Libre — prochain : {next:HH:mm} (dans {(int)delta.TotalHours}h{delta.Minutes:D2})";

        return $"Libre — prochain : {next:HH:mm} (dans {delta.Minutes} min)";
    }

    private void OnSettingsChanged()
    {
        _pauseTimer.Interval = SettingsService.Instance.Current.PauseDuration;
        EvaluateSchedule();
    }

    private bool ShouldBeRestricted(DateTime now)
    {
        if (_testModeUntil > now)
            return true;

        if (_testModeUntil <= now && _testModeUntil is not null)
        {
            _testModeUntil = null;
            if (!_schedule.IsRestricted(now) && !_forceLock)
                return false;
        }

        if (_forceLock)
            return true;

        return _schedule.IsRestricted(now);
    }

    private void EvaluateSchedule()
    {
        var now = DateTime.Now;
        var inRestriction = ShouldBeRestricted(now);

        CheckUpcomingNotification(now);

        if (inRestriction && !_pauseHotkey.IsRunning)
            _pauseHotkey.Start();
        else if (!inRestriction)
        {
            _pauseHotkey.Stop();
            _paused = false;
            _forceLock = false;
            StopChromeElevation();
            _pauseTimer.Stop();
        }

        var shouldLock = inRestriction && !_paused;

        if (shouldLock && !_overlays.IsVisible)
        {
            ShowOverlays();
            StatsService.Instance.BeginSession();
            StatusChanged?.Invoke(_chromeElevated
                ? "Mode focus — Chrome au-dessus"
                : "Actif — mode concentration");
            LockStateChanged?.Invoke(true);
        }
        else if (!shouldLock && _overlays.IsVisible)
        {
            StopChromeElevation();
            StatsService.Instance.EndSession();
            StopLockUi();
            StatusChanged?.Invoke(GetStatusDetail());
            LockStateChanged?.Invoke(false);
        }
        else if (_paused)
        {
            StopChromeElevation();
            var min = SettingsService.Instance.Current.PauseMinutes;
            StatusChanged?.Invoke($"Pause — Échap+Entrée pour reprendre (max {min} min)");
            LockStateChanged?.Invoke(false);
        }
        else if (_overlays.IsVisible)
        {
            UpdateClocks();
            StatusChanged?.Invoke(GetStatusDetail());
        }
        else
        {
            StatusChanged?.Invoke(GetStatusDetail());
        }

        ScheduleNextCheck();
    }

    private void CheckUpcomingNotification(DateTime now)
    {
        var settings = SettingsService.Instance.Current;
        if (settings.NotifyBeforeMinutes <= 0 || _schedule.IsRestricted(now))
            return;

        var notifyAt = _schedule.GetNextNotifyTime(now);
        if (notifyAt is null || now < notifyAt)
            return;

        var nextStart = _schedule.GetNextBlockStart(now);
        if (nextStart is null)
            return;

        if (_notifiedForBlockStart == nextStart)
            return;

        _notifiedForBlockStart = nextStart;
        NotifyRequested?.Invoke($"Créneau focus dans {settings.NotifyBeforeMinutes} min ({nextStart:HH:mm})");
    }

    private void ScheduleNextCheck()
    {
        var now = DateTime.Now;
        var candidates = new List<DateTime> { _schedule.GetNextTransition(now) };

        if (_testModeUntil > now)
            candidates.Add(_testModeUntil.Value);

        var notifyAt = _schedule.GetNextNotifyTime(now);
        if (notifyAt > now)
            candidates.Add(notifyAt.Value);

        var next = candidates.Where(t => t > now).Min();
        var delay = next - now;
        if (delay < TimeSpan.FromMilliseconds(400))
            delay = TimeSpan.FromMilliseconds(400);

        _scheduleTimer.Interval = delay;
        if (!_scheduleTimer.IsEnabled)
            _scheduleTimer.Start();
    }

    private void ShowOverlays()
    {
        _overlays.ShowAll();
        _clockTimer.Start();
        UpdateClocks();
    }

    private void UpdateClocks()
    {
        if (!_overlays.IsVisible)
            return;

        var now = DateTime.Now;
        var settings = SettingsService.Instance.Current;
        var time = now.ToString("HH:mm");
        var label = _schedule.CurrentBlockLabel(now);
        var remaining = settings.ShowCountdown ? _schedule.GetRemainingInBlock(now) : null;
        var countdown = remaining is null
            ? null
            : remaining.Value.TotalHours >= 1
                ? $"{(int)remaining.Value.TotalHours}h {remaining.Value.Minutes:D2}m restantes"
                : $"{remaining.Value.Minutes} min restantes";

        _overlays.UpdateDisplays(w => w.UpdateDisplay(time, label, countdown, settings, _chromeElevated));
    }

    private void TogglePause()
    {
        if (!ShouldBeRestricted(DateTime.Now))
            return;

        if (_paused)
        {
            EndPause();
            return;
        }

        StopChromeElevation();
        _paused = true;
        var duration = SettingsService.Instance.Current.PauseDuration;
        _pauseTimer.Stop();
        _pauseTimer.Interval = duration;
        _pauseTimer.Start();
        StatsService.Instance.EndSession();
        _overlays.HideAll();
        _clockTimer.Stop();
        StatusChanged?.Invoke("Pause — Échap+Entrée pour reprendre");
        LockStateChanged?.Invoke(false);
    }

    private void EndPause()
    {
        if (!_paused)
            return;

        _paused = false;
        _pauseTimer.Stop();
        EvaluateSchedule();
    }

    private void ToggleChromeElevation()
    {
        var settings = SettingsService.Instance.Current;
        if (!ShouldBeRestricted(DateTime.Now) || _paused || !settings.ChromeEnabled)
            return;

        if (_chromeElevated)
        {
            StopChromeElevation();
            UpdateClocks();
            StatusChanged?.Invoke("Mode focus seul");
            return;
        }

        if (!ChromeLauncher.TryLaunch(settings, out _))
        {
            StatusChanged?.Invoke("Chrome introuvable");
            return;
        }

        StartChromeElevation();
    }

    private void StartChromeElevation()
    {
        _chromeElevated = true;
        _chromeTimer.Interval = ChromeElevateInterval;
        if (!_chromeTimer.IsEnabled)
            _chromeTimer.Start();

        ApplyChromeElevation();
        UpdateClocks();
        StatusChanged?.Invoke("Chrome au-dessus — mode focus toujours actif");
        LockStateChanged?.Invoke(true);
    }

    private void StopChromeElevation()
    {
        _chromeElevated = false;
        _chromeTimer.Stop();
    }

    private void OnChromeTimerTick()
    {
        if (!_chromeElevated)
            return;

        if (!ChromeLauncher.IsChromeRunning())
        {
            StopChromeElevation();
            UpdateClocks();
            StatusChanged?.Invoke("Mode focus seul");
            return;
        }

        ApplyChromeElevation();
    }

    private void ApplyChromeElevation()
    {
        if (!_overlays.IsVisible)
            return;

        ChromeWindowElevator.Apply(_overlays.GetWindows(), SettingsService.Instance.Current.AllowedProcesses);
    }

    private void StopLockUi()
    {
        StopChromeElevation();
        _overlays.HideAll();
        _clockTimer.Stop();
    }

    public void Dispose()
    {
        SettingsService.Instance.SettingsChanged -= OnSettingsChanged;
        if (_displayChangedHandler is not null)
            SystemEvents.DisplaySettingsChanged -= _displayChangedHandler;

        StatsService.Instance.EndSession();
        _scheduleTimer.Stop();
        _clockTimer.Stop();
        _pauseTimer.Stop();
        _chromeTimer.Stop();
        _pauseHotkey.Dispose();
        _overlays.HideAll();
    }
}
