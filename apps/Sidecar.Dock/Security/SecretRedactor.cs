using System.Text.RegularExpressions;

namespace ChatGPT.Sidecar.Dock.Security;

internal static partial class SecretRedactor
{
    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var redacted = PrivateKeyRegex().Replace(value, "[REDACTED PRIVATE KEY]");
        redacted = BearerTokenRegex().Replace(redacted, "$1[REDACTED]");
        redacted = ConnectionStringPasswordRegex().Replace(redacted, "$1[REDACTED]");
        redacted = CredentialUrlRegex().Replace(redacted, "$1[REDACTED]@");
        redacted = OpenAiKeyRegex().Replace(redacted, "[REDACTED OPENAI KEY]");
        redacted = GitHubTokenRegex().Replace(redacted, "[REDACTED GITHUB TOKEN]");
        redacted = AwsAccessKeyRegex().Replace(redacted, "[REDACTED AWS ACCESS KEY]");
        redacted = SecretAssignmentRegex().Replace(redacted, "$1[REDACTED]");
        return redacted;
    }

    [GeneratedRegex("-----BEGIN [A-Z ]*PRIVATE KEY-----.*?-----END [A-Z ]*PRIVATE KEY-----", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex("(?i)(authorization\\s*:\\s*bearer\\s+)[A-Za-z0-9._~+/-]+={0,2}")]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex("(?i)(password\\s*=\\s*)[^;\\r\\n]+")]
    private static partial Regex ConnectionStringPasswordRegex();

    [GeneratedRegex("(?i)(https?://[^\\s/:@]+:)[^\\s/@]+@")]
    private static partial Regex CredentialUrlRegex();

    [GeneratedRegex("\\bsk-[A-Za-z0-9_-]{20,}\\b")]
    private static partial Regex OpenAiKeyRegex();

    [GeneratedRegex("\\bgh[pousr]_[A-Za-z0-9]{20,}\\b", RegexOptions.IgnoreCase)]
    private static partial Regex GitHubTokenRegex();

    [GeneratedRegex("\\bAKIA[0-9A-Z]{16}\\b")]
    private static partial Regex AwsAccessKeyRegex();

    [GeneratedRegex("(?im)^(\\s*[\\\"']?(?:api[_-]?key|secret|token|password|passwd|private[_-]?key|client[_-]?secret|access[_-]?token)[\\\"']?\\s*(?:=|:)\\s*).+$")]
    private static partial Regex SecretAssignmentRegex();
}
