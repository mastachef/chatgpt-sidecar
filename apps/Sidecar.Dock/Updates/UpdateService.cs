using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace ChatGPT.Sidecar.Dock.Updates;

internal sealed record SidecarUpdate(
    string Tag,
    string VersionText,
    Uri DownloadUri,
    string Sha256,
    Uri ReleaseUri);

internal sealed class UpdateService
{
    private const string ReleasesApi = "https://api.github.com/repos/mastachef/chatgpt-sidecar/releases?per_page=10";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    internal async Task<SidecarUpdate?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (!SidecarVersion.TryParse(SidecarVersion.Current, out var current))
        {
            throw new InvalidOperationException($"Sidecar's current version '{SidecarVersion.Current}' is invalid.");
        }

        using var response = await HttpClient.GetAsync(ReleasesApi, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("GitHub returned an unexpected releases response.");
        }

        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draft) && draft.GetBoolean())
            {
                continue;
            }

            var tag = release.GetProperty("tag_name").GetString();
            if (!SidecarVersion.TryParse(tag, out var available) || available.CompareTo(current) <= 0)
            {
                continue;
            }

            var releaseUrl = release.TryGetProperty("html_url", out var htmlUrl)
                ? htmlUrl.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(releaseUrl))
            {
                continue;
            }

            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var asset in assets.EnumerateArray())
            {
                if (!string.Equals(asset.GetProperty("name").GetString(), "Sidecar.exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var state = asset.TryGetProperty("state", out var stateProperty) ? stateProperty.GetString() : null;
                var downloadUrl = asset.TryGetProperty("browser_download_url", out var downloadProperty)
                    ? downloadProperty.GetString()
                    : null;
                var digest = asset.TryGetProperty("digest", out var digestProperty)
                    ? digestProperty.GetString()
                    : null;

                if (!string.Equals(state, "uploaded", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(downloadUrl)
                    || string.IsNullOrWhiteSpace(digest)
                    || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sha256 = digest["sha256:".Length..].Trim();
                if (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character)))
                {
                    continue;
                }

                var normalizedTag = tag!.Trim();
                return new SidecarUpdate(
                    normalizedTag,
                    normalizedTag.TrimStart('v', 'V'),
                    new Uri(downloadUrl),
                    sha256.ToLowerInvariant(),
                    new Uri(releaseUrl));
            }
        }

        return null;
    }

    internal async Task StageAndLaunchUpdateAsync(SidecarUpdate update, CancellationToken cancellationToken = default)
    {
        var currentExecutable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Sidecar could not determine the path of the running executable.");
        if (!File.Exists(currentExecutable))
        {
            throw new InvalidOperationException("Sidecar could not find the running executable on disk.");
        }

        var updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTSidecar",
            "Updates",
            update.Tag.TrimStart('v', 'V'));
        Directory.CreateDirectory(updateDirectory);

        var downloadedExecutable = Path.Combine(updateDirectory, "Sidecar.exe.new");
        await DownloadFileAsync(update.DownloadUri, downloadedExecutable, cancellationToken).ConfigureAwait(false);

        var actualSha256 = await ComputeSha256Async(downloadedExecutable, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(actualSha256, update.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            TryDelete(downloadedExecutable);
            throw new InvalidOperationException("The downloaded Sidecar update failed its SHA-256 integrity check.");
        }

        var newSigner = await GetAuthenticodeSignerAsync(downloadedExecutable, requireValid: true, cancellationToken).ConfigureAwait(false);
        var currentSigner = await GetAuthenticodeSignerAsync(currentExecutable, requireValid: false, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(currentSigner)
            && !string.Equals(currentSigner, newSigner, StringComparison.OrdinalIgnoreCase))
        {
            TryDelete(downloadedExecutable);
            throw new InvalidOperationException(
                $"The update was signed by a different publisher than the running Sidecar build. Current: {currentSigner}. Update: {newSigner}.");
        }

        LaunchReplacementScript(downloadedExecutable, currentExecutable, Environment.ProcessId, updateDirectory);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Sidecar-Updater/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static async Task DownloadFileAsync(Uri uri, string destination, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<string?> GetAuthenticodeSignerAsync(
        string path,
        bool requireValid,
        CancellationToken cancellationToken)
    {
        var script = requireValid
            ? "$s=Get-AuthenticodeSignature -LiteralPath $args[0]; if($s.Status -ne 'Valid' -or $null -eq $s.SignerCertificate){[Console]::Error.Write($s.Status); exit 2}; [Console]::Out.Write($s.SignerCertificate.Subject)"
            : "$s=Get-AuthenticodeSignature -LiteralPath $args[0]; if($s.Status -ne 'Valid' -or $null -eq $s.SignerCertificate){exit 0}; [Console]::Out.Write($s.SignerCertificate.Subject)";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("-NoLogo");
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-Command");
        process.StartInfo.ArgumentList.Add(script);
        process.StartInfo.ArgumentList.Add(path);

        if (!process.Start())
        {
            throw new InvalidOperationException("Windows could not start Authenticode verification.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var output = (await outputTask.ConfigureAwait(false)).Trim();
        var error = (await errorTask.ConfigureAwait(false)).Trim();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"The downloaded Sidecar update does not have a valid trusted Authenticode signature{(string.IsNullOrWhiteSpace(error) ? "." : $": {error}")}");
        }

        return string.IsNullOrWhiteSpace(output) ? null : output;
    }

    private static void LaunchReplacementScript(string source, string target, int processId, string updateDirectory)
    {
        var scriptPath = Path.Combine(updateDirectory, "apply-update.ps1");
        var logPath = Path.Combine(updateDirectory, "apply-update.log");
        const string script = """
$ErrorActionPreference = 'Stop'
$pidToWait = [int]$args[0]
$source = $args[1]
$target = $args[2]
$log = $args[3]
$backup = "$target.old"
try {
    Wait-Process -Id $pidToWait -Timeout 60 -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $backup) { Remove-Item -LiteralPath $backup -Force }
    if (Test-Path -LiteralPath $target) { Move-Item -LiteralPath $target -Destination $backup -Force }
    try {
        Move-Item -LiteralPath $source -Destination $target -Force
    }
    catch {
        if ((-not (Test-Path -LiteralPath $target)) -and (Test-Path -LiteralPath $backup)) {
            Move-Item -LiteralPath $backup -Destination $target -Force
        }
        throw
    }
    Start-Process -FilePath $target -WorkingDirectory (Split-Path -Parent $target)
    if (Test-Path -LiteralPath $backup) { Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue }
    if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue }
}
catch {
    $_ | Out-String | Set-Content -LiteralPath $log -Encoding UTF8
    exit 1
}
""";
        File.WriteAllText(scriptPath, script);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(target) ?? Environment.CurrentDirectory
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-WindowStyle");
        startInfo.ArgumentList.Add("Hidden");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add(processId.ToString());
        startInfo.ArgumentList.Add(source);
        startInfo.ArgumentList.Add(target);
        startInfo.ArgumentList.Add(logPath);

        if (Process.Start(startInfo) is null)
        {
            throw new InvalidOperationException("Windows could not start the Sidecar updater.");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Cleanup failure should not hide the actual update failure.
        }
    }
}
