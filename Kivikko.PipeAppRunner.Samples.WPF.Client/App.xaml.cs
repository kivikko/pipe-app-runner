using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Kivikko.PipeAppRunner.Samples.WPF.Client;

public partial class App
{
    private Dispatcher? _dispatcher;
    
    private void AppStartup(object sender, StartupEventArgs e)
    {
        try
        {
            var startClientApp = PipeAppRunner.StartClientApp(
                startApp: options =>
                {
                    _dispatcher = Dispatcher.CurrentDispatcher;
                    var clientLogic = new ClientLogic(options.PipeClient);
                    var window = new ClientWindow(clientLogic);
                    window.Loaded += (_, _) => SetOwner(window, options.OwnerWindowHandle);
                    window.Show();
                },
                args: e.Args,
                activateExistingClient: ActivateWindow);
        
            if (!startClientApp)
                Shutdown(0);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private static void SetOwner(Window window, string ownerWindowHandle)
    {
        if (string.IsNullOrWhiteSpace(ownerWindowHandle) ||
            !int.TryParse(ownerWindowHandle, out var ownerHandle) ||
            ownerHandle <= 0)
            return;

        _ = new WindowInteropHelper(window) { Owner = new IntPtr(ownerHandle) };
    }

    private void ActivateWindow() => _dispatcher?.Invoke(() =>
    {
        try
        {
            var window = this.MainWindow;
        
            if (window is null)
                return;

            if (window.WindowState is WindowState.Minimized)
                window.WindowState = WindowState.Normal;

            window.Topmost = true;
            Task.Delay(100);
            window.Activate();
            window.Topmost = false;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    });
}