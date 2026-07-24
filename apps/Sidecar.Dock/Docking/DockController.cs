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
    private readonly DispatcherTimer _timer;
    private nint _sidecarHandle;
    private CodexWindow? _lastCodexWindow;
    private bool _disposed;

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

    public event EventHandler<string>? StatusChanged;

    public void Start()
    {
        _sidecarHandle = new WindowInteropHelper(_sidecarWindow).EnsureHandle();
        _timer.Start();
        UpdateDockPosition();
    }

    private void UpdateDockPosition()
    {
        if (!IsFollowing || _sidecarHandle == nint.Zero)
        {
            return;
        }

        var codex = _locator.FindBestWindow();
        if (codex is null)
        {
            StatusChanged?.Invoke(this, "Codex window not found");
            return;
        }

        if (!NativeMethods.GetWindowRect(codex.Handle, out var targetRect) || targetRect.Width < 200 || targetRect.Height < 200)
        {
            return;
        }

        var monitor = NativeMethods.MonitorFromWindow(codex.Handle, NativeMethods.MonitorDefaultToNearest);
        var monitorInfo = new NativeMethods.MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
        {
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
            // When Codex fills the monitor, overlap its right edge instead of pushing the app off-screen.
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

        if (_lastCodexWindow?.Handle != codex.Handle || _lastCodexWindow?.Title != codex.Title)
        {
            _lastCodexWindow = codex;
            StatusChanged?.Invoke(this, $"Docked {side} of {codex.Title}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
    }
}
