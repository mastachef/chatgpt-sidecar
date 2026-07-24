using System.IO;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ChatGPT.Sidecar.Dock.ChatGPT;

internal sealed class ChatGptWebViewController
{
    private readonly WebView2 _webView;
    private readonly string _profileDirectory;

    public ChatGptWebViewController(WebView2 webView)
    {
        _webView = webView;
        _profileDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTSidecar",
            "WebView2Profile");
    }

    public bool IsReady => _webView.CoreWebView2 is not null;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_profileDirectory);
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _profileDirectory);
        await _webView.EnsureCoreWebView2Async(environment);

        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = true;
        _webView.Source = new Uri("https://chatgpt.com/");
    }

    public void Reload()
    {
        if (_webView.CoreWebView2 is null)
        {
            return;
        }

        _webView.CoreWebView2.Reload();
    }

    public async Task<bool> TryPopulateComposerAsync(string text)
    {
        if (_webView.CoreWebView2 is null)
        {
            return false;
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

              let composer = null;
              for (const selector of selectors) {
                const candidates = Array.from(document.querySelectorAll(selector));
                composer = candidates.find((element) => {
                  const rect = element.getBoundingClientRect();
                  const style = window.getComputedStyle(element);
                  return rect.width > 180 && rect.height > 20 && style.visibility !== 'hidden' && style.display !== 'none';
                });
                if (composer) break;
              }

              if (!composer) return false;
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
              return true;
            })();
            """;

        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        return string.Equals(result, "true", StringComparison.OrdinalIgnoreCase);
    }
}
