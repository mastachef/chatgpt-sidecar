using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ChatGPT.Sidecar.Dock.Diagnostics;

internal sealed class SidecarDiagnostics
{
    private const long MaxLogBytes = 768_000;
    private const int MaxFieldCharacters = 320;
    private const int ReportLineLimit = 160;
    private readonly object _sync = new();

    public SidecarDiagnostics(string? logPath = null)
    {
        LogPath = logPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTSidecar",
            "Diagnostics",
            "sidecar-dock.log");
    }

    public string LogPath { get; }

    public void Record(string eventName, params (string Key, object? Value)[] fields)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (_sync)
            {
                RotateIfNeeded();
                var line = new StringBuilder();
                line.Append(DateTimeOffset.Now.ToString("O"));
                line.Append(" | ");
                line.Append(Sanitize(eventName));
                foreach (var (key, value) in fields)
                {
                    line.Append(" | ");
                    line.Append(Sanitize(key));
                    line.Append('=');
                    line.Append(Sanitize(value?.ToString()));
                }

                File.AppendAllText(LogPath, line.AppendLine().ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Diagnostics must never interrupt the product workflow.
        }
    }

    public void RecordException(string eventName, Exception exception)
    {
        Record(
            eventName,
            ("exception", exception.GetType().Name),
            ("message", exception.Message));
    }

    public string BuildReport()
    {
        var report = new StringBuilder();
        var assembly = Assembly.GetExecutingAssembly().GetName();
        report.AppendLine("ChatGPT Sidecar Dock diagnostics");
        report.AppendLine($"Generated: {DateTimeOffset.Now:O}");
        report.AppendLine($"App version: {assembly.Version?.ToString() ?? "unknown"}");
        report.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        report.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        report.AppendLine($"Architecture: process={RuntimeInformation.ProcessArchitecture}, OS={RuntimeInformation.OSArchitecture}");
        report.AppendLine($"Log: {LogPath}");
        report.AppendLine();
        report.AppendLine("Recent events (context text and repository contents are never logged):");

        try
        {
            string[] lines;
            lock (_sync)
            {
                lines = File.Exists(LogPath) ? File.ReadAllLines(LogPath) : Array.Empty<string>();
            }

            foreach (var line in lines.TakeLast(ReportLineLimit))
            {
                report.AppendLine(line);
            }
        }
        catch (Exception exception)
        {
            report.AppendLine($"[Could not read diagnostics log: {exception.GetType().Name}: {Sanitize(exception.Message)}]");
        }

        return report.ToString();
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(LogPath) || new FileInfo(LogPath).Length < MaxLogBytes)
        {
            return;
        }

        var archivePath = $"{LogPath}.1";
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        File.Move(LogPath, archivePath);
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "none";
        }

        var clean = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return clean.Length <= MaxFieldCharacters
            ? clean
            : $"{clean[..MaxFieldCharacters]}…";
    }
}
