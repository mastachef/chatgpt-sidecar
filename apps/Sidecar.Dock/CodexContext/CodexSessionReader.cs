using System.IO;
using System.Text.Json;

namespace ChatGPT.Sidecar.Dock.CodexContext;

internal sealed class CodexSessionReader
{
    private readonly string _codexHome;

    public CodexSessionReader(string? codexHome = null)
    {
        _codexHome = codexHome
            ?? Environment.GetEnvironmentVariable("CODEX_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
    }

    public CodexSession? FindLatestRootSession(int maxCandidates = 300)
    {
        var sessionsRoot = Path.Combine(_codexHome, "sessions");
        if (!Directory.Exists(sessionsRoot))
        {
            return null;
        }

        var candidates = Directory
            .EnumerateFiles(sessionsRoot, "*.jsonl", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(maxCandidates);

        CodexSession? newestAny = null;
        foreach (var candidate in candidates)
        {
            CodexSession? parsed;
            try
            {
                parsed = Parse(candidate.FullName);
            }
            catch
            {
                continue;
            }

            if (parsed is null || parsed.Messages.Count == 0)
            {
                continue;
            }

            newestAny ??= parsed;
            if (!parsed.IsSubagent)
            {
                return parsed;
            }
        }

        return newestAny;
    }

    internal static CodexSession? Parse(string path)
    {
        string? sessionId = null;
        string? threadId = null;
        string? cwd = null;
        var isSubagent = false;
        var messages = new List<CodexMessage>();

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                var recordType = ReadString(root, "type");
                var timestamp = ParseTimestamp(ReadString(root, "timestamp"));
                if (!root.TryGetProperty("payload", out var payload)) continue;

                if (recordType == "session_meta")
                {
                    var meta = payload.TryGetProperty("meta", out var nestedMeta) ? nestedMeta : payload;
                    sessionId = ReadString(meta, "session_id") ?? ReadString(meta, "id") ?? sessionId;
                    threadId = ReadString(meta, "id") ?? sessionId ?? threadId;
                    cwd = ReadString(meta, "cwd") ?? cwd;
                    isSubagent = !string.IsNullOrWhiteSpace(ReadString(meta, "parent_thread_id"))
                        || !string.IsNullOrWhiteSpace(ReadString(meta, "agent_role"));
                    continue;
                }

                if (recordType == "response_item" && ReadString(payload, "type") == "message")
                {
                    AddMessage(messages, ReadString(payload, "role"), ReadContentText(payload), timestamp);
                    continue;
                }

                if (recordType == "event_msg")
                {
                    var payloadType = ReadString(payload, "type");
                    if (payloadType == "user_message")
                    {
                        AddMessage(messages, "user", ReadString(payload, "message"), timestamp);
                    }
                    else if (payloadType == "agent_message")
                    {
                        AddMessage(messages, "assistant", ReadString(payload, "message"), timestamp);
                    }
                }
            }
        }

        if (messages.Count == 0)
        {
            return null;
        }

        var firstUserText = messages.FirstOrDefault(message => message.Role == "user")?.Text
            ?? "Untitled Codex conversation";
        var title = CollapseWhitespace(firstUserText);
        if (title.Length > 120) title = $"{title[..117]}...";
        var updatedAt = new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);

        return new CodexSession(
            path,
            sessionId,
            threadId,
            title,
            cwd,
            updatedAt,
            isSubagent,
            messages.TakeLast(160).ToArray());
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static DateTimeOffset? ParseTimestamp(string? raw)
    {
        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
    }

    private static string ReadContentText(JsonElement payload)
    {
        if (!payload.TryGetProperty("content", out var content)) return string.Empty;
        if (content.ValueKind == JsonValueKind.String) return content.GetString() ?? string.Empty;
        if (content.ValueKind != JsonValueKind.Array) return string.Empty;

        var parts = new List<string>();
        foreach (var part in content.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.String)
            {
                parts.Add(part.GetString() ?? string.Empty);
                continue;
            }

            if (part.ValueKind != JsonValueKind.Object) continue;
            var text = ReadString(part, "text") ?? ReadString(part, "content");
            if (!string.IsNullOrWhiteSpace(text)) parts.Add(text);
        }

        return string.Join("\n", parts);
    }

    private static void AddMessage(List<CodexMessage> messages, string? role, string? text, DateTimeOffset? timestamp)
    {
        var cleanRole = role?.Trim();
        var cleanText = text?.Trim();
        if (string.IsNullOrWhiteSpace(cleanRole) || string.IsNullOrWhiteSpace(cleanText)) return;

        var previous = messages.LastOrDefault();
        if (previous is not null && previous.Role == cleanRole && previous.Text == cleanText) return;
        messages.Add(new CodexMessage(cleanRole, cleanText, timestamp));
    }

    private static string CollapseWhitespace(string value)
    {
        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
