namespace ChatGPT.Sidecar.Dock.Updates;

internal readonly record struct SidecarVersion(
    int Major,
    int Minor,
    int Patch,
    string? PrereleaseLabel,
    int PrereleaseNumber) : IComparable<SidecarVersion>
{
    internal const string Current = "0.8.1-alpha.8";

    internal static bool TryParse(string? value, out SidecarVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().TrimStart('v', 'V');
        var plusIndex = normalized.IndexOf('+');
        if (plusIndex >= 0)
        {
            normalized = normalized[..plusIndex];
        }

        var parts = normalized.Split('-', 2, StringSplitOptions.RemoveEmptyEntries);
        var core = parts[0].Split('.');
        if (core.Length != 3
            || !int.TryParse(core[0], out var major)
            || !int.TryParse(core[1], out var minor)
            || !int.TryParse(core[2], out var patch))
        {
            return false;
        }

        string? prereleaseLabel = null;
        var prereleaseNumber = 0;
        if (parts.Length == 2)
        {
            var prerelease = parts[1].Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
            if (prerelease.Length == 0)
            {
                return false;
            }

            prereleaseLabel = prerelease[0].ToLowerInvariant();
            if (prerelease.Length == 2 && !int.TryParse(prerelease[1], out prereleaseNumber))
            {
                return false;
            }
        }

        version = new SidecarVersion(major, minor, patch, prereleaseLabel, prereleaseNumber);
        return true;
    }

    public int CompareTo(SidecarVersion other)
    {
        var core = Major.CompareTo(other.Major);
        if (core != 0) return core;
        core = Minor.CompareTo(other.Minor);
        if (core != 0) return core;
        core = Patch.CompareTo(other.Patch);
        if (core != 0) return core;

        if (PrereleaseLabel is null && other.PrereleaseLabel is null) return 0;
        if (PrereleaseLabel is null) return 1;
        if (other.PrereleaseLabel is null) return -1;

        var label = PrereleaseRank(PrereleaseLabel).CompareTo(PrereleaseRank(other.PrereleaseLabel));
        if (label != 0) return label;

        label = string.Compare(PrereleaseLabel, other.PrereleaseLabel, StringComparison.OrdinalIgnoreCase);
        if (label != 0) return label;

        return PrereleaseNumber.CompareTo(other.PrereleaseNumber);
    }

    private static int PrereleaseRank(string label) => label switch
    {
        "alpha" => 0,
        "beta" => 1,
        "rc" => 2,
        _ => -1
    };
}
