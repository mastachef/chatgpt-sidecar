using System.IO;
using System.Text.Json;
using ChatGPT.Sidecar.Dock.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ChatGPT.Sidecar.Dock.Browser;

internal sealed class ChatGptWebViewController
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly WebView2 _webView;
    private readonly string _profileDirectory;
    private readonly SidecarDiagnostics _diagnostics;

    public ChatGptWebViewController(WebView2 webView, SidecarDiagnostics diagnostics)
    {
        _webView = webView;
        _diagnostics = diagnostics;
        _profileDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTSidecar",
            "WebView2Profile");
    }

    public event EventHandler<string>? StatusChanged;

    public bool IsReady => _webView.CoreWebView2 is not null;

    public string DiagnosticsLogPath => _diagnostics.LogPath;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_profileDirectory);
        _diagnostics.Record("webview.initialize.start", ("profile_exists", Directory.Exists(_profileDirectory)));

        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _profileDirectory);
        _diagnostics.Record("webview.environment.ready", ("browser_version", environment.BrowserVersionString));
        await _webView.EnsureCoreWebView2Async(environment);

        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = true;

        _webView.CoreWebView2.NavigationStarting += (_, args) =>
        {
            var destination = DescribeUri(args.Uri);
            _diagnostics.Record("webview.navigation.start", ("destination", destination));
            StatusChanged?.Invoke(this, $"Loading {destination}...");
        };
        _webView.CoreWebView2.NavigationCompleted += (_, args) =>
        {
            _diagnostics.Record(
                "webview.navigation.complete",
                ("success", args.IsSuccess),
                ("web_error", args.WebErrorStatus),
                ("source", DescribeUri(_webView.Source?.ToString())));
            StatusChanged?.Invoke(
                this,
                args.IsSuccess
                    ? "ChatGPT page loaded. Sign in if requested."
                    : $"ChatGPT navigation failed: {args.WebErrorStatus}");
        };
        _webView.CoreWebView2.ProcessFailed += (_, args) =>
        {
            _diagnostics.Record("webview.process.failed", ("kind", args.ProcessFailedKind));
            StatusChanged?.Invoke(this, $"WebView process failed: {args.ProcessFailedKind}");
        };
        _webView.CoreWebView2.SourceChanged += (_, _) =>
        {
            _diagnostics.Record("webview.source.changed", ("source", DescribeUri(_webView.Source?.ToString())));
        };

        _diagnostics.Record("webview.initialize.complete");
        _webView.Source = new Uri("https://chatgpt.com/");
    }

    public void Reload()
    {
        if (_webView.CoreWebView2 is null)
        {
            _diagnostics.Record("webview.reload.skipped", ("reason", "not_ready"));
            return;
        }

        _diagnostics.Record("webview.reload.requested", ("source", DescribeUri(_webView.Source?.ToString())));
        _webView.CoreWebView2.Reload();
    }

    public async Task<ComposerPopulateResult> TryPopulateComposerAsync(string text)
    {
        if (_webView.CoreWebView2 is null)
        {
            var notReady = ComposerPopulateResult.NotReady("webview_not_ready");
            RecordComposerResult(notReady);
            return notReady;
        }

        var encodedText = JsonSerializer.Serialize(text);
        var script = $$"""
            (() => {
              const selectors = [
                "[data-testid='prompt-textarea']",
                "textarea[placeholder*='Message']",
                "textarea[placeholder*='Ask']",
                "[contenteditable='true'][role='textbox']",
                "div[contenteditable='true']"
              ];

              let candidateCount = 0;
              for (const selector of selectors) {
                const candidates = Array.from(document.querySelectorAll(selector));
                candidateCount += candidates.length;
                const composer = candidates.find((element) => {
                  const rect = element.getBoundingClientRect();
                  const style = window.getComputedStyle(element);
                  return rect.width > 180
                    && rect.height > 20
                    && style.visibility !== 'hidden'
                    && style.display !== 'none';
                });

                if (!composer) continue;
                composer.focus();
                const value = {{encodedText}};

                if (composer instanceof HTMLTextAreaElement || composer instanceof HTMLInputElement) {
                  const prototype = composer instanceof HTMLTextAreaElement
                    ? HTMLTextAreaElement.prototype
                    : HTMLInputElement.prototype;
                  const setter = Object.getOwnPropertyDescriptor(prototype, 'value')?.set;
                  setter?.call(composer, value);
                } else {
                  composer.innerHTML = '';
                  const paragraph = document.createElement('p');
                  paragraph.textContent = value;
                  composer.appendChild(paragraph);
                }

                composer.dispatchEvent(new InputEvent('input', {
                  bubbles: true,
                  inputType: 'insertText',
                  data: value
                }));
                composer.dispatchEvent(new Event('change', { bubbles: true }));

                return {
                  success: true,
                  reason: 'populated',
                  selector,
                  candidateCount,
                  elementTag: composer.tagName.toLowerCase()
                };
              }

              return {
                success: false,
                reason: 'no_visible_composer',
                selector: null,
                candidateCount,
                elementTag: null
              };
            })();
            """;

        try
        {
            var rawResult = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            var result = ParseComposerResult(rawResult)
                ?? new ComposerPopulateResult(false, "invalid_script_result");
            RecordComposerResult(result);
            return result;
        }
        catch (Exception exception)
        {
            _diagnostics.RecordException("composer.populate.exception", exception);
            var failed = new ComposerPopulateResult(false, $"script_exception:{exception.GetType().Name}");
            RecordComposerResult(failed);
            return failed;
        }
    }

    public string BuildDiagnosticsReport() => _diagnostics.BuildReport();

    internal static ComposerPopulateResult? ParseComposerResult(string? rawResult)
    {
        if (string.IsNullOrWhiteSpace(rawResult) || rawResult == "null")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ComposerPopulateResult>(rawResult, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void RecordComposerResult(ComposerPopulateResult result)
    {
        _diagnostics.Record(
            "composer.populate.result",
            ("success", result.Success),
            ("reason", result.Reason),
            ("selector", result.Selector),
            ("candidates", result.CandidateCount),
            ("element", result.ElementTag),
            ("source", DescribeUri(_webView.Source?.ToString())));
    }

    private static string DescribeUri(string? rawUri)
    {
        if (!Uri.TryCreate(rawUri, UriKind.Absolute, out var uri))
        {
            return "unknown";
        }

        var route = uri.AbsolutePath switch
        {
            "/" => "/",
            var path when path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase) => "/auth/*",
            var path when path.StartsWith("/login", StringComparison.OrdinalIgnoreCase) => "/login/*",
            var path when path.StartsWith("/c/", StringComparison.OrdinalIgnoreCase) => "/c/*",
            var path when path.StartsWith("/g/", StringComparison.OrdinalIgnoreCase) => "/g/*",
            _ => "/other"
        };
        return $"{uri.Scheme}://{uri.Host}{route}";
    }
}
