using System.Threading;
using System.Windows;
using System.Windows.Threading;
using ChatGPT.Sidecar.Dock.Diagnostics;

namespace ChatGPT.Sidecar.Dock;

public partial class App : Application
{
    private readonly StartupCrashReporter _startupReporter;
    private int _handlingFatalException;

    public App()
    {
        _startupReporter = new StartupCrashReporter();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _startupReporter.RecordEnvironment();
        _startupReporter.Record("application.constructed");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _startupReporter.Record("application.startup.enter");

        try
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var window = new MainWindow();
            MainWindow = window;
            _startupReporter.Record("main_window.constructed");

            window.SourceInitialized += (_, _) => _startupReporter.Record("main_window.source_initialized");
            window.Loaded += (_, _) => _startupReporter.Record("main_window.loaded");
            window.ContentRendered += (_, _) => _startupReporter.Record("main_window.content_rendered");
            window.Show();

            _startupReporter.Record("main_window.show_called");
        }
        catch (Exception exception)
        {
            ReportFatal("application startup", exception, showDialog: true);
            Shutdown(-1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportFatal("UI dispatcher", e.Exception, showDialog: true);
        e.Handled = true;
        Shutdown(-1);
    }

    private void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException($"Unhandled non-Exception object: {e.ExceptionObject}");
        ReportFatal("application domain", exception, showDialog: false);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _startupReporter.RecordException("unobserved task", e.Exception);
        e.SetObserved();
    }

    private void ReportFatal(string stage, Exception exception, bool showDialog)
    {
        if (Interlocked.Exchange(ref _handlingFatalException, 1) != 0)
        {
            return;
        }

        _startupReporter.RecordException(stage, exception);
        if (!showDialog)
        {
            return;
        }

        try
        {
            MessageBox.Show(
                _startupReporter.BuildFailureMessage(stage),
                "ChatGPT Sidecar startup failure",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // The startup log still contains the failure if WPF cannot display a dialog.
        }
    }
}
