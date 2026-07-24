using ChatGPT.Sidecar.Dock.Diagnostics;
using Xunit;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class SidecarDiagnosticsTests
{
    [Fact]
    public void BuildReport_IncludesEnvironmentAndRecordedMetadata()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "sidecar.log");
            var diagnostics = new SidecarDiagnostics(path);
            diagnostics.Record(
                "composer.populate.result",
                ("success", false),
                ("reason", "no_visible_composer"),
                ("candidates", 0));

            var report = diagnostics.BuildReport();

            Assert.Contains("ChatGPT Sidecar Dock diagnostics", report, StringComparison.Ordinal);
            Assert.Contains("composer.populate.result", report, StringComparison.Ordinal);
            Assert.Contains("reason=no_visible_composer", report, StringComparison.Ordinal);
            Assert.Contains(path, report, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Record_SanitizesMultilineFields()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "sidecar.log");
            var diagnostics = new SidecarDiagnostics(path);
            diagnostics.Record("test.event", ("message", "first line\r\nsecond line"));

            var line = File.ReadAllText(path);

            Assert.Contains("message=first line  second line", line, StringComparison.Ordinal);
            Assert.DoesNotContain("\r", line, StringComparison.Ordinal);
            Assert.Equal(1, line.Count(character => character == '\n'));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-diagnostics-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
