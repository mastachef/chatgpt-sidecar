using System.Text.Json;
using ChatGPT.Sidecar.Dock.CodexContext;
using Xunit;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class CodexSessionReaderTests
{
    [Fact]
    public void ListRecentRootSessions_SortsRootsAndExcludesSubagents()
    {
        var root = CreateTempDirectory();
        try
        {
            WriteRollout(root, "older", "thread-older", "C:/repos/older", false, new DateTime(2026, 7, 24, 1, 0, 0, DateTimeKind.Utc));
            WriteRollout(root, "subagent", "thread-worker", "C:/repos/newer", true, new DateTime(2026, 7, 24, 3, 0, 0, DateTimeKind.Utc));
            WriteRollout(root, "newer", "thread-newer", "C:/repos/newer", false, new DateTime(2026, 7, 24, 2, 0, 0, DateTimeKind.Utc));

            var sessions = new CodexSessionReader(root).ListRecentRootSessions();

            Assert.Equal(2, sessions.Count);
            Assert.Equal("thread-newer", sessions[0].ThreadId);
            Assert.Equal("thread-older", sessions[1].ThreadId);
            Assert.DoesNotContain(sessions, session => session.IsSubagent);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Parse_DeduplicatesMirroredEventAndResponseMessages()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = WriteRollout(root, "dedupe", "thread-dedupe", root, false, DateTime.UtcNow, includeMirroredEvents: true);
            var session = CodexSessionReader.Parse(path);

            Assert.NotNull(session);
            Assert.Equal(2, session!.Messages.Count);
            Assert.Equal("user", session.Messages[0].Role);
            Assert.Equal("assistant", session.Messages[1].Role);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string WriteRollout(
        string codexHome,
        string fileName,
        string threadId,
        string workingDirectory,
        bool isSubagent,
        DateTime lastWriteUtc,
        bool includeMirroredEvents = false)
    {
        var directory = Path.Combine(codexHome, "sessions", "2026", "07", "24");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{fileName}.jsonl");
        var timestamp = "2026-07-24T00:00:00Z";

        var records = new List<object>
        {
            new
            {
                timestamp,
                type = "session_meta",
                payload = new
                {
                    meta = new
                    {
                        id = threadId,
                        session_id = threadId,
                        cwd = workingDirectory,
                        parent_thread_id = isSubagent ? "parent-thread" : null,
                        agent_role = isSubagent ? "worker" : null
                    }
                }
            },
            new
            {
                timestamp,
                type = "response_item",
                payload = new
                {
                    type = "message",
                    role = "user",
                    content = new[] { new { type = "input_text", text = $"Plan {fileName}" } }
                }
            },
            new
            {
                timestamp,
                type = "response_item",
                payload = new
                {
                    type = "message",
                    role = "assistant",
                    content = new[] { new { type = "output_text", text = $"Working on {fileName}" } }
                }
            }
        };

        if (includeMirroredEvents)
        {
            records.Add(new { timestamp, type = "event_msg", payload = new { type = "user_message", message = $"Plan {fileName}" } });
            records.Add(new { timestamp, type = "event_msg", payload = new { type = "agent_message", message = $"Working on {fileName}" } });
        }

        File.WriteAllLines(path, records.Select(JsonSerializer.Serialize));
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
        return path;
    }
}
