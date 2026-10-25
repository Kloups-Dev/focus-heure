using System.Windows;
using System.Windows.Forms;
using FocusHeure.Views;

namespace FocusHeure.Services;

public sealed class OverlayManager
{
    private readonly List<OverlayWindow> _windows = [];
    private readonly object _sync = new();

    public bool IsVisible { get; private set; }
    public Action? OnChromeClick { get; set; }

    public void ShowAll()
    {
        lock (_sync)
        {
            HideAllInternal();

            foreach (var screen in Screen.AllScreens)
            {
                var window = new OverlayWindow(screen) { OnChromeClick = OnChromeClick };
                window.Show();
                _windows.Add(window);
            }

            IsVisible = _windows.Count > 0;
        }
    }

    public void HideAll()
    {
        lock (_sync)
        {
            HideAllInternal();
        }
    }

    private void HideAllInternal()
    {
        foreach (var w in _windows)
            w.RequestClose();

        _windows.Clear();
        IsVisible = false;
    }

    public void UpdateDisplays(Action<OverlayWindow> update)
    {
        lock (_sync)
        {
            foreach (var w in _windows)
                update(w);
        }
    }

    public IReadOnlyList<Window> GetWindows()
    {
        lock (_sync)
            return _windows.Cast<Window>().ToList();
    }

    public void RefreshMonitors()
    {
        if (!IsVisible)
            return;

        var onChrome = OnChromeClick;
        HideAll();
        OnChromeClick = onChrome;
        ShowAll();
    }
}
