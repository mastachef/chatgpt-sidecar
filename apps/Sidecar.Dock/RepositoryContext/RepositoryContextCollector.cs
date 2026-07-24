using System.Diagnostics;
using System.IO;
using System.Text;

namespace ChatGPT.Sidecar.Dock.RepositoryContext;

internal sealed class RepositoryContextCollector
{
    private static readonly string[] CandidateProjectFiles =
    [
        "AGENTS.md",
        "README.md",
        "README.txt",
        "package.json",
        "pyproject.toml",
        "Cargo.toml",
        "go.mod",
        "pom.xml",
        "build.gradle",
        "build.gradle.kts",
        "Directory.Build.props",
        "global.json"
    ];

    public RepositorySnapshot Collect(
        string workingDirectory,
        IEnumerable<string>? conversationTexts = null)
    {
        var existingDirectory = Directory.Exists(workingDirectory)
            ? Path.GetFullPath(workingDirectory)
            : Environment.CurrentDirectory;
        var root = RunGit(existingDirectory, "rev-parse --show-toplevel", 4_000).Trim();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) root = existingDirectory;

        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fileName in CandidateProjectFiles)
        {
            var path = Path.Combine(root, fileName);
            if (!File.Exists(path)) continue;
            files[fileName] = ReadBoundedText(path, 12_000);
        }

        var referencedFiles = ReferencedFileCollector
            .Collect(root, conversationTexts ?? Array.Empty<string>())
            .Where(file => !files.ContainsKey(file.Key))
            .ToDictionary(file => file.Key, file => file.Value, StringComparer.OrdinalIgnoreCase);

        return new RepositorySnapshot(
            root,
            RunGit(root, "status --short --branch", 12_000),
            RunGit(root, "diff --no-ext-diff --unified=3", 24_000),
            RunGit(root, "diff --cached --no-ext-diff --unified=3", 18_000),
            RunGit(root, "log -5 --pretty=format:%h%x09%ad%x09%s --date=short", 8_000),
            files,
            referencedFiles);
    }

    private static string RunGit(string workingDirectory, string arguments, int maxCharacters)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(6_000))
            {
                process.Kill(entireProcessTree: true);
                return "[git command timed out]";
            }

            Task.WaitAll(outputTask, errorTask);
            var output = outputTask.Result.Trim();
            if (string.IsNullOrWhiteSpace(output)) output = errorTask.Result.Trim();
            return TruncateMiddle(output, maxCharacters);
        }
        catch (Exception exception)
        {
            return $"[git unavailable: {exception.Message}]";
        }
    }

    private static string ReadBoundedText(string path, int maxCharacters)
    {
        try
        {
            using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var buffer = new char[maxCharacters + 1];
            var count = reader.ReadBlock(buffer, 0, buffer.Length);
            var text = new string(buffer, 0, Math.Min(count, maxCharacters));
            return count > maxCharacters ? $"{text}\n[truncated]" : text;
        }
        catch (Exception exception)
        {
            return $"[could not read {Path.GetFileName(path)}: {exception.Message}]";
        }
    }

    internal static string TruncateMiddle(string value, int maxCharacters)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxCharacters) return value;
        var half = Math.Max(1, (maxCharacters - 48) / 2);
        return $"{value[..half]}\n...[truncated {value.Length - (half * 2)} characters]...\n{value[^half..]}";
    }
}
