using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace FocusHeure.Services;

/// <summary>
/// Détecte Échap + Entrée maintenus ensemble (polling léger, actif uniquement en mode verrou).
/// </summary>
public sealed class PauseHotkeyService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VkEscape = 0x1B;
    private const int VkReturn = 0x0D;

    private readonly DispatcherTimer _timer;
    private bool _bothWereDown;

    public event Action? PauseRequested;

    public bool IsRunning => _timer.IsEnabled;

    public PauseHotkeyService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _timer.Tick += OnTick;
    }

    public void Start() => _timer.Start();

    public void Stop()
    {
        _timer.Stop();
        _bothWereDown = false;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var esc = IsDown(VkEscape);
        var enter = IsDown(VkReturn);

        if (esc && enter)
        {
            if (!_bothWereDown)
            {
                _bothWereDown = true;
                PauseRequested?.Invoke();
            }
        }
        else
        {
            _bothWereDown = false;
        }
    }

    private static bool IsDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    public void Dispose() => _timer.Stop();
}
