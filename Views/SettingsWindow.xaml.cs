using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FocusHeure.Models;
using FocusHeure.Services;

namespace FocusHeure.Views;

public partial class SettingsWindow : Window
{
    private readonly List<WindowRow> _rows = [];

    public SettingsWindow()
    {
        InitializeComponent();
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = SettingsService.Instance.Current;
        _rows.Clear();
        WindowsList.Items.Clear();

        foreach (var w in s.Windows)
            AddRow(w.Start, w.End, w.Enabled);

        WeekendExemptBox.IsChecked = s.WeekendExempt;
        ShowCountdownBox.IsChecked = s.ShowCountdown;
        ShowChromeBox.IsChecked = s.ShowChromeButton;
        ChromeEnabledBox.IsChecked = s.ChromeEnabled;
        StartupBox.IsChecked = s.RunAtStartup;
        PauseMinutesBox.Text = s.PauseMinutes.ToString();
        NotifyMinutesBox.Text = s.NotifyBeforeMinutes.ToString();
        CustomMessageBox.Text = s.CustomMessage;
        ChromePathBox.Text = s.ChromePath ?? "";
        AllowedProcessesBox.Text = string.Join(", ", s.AllowedProcesses);

        var stats = StatsService.Instance.Data;
        StatsText.Text =
            $"Statistiques : {stats.SessionCount} sessions · {stats.TotalFocusMinutes} min de focus au total";
    }

    private void AddRow(string start, string end, bool enabled)
    {
        var enabledCheck = new System.Windows.Controls.CheckBox { IsChecked = enabled, VerticalAlignment = VerticalAlignment.Center };
        var startBox = new System.Windows.Controls.TextBox { Text = start, Width = 70, Padding = new Thickness(4), Margin = new Thickness(4, 0, 0, 0) };
        var endBox = new System.Windows.Controls.TextBox { Text = end, Width = 70, Padding = new Thickness(4), Margin = new Thickness(4, 0, 0, 0) };
        var removeBtn = new System.Windows.Controls.Button
        {
            Content = "×",
            Width = 28,
            Margin = new Thickness(8, 0, 0, 0),
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
        };

        var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        panel.Children.Add(enabledCheck);
        panel.Children.Add(new TextBlock { Text = "De", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 4, 0) });
        panel.Children.Add(startBox);
        panel.Children.Add(new TextBlock { Text = "à", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) });
        panel.Children.Add(endBox);
        panel.Children.Add(removeBtn);

        var row = new WindowRow(enabledCheck, startBox, endBox, panel);
        _rows.Add(row);
        removeBtn.Click += (_, _) =>
        {
            _rows.Remove(row);
            WindowsList.Items.Remove(panel);
        };
        WindowsList.Items.Add(panel);
    }

    private void AddWindow_OnClick(object sender, RoutedEventArgs e) =>
        AddRow("12:00", "13:00", true);

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        var windows = new List<TimeWindowDto>();
        foreach (var row in _rows)
        {
            var dto = new TimeWindowDto
            {
                Start = row.Start.Text.Trim(),
                End = row.End.Text.Trim(),
                Enabled = row.Enabled.IsChecked == true,
            };

            if (!dto.IsValid())
            {
                System.Windows.MessageBox.Show(
                    "Format horaire invalide (HH:mm), ou début et fin identiques.",
                    "Focus Heure",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            windows.Add(dto);
        }

        if (!int.TryParse(PauseMinutesBox.Text, out var pauseMin) || pauseMin < 1 || pauseMin > 180)
        {
            System.Windows.MessageBox.Show("Pause : entre 1 et 180 minutes.", "Focus Heure");
            return;
        }

        if (!int.TryParse(NotifyMinutesBox.Text, out var notifyMin) || notifyMin < 0 || notifyMin > 60)
        {
            System.Windows.MessageBox.Show("Alerte : entre 0 et 60 minutes.", "Focus Heure");
            return;
        }

        SettingsService.Instance.Update(s =>
        {
            s.Windows = windows;
            s.WeekendExempt = WeekendExemptBox.IsChecked == true;
            s.ShowCountdown = ShowCountdownBox.IsChecked == true;
            s.ShowChromeButton = ShowChromeBox.IsChecked == true;
            s.ChromeEnabled = ChromeEnabledBox.IsChecked == true;
            s.RunAtStartup = StartupBox.IsChecked == true;
            s.PauseMinutes = pauseMin;
            s.NotifyBeforeMinutes = notifyMin;
            s.CustomMessage = CustomMessageBox.Text.Trim();
            s.ChromePath = string.IsNullOrWhiteSpace(ChromePathBox.Text) ? null : ChromePathBox.Text.Trim();
            s.AllowedProcesses = ParseAllowedProcesses();

            if (!string.IsNullOrWhiteSpace(PinBox.Password))
                s.PinHash = SettingsService.HashPin(PinBox.Password);
        });

        try
        {
            var path = Environment.ProcessPath ?? "";
            StartupService.SetEnabled(StartupBox.IsChecked == true, path);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Démarrage auto");
        }

        System.Windows.MessageBox.Show("Paramètres enregistrés.", "Focus Heure");
        Close();
    }

    private void OpenFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var dir = SettingsService.Instance.SettingsDirectory;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
    }

    private List<string> ParseAllowedProcesses() =>
        AllowedProcessesBox.Text
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

    private sealed record WindowRow(
        System.Windows.Controls.CheckBox Enabled,
        System.Windows.Controls.TextBox Start,
        System.Windows.Controls.TextBox End,
        StackPanel Panel);
}
