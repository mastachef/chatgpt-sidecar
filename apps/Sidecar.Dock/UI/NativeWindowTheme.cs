using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ChatGPT.Sidecar.Dock.UI;

/// <summary>
/// Keeps the small amount of Windows/DWM-owned non-client chrome in sync with
/// Sidecar's WPF theme. Unsupported attributes are intentionally ignored so
/// the app continues to work on older Windows 10 builds.
/// </summary>
internal static class NativeWindowTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    internal static void Apply(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var surface = GetThemeColor("SurfaceBrush", Colors.Black);
        var border = GetThemeColor("BorderBrush", surface);
        var text = GetThemeColor("PrimaryTextBrush", Colors.White);
        var useDarkChrome = RelativeLuminance(surface) < 0.5;

        var darkMode = useDarkChrome ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref darkMode, sizeof(int));
        }

        SetColorAttribute(hwnd, DwmwaBorderColor, border);
        SetColorAttribute(hwnd, DwmwaCaptionColor, surface);
        SetColorAttribute(hwnd, DwmwaTextColor, text);
    }

    private static Color GetThemeColor(string resourceKey, Color fallback)
    {
        return Application.Current?.Resources[resourceKey] is SolidColorBrush brush
            ? brush.Color
            : fallback;
    }

    private static void SetColorAttribute(IntPtr hwnd, int attribute, Color color)
    {
        // DWM expects COLORREF (0x00BBGGRR), not ARGB.
        var colorRef = color.R | (color.G << 8) | (color.B << 16);
        DwmSetWindowAttribute(hwnd, attribute, ref colorRef, sizeof(int));
    }

    private static double RelativeLuminance(Color color)
    {
        static double Linearize(byte channel)
        {
            var value = channel / 255.0;
            return value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linearize(color.R)
            + 0.7152 * Linearize(color.G)
            + 0.0722 * Linearize(color.B);
    }
}
