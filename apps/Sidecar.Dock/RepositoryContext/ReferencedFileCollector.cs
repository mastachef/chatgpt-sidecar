using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ChatGPT.Sidecar.Dock.Security;

namespace ChatGPT.Sidecar.Dock.RepositoryContext;

internal static partial class ReferencedFileCollector
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".xaml", ".json", ".jsonl", ".md", ".txt",
        ".js", ".mjs", ".cjs", ".ts", ".tsx", ".jsx", ".py", ".rs", ".go",
        ".java", ".kt", ".kts", ".cpp", ".c", ".h", ".hpp", ".css", ".scss",
        ".html", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".conf",
        ".gradle", ".properties", ".sql", ".sh", ".ps1", ".bat", ".cmd"
    };

    private static readonly HashSet<string> BlockedSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "node_modules", "bin", "obj", "dist", "build", "vendor", ".next",
        ".turbo", ".cache", "coverage", "packages"
    };

    public static IReadOnlyDictionary<string, string> Collect(
        string repositoryRoot,
        IEnumerable<string> conversationTexts,
        int maxFiles = 12,
        int maxCharactersPerFile = 10_000,
        int maxTotalCharacters = 36_000)
    {
        if (!Directory.Exists(repositoryRoot) || maxFiles <= 0 || maxTotalCharacters <= 0)
        {
            return new Dictionary<string, string>();
        }

        var root = Path.GetFullPath(repositoryRoot);
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var text in conversationTexts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foreach (Match match in PathReferenceRegex().Matches(text))
            {
                var raw = match.Groups["path"].Value
                    .Trim('`', '"', '\'', '(', ')', '[', ']', '{', '}', ',', ';', ':', '.');
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    candidates.Add(raw);
                }
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usedCharacters = 0;
        foreach (var candidate in candidates)
        {
            if (result.Count >= maxFiles || usedCharacters >= maxTotalCharacters)
            {
                break;
            }

            var normalized = candidate.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            while (normalized.StartsWith($".{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                normalized = normalized[2..];
            }

            if (Path.IsPathRooted(normalized) || normalized.Split(Path.DirectorySeparatorChar).Any(segment => segment == ".."))
            {
                continue;
            }

            var segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || segments.Any(segment => BlockedSegments.Contains(segment)))
            {
                continue;
            }

            var fileName = segments[^1];
            if (IsBlockedFileName(fileName) || !AllowedExtensions.Contains(Path.GetExtension(fileName)))
            {
                continue;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(root, normalized));
            }
            catch
            {
                continue;
            }

            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            {
                continue;
            }

            var remaining = Math.Min(maxCharactersPerFile, maxTotalCharacters - usedCharacters);
            var content = ReadBoundedText(fullPath, remaining);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
            result[relativePath] = SecretRedactor.Redact(content);
            usedCharacters += content.Length;
        }

        return result;
    }

    private static bool IsBlockedFileName(string fileName)
    {
        var normalized = fileName.ToLowerInvariant();
        return normalized == ".env"
            || normalized.StartsWith(".env.", StringComparison.Ordinal)
            || normalized.Contains("credential", StringComparison.Ordinal)
            || normalized.Contains("secret", StringComparison.Ordinal)
            || normalized.EndsWith(".pem", StringComparison.Ordinal)
            || normalized.EndsWith(".key", StringComparison.Ordinal)
            || normalized.EndsWith(".pfx", StringComparison.Ordinal)
            || normalized.EndsWith(".p12", StringComparison.Ordinal);
    }

    private static string ReadBoundedText(string path, int maxCharacters)
    {
        if (maxCharacters <= 0)
        {
            return string.Empty;
        }

        try
        {
            var info = new FileInfo(path);
            if (info.Length > 1_000_000)
            {
                return string.Empty;
            }

            using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var buffer = new char[maxCharacters + 1];
            var count = reader.ReadBlock(buffer, 0, buffer.Length);
            var text = new string(buffer, 0, Math.Min(count, maxCharacters));
            return count > maxCharacters ? $"{text}\n[truncated]" : text;
        }
        catch
        {
            return string.Empty;
        }
    }

    // Extraction is intentionally broad. Extension, repository-boundary, traversal,
    // blocked-directory, filename, size, and existence checks provide the security boundary.
    [GeneratedRegex("(?<path>(?:(?:\\.{1,2})?[\\\\/])?(?:[A-Za-z0-9_@+.-]+[\\\\/])*[A-Za-z0-9_@+.-]+\\.[A-Za-z0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PathReferenceRegex();
}
