using System.Windows;
using FocusHeure.Services;

namespace FocusHeure.Views;

public partial class PinDialog : Window
{
    public bool IsAuthorized { get; private set; }

    public PinDialog()
    {
        InitializeComponent();
        PinBox.Focus();
    }

    private void Ok_OnClick(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        var hash = SettingsService.Instance.Current.PinHash;

        if (SettingsService.VerifyPin(pin, hash))
        {
            IsAuthorized = true;
            DialogResult = true;
            Close();
            return;
        }

        ErrorText.Text = "Code incorrect";
        ErrorText.Visibility = Visibility.Visible;
        PinBox.Clear();
        PinBox.Focus();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
