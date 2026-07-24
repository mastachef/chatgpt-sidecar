using System.Runtime.InteropServices;
using System.Text;

namespace ChatGPT.Sidecar.Dock.Diagnostics;

internal sealed class StartupCrashReporter
{
    private readonly object _sync = new();

    public StartupCrashReporter(string? logPath = null)
    {
        LogPath = logPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTSidecar",
            "Diagnostics",
            "startup-crash.log");
    }

    public string LogPath { get; }

    public void Record(string eventName, params (string Key, object? Value)[] fields)
    {
        var line = new StringBuilder();
        line.Append(DateTimeOffset.Now.ToString("O"));
        line.Append(" | ");
        line.Append(Sanitize(eventName));

        foreach (var (key, value) in fields)
        {
            line.Append(" | ");
            line.Append(Sanitize(key));
            line.Append('=');
            line.Append(Sanitize(value?.ToString() ?? "null"));
        }

        AppendLine(line.ToString());
    }

    public void RecordEnvironment()
    {
        Record(
            "process.start",
            ("version", typeof(StartupCrashReporter).Assembly.GetName().Version?.ToString() ?? "unknown"),
            ("process_arch", RuntimeInformation.ProcessArchitecture),
            ("os_arch", RuntimeInformation.OSArchitecture),
            ("framework", RuntimeInformation.FrameworkDescription),
            ("os", RuntimeInformation.OSDescription),
            ("base_directory", AppContext.BaseDirectory),
            ("current_directory", Environment.CurrentDirectory),
            ("command_line", Environment.CommandLine));
    }

    public void RecordException(string stage, Exception exception)
    {
        Record(
            "fatal.exception",
            ("stage", stage),
            ("type", exception.GetType().FullName ?? exception.GetType().Name),
            ("message", exception.Message),
            ("hresult", $"0x{exception.HResult:X8}"));

        AppendLine(exception.ToString());
    }

    public string BuildFailureMessage(string stage)
    {
        return $"ChatGPT Sidecar failed during {stage} before it could finish opening.\n\n" +
               $"A startup report was written to:\n{LogPath}\n\n" +
               "Open that file in Notepad and paste its contents into the ChatGPT conversation so the exact crash can be fixed.";
    }

    private void AppendLine(string value)
    {
        try
        {
            lock (_sync)
            {
                var directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(LogPath, value + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Startup diagnostics must never create a second startup failure.
        }
    }

    internal static string Sanitize(string value)
    {
        return value.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }
}
