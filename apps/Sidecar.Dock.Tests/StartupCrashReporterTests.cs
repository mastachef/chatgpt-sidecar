using ChatGPT.Sidecar.Dock.Diagnostics;
using Xunit;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class StartupCrashReporterTests
{
    [Fact]
    public void Record_SanitizesFieldsAndCreatesParentDirectory()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "nested", "startup-crash.log");
            var reporter = new StartupCrashReporter(path);

            reporter.Record("startup.event", ("message", "first line\r\nsecond line"));

            var lines = File.ReadAllLines(path);
            Assert.Single(lines);
            Assert.Contains("startup.event", lines[0], StringComparison.Ordinal);
            Assert.Contains("message=first line  second line", lines[0], StringComparison.Ordinal);
            Assert.DoesNotContain('\r', lines[0]);
            Assert.DoesNotContain('\n', lines[0]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RecordException_WritesActionableFailureAndMessageIncludesLogPath()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "startup-crash.log");
            var reporter = new StartupCrashReporter(path);
            var exception = new InvalidOperationException("window failed");

            reporter.RecordException("application startup", exception);
            var message = reporter.BuildFailureMessage("application startup");
            var log = File.ReadAllText(path);

            Assert.Contains("fatal.exception", log, StringComparison.Ordinal);
            Assert.Contains("stage=application startup", log, StringComparison.Ordinal);
            Assert.Contains("InvalidOperationException", log, StringComparison.Ordinal);
            Assert.Contains("window failed", log, StringComparison.Ordinal);
            Assert.Contains(path, message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-startup-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
