using System.Diagnostics;
using ChatGPT.Sidecar.Dock.Interop;

namespace ChatGPT.Sidecar.Dock.WindowDetection;

internal sealed class WindowTargetPicker
{
    private readonly int _currentProcessId = Environment.ProcessId;

    public bool TryPickAtCursor(nint sidecarHandle, out CodexWindow? target, out string failure)
    {
        target = null;
        failure = string.Empty;

        if (!NativeMethods.GetCursorPos(out var cursor))
        {
            failure = "Windows could not read the cursor position.";
            return false;
        }

        var hit = NativeMethods.WindowFromPoint(cursor);
        var root = hit == nint.Zero ? nint.Zero : NativeMethods.GetAncestor(hit, NativeMethods.GaRoot);
        if (root == nint.Zero)
        {
            failure = "No application window was found under the cursor.";
            return false;
        }

        if (root == sidecarHandle)
        {
            failure = "Release the Attach handle over the Codex window, not over Sidecar.";
            return false;
        }

        if (!NativeMethods.IsWindow(root) || !NativeMethods.IsWindowVisible(root))
        {
            failure = "The selected window is no longer available.";
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(root, out var rawProcessId);
        var processId = unchecked((int)rawProcessId);
        if (processId <= 0 || processId == _currentProcessId)
        {
            failure = "Sidecar cannot attach to its own window.";
            return false;
        }

        if (!NativeMethods.GetWindowRect(root, out var rect) || rect.Width < 200 || rect.Height < 200)
        {
            failure = "The selected target is not a normal application window.";
            return false;
        }

        string processName;
        try
        {
            processName = Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            failure = "Windows could not identify the selected application.";
            return false;
        }

        var title = NativeMethods.ReadWindowTitle(root).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            failure = "The selected target has no window title. Release over the visible Codex window.";
            return false;
        }

        target = new CodexWindow(root, processId, processName, title);
        return true;
    }
}