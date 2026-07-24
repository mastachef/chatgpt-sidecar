using Xunit;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class UpdateServiceSourceTests
{
    [Fact]
    public void Update_service_requires_digest_and_authenticode_validation()
    {
        var sourcePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "Sidecar.Dock", "Updates", "UpdateService.cs"));
        var source = File.ReadAllText(sourcePath);

        Assert.Contains("sha256:", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SHA256.HashDataAsync", source, StringComparison.Ordinal);
        Assert.Contains("Get-AuthenticodeSignature", source, StringComparison.Ordinal);
        Assert.Contains("requireValid: true", source, StringComparison.Ordinal);
        Assert.Contains("different publisher", source, StringComparison.OrdinalIgnoreCase);
    }
}
