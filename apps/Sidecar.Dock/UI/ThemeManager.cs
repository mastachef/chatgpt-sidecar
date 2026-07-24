using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace ChatGPT.Sidecar.Dock.UI;

internal sealed record ThemeOption(string Id, string Name)
{
    public override string ToString() => Name;
}

internal static class ThemeManager
{
    private sealed record ThemePalette(
        string Window,
        string Surface,
        string SurfaceRaised,
        string Input,
        string Border,
        string Primary,
        string Secondary,
        string Accent,
        string AccentHover,
        string AccentText,
        string AccentSoft,
        string BrowserBackdrop,
        string Danger);

    private sealed record ThemeSettings(string Theme);

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTSidecar",
        "Settings",
        "ui.json");

    internal static readonly ThemeOption[] Options =
    [
        new("codex-green", "Codex Green"),
        new("codex-dark", "Codex Dark"),
        new("midnight", "Midnight"),
        new("light", "Light"),
        new("system", "System")
    ];

    internal static string ApplySavedTheme()
    {
        var saved = "codex-green";
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(SettingsPath));
                if (!string.IsNullOrWhiteSpace(settings?.Theme))
                {
                    saved = settings.Theme;
                }
            }
        }
        catch
        {
            // A corrupt preference should never prevent Sidecar from opening.
        }

        Apply(saved, persist: false);
        return saved;
    }

    internal static void Apply(string themeId, bool persist = true)
    {
        var resolvedId = themeId == "system" ? ResolveSystemTheme() : themeId;
        var palette = resolvedId switch
        {
            "light" => new ThemePalette(
                "#F5F7F5", "#FFFFFF", "#EEF3EF", "#FFFFFF", "#CCD7CE",
                "#102116", "#607267", "#147A3D", "#0F6533", "#FFFFFF",
                "#DDF2E4", "#F8FAF8", "#C83D4C"),
            "midnight" => new ThemePalette(
                "#050816", "#0A1024", "#111A35", "#080E20", "#24305B",
                "#E4E9FF", "#8791B8", "#8B7CFF", "#A499FF", "#080B18",
                "#1A2148", "#060A18", "#FF6678"),
            "codex-dark" => new ThemePalette(
                "#090B0A", "#111513", "#171D1A", "#0D110F", "#2A332E",
                "#E7ECE8", "#8D9A91", "#65D68A", "#80E8A0", "#071009",
                "#193121", "#080A09", "#FF6B75"),
            _ => new ThemePalette(
                "#020703", "#0B140E", "#101E15", "#08110C", "#1E3927",
                "#B9FFC9", "#5F8A69", "#74F59B", "#91FFAF", "#021006",
                "#132C1B", "#020703", "#FF6E7D")
        };

        var resources = Application.Current.Resources;
        SetBrush(resources, "WindowBackgroundBrush", palette.Window);
        SetBrush(resources, "SurfaceBrush", palette.Surface);
        SetBrush(resources, "SurfaceRaisedBrush", palette.SurfaceRaised);
        SetBrush(resources, "InputBackgroundBrush", palette.Input);
        SetBrush(resources, "BorderBrush", palette.Border);
        SetBrush(resources, "PrimaryTextBrush", palette.Primary);
        SetBrush(resources, "SecondaryTextBrush", palette.Secondary);
        SetBrush(resources, "AccentBrush", palette.Accent);
        SetBrush(resources, "AccentHoverBrush", palette.AccentHover);
        SetBrush(resources, "AccentTextBrush", palette.AccentText);
        SetBrush(resources, "AccentSoftBrush", palette.AccentSoft);
        SetBrush(resources, "BrowserBackdropBrush", palette.BrowserBackdrop);
        SetBrush(resources, "DangerBrush", palette.Danger);

        if (persist)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new ThemeSettings(themeId)));
            }
            catch
            {
                // Theme changes still apply even when settings cannot be persisted.
            }
        }
    }

    private static string ResolveSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value != 0 ? "light" : "codex-dark";
        }
        catch
        {
            return "codex-dark";
        }
    }

    private static void SetBrush(ResourceDictionary resources, string key, string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        resources[key] = brush;
    }
}