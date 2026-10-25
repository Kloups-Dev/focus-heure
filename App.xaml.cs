using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using FocusHeure.Services;
using FocusHeure.Views;

namespace FocusHeure;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _tray;
    private FocusSessionController? _controller;
    private ToolStripMenuItem? _statusItem;
    private ToolStripMenuItem? _detailItem;
    private ToolStripMenuItem? _startupItem;
    private Icon? _iconActive;
    private Icon? _iconIdle;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SettingsService.Instance.Load();
        StatsService.Instance.Load();

        ApplyStartupSetting();

        _controller = new FocusSessionController();
        _controller.StatusChanged += OnStatusChanged;
        _controller.LockStateChanged += OnLockStateChanged;
        _controller.NotifyRequested += OnNotify;
        _controller.Start();

        _iconActive = Helpers.TrayIconFactory.Create(true);
        _iconIdle = Helpers.TrayIconFactory.Create(false);

        BuildTray();
        OnStatusChanged(_controller.GetStatusDetail());
    }

    private static void ApplyStartupSetting()
    {
        var settings = SettingsService.Instance.Current;
        var path = Environment.ProcessPath ?? "";
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            if (StartupService.IsEnabled != settings.RunAtStartup)
                StartupService.SetEnabled(settings.RunAtStartup, path);
        }
        catch
        {
            // ignore at startup
        }
    }

    private void BuildTray()
    {
        var menu = new ContextMenuStrip();

        _statusItem = new ToolStripMenuItem("Focus Heure")
        {
            Enabled = false,
            Font = new Font(menu.Font, System.Drawing.FontStyle.Bold),
        };
        menu.Items.Add(_statusItem);

        _detailItem = new ToolStripMenuItem("") { Enabled = false };
        menu.Items.Add(_detailItem);
        menu.Items.Add(new ToolStripSeparator());

        AddMenu(menu, "Paramètres…", (_, _) => OpenSettings());
        AddMenu(menu, "Tester l'écran (30 s)", (_, _) => _controller?.TestOverlay());
        AddMenu(menu, "Activer le mode maintenant", (_, _) => _controller?.ForceLockNow());
        AddMenu(menu, "Jour libre (jusqu'à demain)", (_, _) => _controller?.SkipToday());
        menu.Items.Add(new ToolStripSeparator());

        var pauseInfo = new ToolStripMenuItem("Pause : Échap + Entrée") { Enabled = false };
        menu.Items.Add(pauseInfo);
        menu.Items.Add(new ToolStripSeparator());

        _startupItem = new ToolStripMenuItem("Lancer au démarrage de Windows")
        {
            CheckOnClick = true,
            Checked = StartupService.IsEnabled,
        };
        _startupItem.Click += (_, _) => ToggleStartup();
        menu.Items.Add(_startupItem);

        menu.Items.Add(new ToolStripSeparator());
        AddMenu(menu, "Quitter", (_, _) => TryQuit());

        _tray = new NotifyIcon
        {
            Icon = _iconIdle,
            Visible = true,
            Text = "Focus Heure",
            ContextMenuStrip = menu,
        };

        _tray.DoubleClick += (_, _) => OpenSettings();
    }

    private static void AddMenu(ContextMenuStrip menu, string text, EventHandler handler)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += handler;
        menu.Items.Add(item);
    }

    private void OpenSettings()
    {
        var win = new SettingsWindow();
        win.ShowDialog();
        _controller?.ReloadSettings();
        if (_startupItem is not null)
            _startupItem.Checked = StartupService.IsEnabled;
        OnStatusChanged(_controller?.GetStatusDetail() ?? "");
    }

    private void ToggleStartup()
    {
        var path = Environment.ProcessPath ?? "";
        var enable = _startupItem?.Checked == true;
        try
        {
            StartupService.SetEnabled(enable, path);
            SettingsService.Instance.Update(s => s.RunAtStartup = enable);
            OnStatusChanged(enable ? "Démarrage auto activé" : "Démarrage auto désactivé");
        }
        catch (Exception ex)
        {
            if (_startupItem is not null)
                _startupItem.Checked = !enable;
            System.Windows.MessageBox.Show(ex.Message, "Focus Heure", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnStatusChanged(string status)
    {
        if (_statusItem is not null)
            _statusItem.Text = $"État : {status}";
        if (_detailItem is not null && _controller is not null)
            _detailItem.Text = _controller.GetStatusDetail();
        if (_tray is not null)
            _tray.Text = Truncate($"Focus Heure — {status}", 63);
    }

    private void OnLockStateChanged(bool active)
    {
        if (_tray is null)
            return;

        var old = _tray.Icon;
        _tray.Icon = active ? _iconActive : _iconIdle;
        if (old != _iconActive && old != _iconIdle)
            old?.Dispose();
    }

    private void OnNotify(string message)
    {
        if (_tray is null)
            return;

        _tray.BalloonTipTitle = "Focus Heure";
        _tray.BalloonTipText = message;
        _tray.ShowBalloonTip(4000);
        OnStatusChanged(message);
    }

    private void TryQuit()
    {
        var settings = SettingsService.Instance.Current;
        var needsPin = settings.HasPin &&
                       (_controller?.IsOverlayVisible == true || _controller?.IsInRestriction == true);

        if (needsPin)
        {
            var dlg = new PinDialog();
            if (dlg.ShowDialog() != true || !dlg.IsAuthorized)
                return;
        }

        ShutdownApp();
    }

    private void ShutdownApp()
    {
        _tray!.Visible = false;
        _tray.Dispose();
        _iconActive?.Dispose();
        _iconIdle?.Dispose();
        _controller?.Dispose();
        Shutdown();
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _controller?.Dispose();
        base.OnExit(e);
    }
}
