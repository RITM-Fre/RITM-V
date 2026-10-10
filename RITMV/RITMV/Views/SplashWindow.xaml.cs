using System.Windows.Media.Animation;

namespace RITMV.Views;

/// <summary>
/// RITM V animated startup splash.
/// </summary>
public partial class SplashWindow
{
    private readonly DispatcherTimer _dots;
    private string _baseStatus = "Starting";
    private int _dotCount;

    public SplashWindow()
    {
        InitializeComponent();

        _dots = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(320),
        };
        _dots.Tick += (_, _) =>
        {
            _dotCount = (_dotCount + 1) % 4;
            txtStatus.Text = _baseStatus + new string('.', _dotCount);
        };
        _dots.Start();
    }

    /// <summary>Updates the live status line shown under the progress bar.</summary>
    public void SetStatus(string text)
    {
        _baseStatus = text;
        txtStatus.Text = text;
    }

    /// <summary>Fades the splash out and closes it.</summary>
    public void CloseWithFade()
    {
        try
        {
            _dots.Stop();

            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            };
            fade.Completed += (_, _) =>
            {
                try
                {
                    Close();
                }
                catch { /* already gone */ }
            };
            BeginAnimation(OpacityProperty, fade);
        }
        catch
        {
            try
            {
                Close();
            }
            catch { /* already gone */ }
        }
    }
}
