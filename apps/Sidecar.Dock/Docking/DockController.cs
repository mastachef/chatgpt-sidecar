using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ChatGPT.Sidecar.Dock.Interop;
using ChatGPT.Sidecar.Dock.WindowDetection;

namespace ChatGPT.Sidecar.Dock.Docking;

internal sealed class DockController : IDisposable
{
    private readonly Window _sidecarWindow;
    private readonly WindowTargetPicker _targetPicker = new();
    private readonly DispatcherTimer _timer;
    private nint _sidecarHandle;
    private CodexWindow? _manualTarget;
    private CodexWindow? _lastDockedWindow;
    private bool _disposed;
    private string? _lastStatus;
    private string? _lastTargetLabel;

    public DockController(Window sidecarWindow)
    {
        _sidecarWindow = sidecarWindow;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _timer.Tick += (_, _) => UpdateDockPosition();
    }

    // Kept for source compatibility with the previous constructor. The locator is deliberately ignored:
    // Sidecar no longer auto-selects or auto-magnets to any window.
    public DockController(Window sidecarWindow, CodexWindowLocator _) : this(sidecarWindow)
    {
    }

    public bool IsFollowing { get; set; } = true;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? TargetChanged;

    public void Start()
    {
        _sidecarHandle = new WindowInteropHelper(_sidecarWindow).EnsureHandle();
        _timer.Start();
        ReportTarget("No target selected");
        ReportStatus("Drag Attach onto the Codex window to pin Sidecar.");
    }

    public bool TryAttachAtCursor(out string result)
    {
        if (_sidecarHandle == nint.Zero)
        {
            result = "Sidecar is not ready to attach yet.";
            return false;
        }

        if (!_targetPicker.TryPickAtCursor(_sidecarHandle, out var target, out var failure) || target is null)
        {
            result = failure;
            return false;
        }

        _manualTarget = target;
        _lastDockedWindow = null;
        IsFollowing = true;

        var label = $"Pinned: {target.Title}";
        ReportTarget(label);
        ReportStatus($"Attached to {target.Title}");
        UpdateDockPosition();
        result = label;
        return true;
    }

    // Legacy call target retained only so older code still compiles. It clears the pin and does not perform detection.
    public void UseAutomaticTargeting()
    {
        _manualTarget = null;
        _lastDockedWindow = null;
        ReportTarget("No target selected");
        ReportStatus("Automatic targeting is disabled. Drag Attach onto the Codex window.");
    }

    private void UpdateDockPosition()
    {
        if (!IsFollowing || _sidecarHandle == nint.Zero)
        {
            return;
        }

        var target = ResolveTarget();
        if (target is null)
        {
            return;
        }

        if (NativeMethods.IsIconic(target.Handle))
        {
            ReportStatus("Pinned Codex window is minimized");
            return;
        }

        if (!NativeMethods.GetWindowRect(target.Handle, out var targetRect) || targetRect.Width < 200 || targetRect.Height < 200)
        {
            ReportStatus("Pinned window bounds are unavailable");
            return;
        }

        var monitor = NativeMethods.MonitorFromWindow(target.Handle, NativeMethods.MonitorDefaultToNearest);
        var monitorInfo = new NativeMethods.MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
        {
            ReportStatus("Pinned window monitor information is unavailable");
            return;
        }

        var source = PresentationSource.FromVisual(_sidecarWindow);
        var scaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var desiredWidth = Math.Max(380, (int)Math.Round(_sidecarWindow.ActualWidth * scaleX));
        var workArea = monitorInfo.WorkArea;

        int x;
        string side;
        if (workArea.Right - targetRect.Right >= desiredWidth)
        {
            x = targetRect.Right;
            side = "right";
        }
        else if (targetRect.Left - workArea.Left >= desiredWidth)
        {
            x = targetRect.Left - desiredWidth;
            side = "left";
        }
        else
        {
            x = Math.Max(workArea.Left, targetRect.Right - desiredWidth);
            side = "overlay-right";
        }

        var y = Math.Max(workArea.Top, targetRect.Top);
        var height = Math.Min(targetRect.Height, workArea.Bottom - y);
        _ = NativeMethods.SetWindowPos(
            _sidecarHandle,
            nint.Zero,
            x,
            y,
            desiredWidth,
            height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpNoZOrder | NativeMethods.SwpShowWindow);

        if (_lastDockedWindow?.Handle != target.Handle || _lastDockedWindow?.Title != target.Title)
        {
            _lastDockedWindow = target;
            ReportTarget($"Pinned: {target.Title}");
        }

        ReportStatus($"Docked {side} of {target.Title}");
    }

    private CodexWindow? ResolveTarget()
    {
        if (_manualTarget is null)
        {
            return null;
        }

        if (!NativeMethods.IsWindow(_manualTarget.Handle))
        {
            _manualTarget = null;
            _lastDockedWindow = null;
            ReportTarget("Pinned target closed — drag Attach onto Codex again");
            ReportStatus("Pinned target is no longer available");
            return null;
        }

        var currentTitle = NativeMethods.ReadWindowTitle(_manualTarget.Handle).Trim();
        if (!string.IsNullOrWhiteSpace(currentTitle) && currentTitle != _manualTarget.Title)
        {
            _manualTarget = _manualTarget with { Title = currentTitle };
        }

        return _manualTarget;
    }

    private void ReportStatus(string status)
    {
        if (string.Equals(_lastStatus, status, StringComparison.Ordinal))
        {
            return;
        }

        _lastStatus = status;
        StatusChanged?.Invoke(this, status);
    }

    private void ReportTarget(string target)
    {
        if (string.Equals(_lastTargetLabel, target, StringComparison.Ordinal))
        {
            return;
        }

        _lastTargetLabel = target;
        TargetChanged?.Invoke(this, target);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
    }
}
