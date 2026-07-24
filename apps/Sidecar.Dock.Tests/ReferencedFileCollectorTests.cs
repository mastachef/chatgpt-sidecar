using ChatGPT.Sidecar.Dock.RepositoryContext;
using Xunit;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class ReferencedFileCollectorTests
{
    [Fact]
    public void Collect_IncludesSafeReferencedFilesAndBlocksSensitivePaths()
    {
        var root = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            File.WriteAllText(Path.Combine(root, "src", "worker.cs"), "internal class Worker { }\n");
            File.WriteAllText(Path.Combine(root, ".env"), "OPENAI_API_KEY=sk-this-must-never-leave\n");
            Directory.CreateDirectory(Path.Combine(root, "secrets"));
            File.WriteAllText(Path.Combine(root, "secrets", "credentials.json"), "{\"token\":\"hidden\"}\n");

            var files = ReferencedFileCollector.Collect(
                root,
                ["Review `src/worker.cs`, then compare .env and secrets/credentials.json."]);

            Assert.True(files.ContainsKey("src/worker.cs"));
            Assert.DoesNotContain(files.Keys, path => path.Contains(".env", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(files.Keys, path => path.Contains("credential", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Collect_RejectsTraversalOutsideRepository()
    {
        var root = CreateTempDirectory();
        var outside = Path.Combine(Path.GetDirectoryName(root)!, "outside.cs");
        try
        {
            File.WriteAllText(outside, "secret outside repository");
            var files = ReferencedFileCollector.Collect(root, ["Read ../outside.cs"]);
            Assert.Empty(files);
        }
        finally
        {
            if (File.Exists(outside)) File.Delete(outside);
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-files-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
