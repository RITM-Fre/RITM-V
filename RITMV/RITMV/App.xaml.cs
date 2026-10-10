using RITMV.Manager;
using RITMV.Views;

namespace RITMV;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static EventWaitHandle ProgramStarted;

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// Open only one process
    /// </summary>
    /// <param name="e"></param>
    protected override void OnStartup(StartupEventArgs e)
    {
        var exePathKey = Utils.GetMd5(Utils.GetExePath());

        var rebootas = e.Args.Any(t => t == Global.RebootAs);
        ProgramStarted = new EventWaitHandle(false, EventResetMode.AutoReset, exePathKey, out var bCreatedNew);
        if (!rebootas && !bCreatedNew)
        {
            ProgramStarted.Set();
            Environment.Exit(0);
            return;
        }

        base.OnStartup(e);

        // RITM V: when a second instance is launched, it signals this handle so we can
        // bring the (possibly tray-hidden) main window back to the foreground instead of
        // silently starting a dead second copy.
        _ = Task.Run(() =>
        {
            while (true)
            {
                try
                {
                    ProgramStarted.WaitOne();
                }
                catch
                {
                    break;
                }

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var w = Application.Current.MainWindow as Window;
                    if (w == null)
                    {
                        return;
                    }

                    if (w.WindowState == WindowState.Minimized)
                    {
                        w.WindowState = WindowState.Normal;
                    }

                    w.Show();
                    w.Visibility = Visibility.Visible;
                    w.Activate();
                    // brief topmost pulse so the restored window is guaranteed to surface
                    w.Topmost = true;
                    w.Topmost = false;
                });
            }
        });

        // RITM V: show the animated splash while the application boots up.
        var splash = new SplashWindow();
        splash.Show();
        splash.SetStatus("Loading configuration");

        Dispatcher.InvokeAsync(() => { _ = BootAsync(splash); }, DispatcherPriority.Background);
    }

    /// <summary>
    /// RITM V boot sequence: configuration -> components -> theme -> shell,
    /// with live feedback on the splash screen.
    /// </summary>
    private async Task BootAsync(SplashWindow splash)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            var ok = await Task.Run(() => AppManager.Instance.InitApp());
            if (!ok)
            {
                splash.CloseWithFade();
                UI.Show($"Loading GUI configuration file is abnormal,please restart the application{Environment.NewLine}加载GUI配置文件异常,请重启应用");
                Environment.Exit(0);
                return;
            }

            // RITM V neon theme (dark by default)
            RitmTheme.Apply(RitmTheme.ResolveIsDark(AppManager.Instance.Config?.UiItem?.CurrentTheme));

            splash.SetStatus("Preparing components");
            await Task.Run(() =>
            {
                AppManager.Instance.WindowDialog = new WindowDialog();
                AppManager.Instance.InitComponents();
            });

            splash.SetStatus("Starting interface");
            RxAppBuilder.CreateReactiveUIBuilder()
                .WithWpf()
                .BuildApp();

            var mainWindowViewModel = new MainWindowViewModel();
            var viewFor = SimpleViewLocator.Instance.ResolveView(mainWindowViewModel);
            viewFor!.ViewModel = mainWindowViewModel;

            // keep the splash visible long enough for the animation to land
            var remaining = 2400 - watch.ElapsedMilliseconds;
            if (remaining > 0)
            {
                await Task.Delay((int)remaining);
            }

            splash.SetStatus("Ready");
            splash.CloseWithFade();

            var mainWindow = (MainWindow)viewFor;
            mainWindow.Show();
            MainWindow = mainWindow;
        }
        catch (Exception ex)
        {
            Logging.SaveLog("BootAsync", ex);
            splash.CloseWithFade();
            Environment.Exit(0);
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logging.SaveLog("App_DispatcherUnhandledException", e.Exception);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject != null)
        {
            Logging.SaveLog("CurrentDomain_UnhandledException", (Exception)e.ExceptionObject);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logging.SaveLog("TaskScheduler_UnobservedTaskException", e.Exception);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logging.SaveLog("OnExit");
        base.OnExit(e);
        Process.GetCurrentProcess().Kill();
    }
}
