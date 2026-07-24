using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ChatGPT.Sidecar.Dock.Interop;
using ChatGPT.Sidecar.Dock.WindowDetection;

namespace ChatGPT.Sidecar.Dock.Docking;

internal sealed class DockController : IDisposable
{
    private readonly Window _sidecarWindow;
    private readonly CodexWindowLocator _locator;
    private readonly WindowTargetPicker _targetPicker = new();
    private readonly DispatcherTimer _timer;
    private nint _sidecarHandle;
    private CodexWindow? _manualTarget;
    private CodexWindow? _lastDockedWindow;
    private bool _automaticTargeting = true;
    private bool _disposed;
    private string? _lastStatus;
    private string? _lastTargetLabel;

    public DockController(Window sidecarWindow, CodexWindowLocator locator)
    {
        _sidecarWindow = sidecarWindow;
        _locator = locator;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _timer.Tick += (_, _) => UpdateDockPosition();
    }

    public bool IsFollowing { get; set; } = true;

    public bool IsManualTarget => !_automaticTargeting;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? TargetChanged;

    public void Start()
    {
        _sidecarHandle = new WindowInteropHelper(_sidecarWindow).EnsureHandle();
        _timer.Start();
        ReportTarget("Target: automatic detection");
        UpdateDockPosition();
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
        _automaticTargeting = false;
        _lastDockedWindow = null;
        IsFollowing = true;

        var label = $"Pinned: {target.Title}";
        ReportTarget(label);
        ReportStatus($"Attached to {target.Title}");
        UpdateDockPosition();
        result = label;
        return true;
    }

    public void UseAutomaticTargeting()
    {
        _manualTarget = null;
        _automaticTargeting = true;
        _lastDockedWindow = null;
        ReportTarget("Target: automatic detection");
        ReportStatus("Automatic Codex window detection enabled");
        UpdateDockPosition();
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
            ReportStatus(IsManualTarget ? "Pinned window is minimized" : "Codex window is minimized");
            return;
        }

        if (!NativeMethods.GetWindowRect(target.Handle, out var targetRect) || targetRect.Width < 200 || targetRect.Height < 200)
        {
            ReportStatus("Target window bounds are unavailable");
            return;
        }

        var monitor = NativeMethods.MonitorFromWindow(target.Handle, NativeMethods.MonitorDefaultToNearest);
        var monitorInfo = new NativeMethods.MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
        {
            ReportStatus("Target monitor information is unavailable");
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
            // When the selected window fills the monitor, overlap its right edge instead of moving Sidecar off-screen.
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
            var mode = IsManualTarget ? "Pinned" : "Auto-detected";
            ReportTarget($"{mode}: {target.Title}");
        }

        ReportStatus($"Docked {side} of {target.Title}");
    }

    private CodexWindow? ResolveTarget()
    {
        if (!_automaticTargeting)
        {
            if (_manualTarget is null || !NativeMethods.IsWindow(_manualTarget.Handle))
            {
                _manualTarget = null;
                ReportTarget("Pinned target closed — drag Attach to Codex again");
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

        var detected = _locator.FindBestWindow();
        if (detected is null)
        {
            ReportTarget("Target: no Codex window detected");
            ReportStatus("Codex window not found — drag Attach onto it");
        }

        return detected;
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