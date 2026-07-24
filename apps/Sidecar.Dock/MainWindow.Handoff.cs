using System.Windows;
using ChatGPT.Sidecar.Dock.CodexContext;

namespace ChatGPT.Sidecar.Dock;

public partial class MainWindow
{
    private async void PrepareCodexHandoffButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            PrepareHandoffButton.IsEnabled = false;
            var prompt = CodexHandoffPromptBuilder.Build(SelectedSession, RequestBox.Text);
            StatusText.Text = "Preparing a detailed return-to-Codex handoff request...";

            var result = await _chatGptController.TryPopulateComposerAsync(prompt);
            if (result.Success)
            {
                StatusText.Text = "Codex handoff request populated. Send it in ChatGPT, then use Copy latest reply.";
                _diagnostics.Record("handoff.prompt.populated", ("characters", prompt.Length));
                return;
            }

            Clipboard.SetText(prompt);
            StatusText.Text = $"Handoff composer unavailable ({result.Reason}). Prompt copied to clipboard.";
            _diagnostics.Record("handoff.prompt.clipboard_fallback", ("reason", result.Reason));
            MessageBox.Show(
                "Sidecar could not safely locate ChatGPT's composer. The detailed Codex handoff request was copied to your clipboard instead.",
                "Sidecar",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception exception)
        {
            _diagnostics.RecordException("handoff.prompt.failed", exception);
            StatusText.Text = "Could not prepare the Codex handoff request";
            MessageBox.Show(exception.Message, "Sidecar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PrepareHandoffButton.IsEnabled = true;
        }
    }

    private async void CopyLatestReplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            CopyHandoffButton.IsEnabled = false;
            StatusText.Text = "Reading ChatGPT's latest completed reply...";
            var result = await _chatGptController.TryReadLatestAssistantMessageAsync();
            if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
            {
                Clipboard.SetText(result.Text);
                StatusText.Text = $"Latest ChatGPT reply copied ({result.Text.Length:N0} chars). Paste it into Codex.";
                _diagnostics.Record(
                    "handoff.reply.copied",
                    ("characters", result.Text.Length),
                    ("selector", result.Selector));
                return;
            }

            StatusText.Text = $"Could not find a completed ChatGPT reply ({result.Reason}).";
            _diagnostics.Record("handoff.reply.copy_failed", ("reason", result.Reason));
            MessageBox.Show(
                "Sidecar could not safely identify ChatGPT's latest assistant response. Wait for the handoff response to finish, then try again. You can still copy it manually from ChatGPT.",
                "Sidecar",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception exception)
        {
            _diagnostics.RecordException("handoff.reply.copy_exception", exception);
            StatusText.Text = "Could not copy the latest ChatGPT reply";
            MessageBox.Show(exception.Message, "Sidecar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CopyHandoffButton.IsEnabled = true;
        }
    }
}
