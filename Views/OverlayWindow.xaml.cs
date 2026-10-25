using System.Windows;
using System.Windows.Forms;
using FocusHeure.Helpers;
using FocusHeure.Models;

namespace FocusHeure.Views;

public partial class OverlayWindow : Window
{
    public Action? OnChromeClick { get; set; }

    private readonly Screen _screen;
    private bool _allowClose;

    public OverlayWindow(Screen screen)
    {
        _screen = screen;
        InitializeComponent();
        Loaded += (_, _) => MonitorHelper.PlaceFullscreen(this, _screen);
        Closing += (_, e) =>
        {
            if (!_allowClose)
                e.Cancel = true;
        };
    }

    public void RequestClose()
    {
        _allowClose = true;
        Close();
    }

    public void UpdateDisplay(string time, string? blockLabel, string? countdown, AppSettings settings, bool chromeElevated)
    {
        ClockText.Text = time;
        MessageText.Text = settings.CustomMessage;
        BlockText.Text = blockLabel is null ? "" : $"Créneau {blockLabel}";

        if (settings.ShowCountdown && !string.IsNullOrEmpty(countdown))
        {
            CountdownText.Text = countdown;
            CountdownText.Visibility = Visibility.Visible;
        }
        else
        {
            CountdownText.Visibility = Visibility.Collapsed;
        }

        if (settings.ShowChromeButton && settings.ChromeEnabled)
        {
            ChromeButton.Visibility = Visibility.Visible;
            ChromeButton.Content = chromeElevated
                ? "Chrome actif — recliquer pour masquer"
                : "Ouvrir Chrome au-dessus";
        }
        else
        {
            ChromeButton.Visibility = Visibility.Collapsed;
        }
    }

    private void ChromeButton_OnClick(object sender, RoutedEventArgs e) => OnChromeClick?.Invoke();

    private void Window_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.System && e.SystemKey == System.Windows.Input.Key.F4)
            e.Handled = true;
    }
}
