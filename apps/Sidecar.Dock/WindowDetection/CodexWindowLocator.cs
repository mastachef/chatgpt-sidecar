using System.Diagnostics;
using ChatGPT.Sidecar.Dock.Interop;

namespace ChatGPT.Sidecar.Dock.WindowDetection;

internal sealed record CodexWindow(nint Handle, int ProcessId, string ProcessName, string Title);

internal sealed class CodexWindowLocator
{
    private readonly int _currentProcessId = Environment.ProcessId;

    public CodexWindow? FindBestWindow()
    {
        CodexWindow? best = null;
        var bestScore = 0;

        NativeMethods.EnumWindows((handle, lParam) =>
        {
            if (!NativeMethods.IsWindowVisible(handle) || NativeMethods.IsIconic(handle))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(handle, out var rawProcessId);
            var processId = unchecked((int)rawProcessId);
            if (processId <= 0 || processId == _currentProcessId)
            {
                return true;
            }

            string processName;
            try
            {
                processName = Process.GetProcessById(processId).ProcessName;
            }
            catch
            {
                return true;
            }

            var title = NativeMethods.ReadWindowTitle(handle);
            var score = Score(processName, title);
            if (score > bestScore)
            {
                bestScore = score;
                best = new CodexWindow(handle, processId, processName, title);
            }

            return true;
        }, nint.Zero);

        return best;
    }

    private static int Score(string processName, string title)
    {
        if (processName.Contains("sidecar", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Sidecar", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var score = 0;
        if (processName.Contains("codex", StringComparison.OrdinalIgnoreCase)) score += 140;
        if (processName.Contains("chatgpt", StringComparison.OrdinalIgnoreCase)) score += 80;
        if (processName.Contains("openai", StringComparison.OrdinalIgnoreCase)) score += 50;
        if (title.Contains("Codex", StringComparison.OrdinalIgnoreCase)) score += 160;
        if (title.Contains("ChatGPT", StringComparison.OrdinalIgnoreCase)) score += 50;

        // Ignore invisible utility windows from Chromium/WebView processes.
        if (string.IsNullOrWhiteSpace(title)) score -= 200;
        return score;
    }
}
