using System.IO;
using System.Windows;
using System.Windows.Controls;
using ChatGPT.Sidecar.Dock.Browser;
using ChatGPT.Sidecar.Dock.CodexContext;
using ChatGPT.Sidecar.Dock.Docking;
using ChatGPT.Sidecar.Dock.RepositoryContext;
using ChatGPT.Sidecar.Dock.WindowDetection;

namespace ChatGPT.Sidecar.Dock;

public partial class MainWindow : Window
{
    private readonly CodexSessionReader _sessionReader = new();
    private readonly RepositoryContextCollector _repositoryCollector = new();
    private readonly ContextPackageBuilder _packageBuilder = new();
    private readonly DockController _dockController;
    private readonly ChatGptWebViewController _chatGptController;
    private string? _latestContextPackage;

    public MainWindow()
    {
        InitializeComponent();
        _dockController = new DockController(this, new CodexWindowLocator());
        _chatGptController = new ChatGptWebViewController(ChatGptWebView);
        _dockController.StatusChanged += (_, status) => Dispatcher.Invoke(() => StatusText.Text = status);
        Loaded += MainWindow_OnLoaded;
        Closed += (_, _) => _dockController.Dispose();
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Loading ChatGPT...";
            await _chatGptController.InitializeAsync();
            StatusText.Text = "ChatGPT loaded. Sign in once if requested.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "ChatGPT WebView failed to initialize";
            MessageBox.Show(
                $"WebView2 could not start. Install the Microsoft Edge WebView2 Runtime and restart Sidecar.\n\n{exception.Message}",
                "ChatGPT Sidecar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        _dockController.Start();
        RefreshDetectedContext();
    }

    private async void PrepareButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            PrepareButton.IsEnabled = false;
            StatusText.Text = "Reading Codex session and repository...";
            var context = BuildContextPackage();
            _latestContextPackage = context.Package;
            ContextStatsText.Text = $"{context.Package.Length:N0} chars";
            DetectedContextText.Text = $"{context.Session.Title} — {Path.GetFileName(context.Repository.Root)}";

            StatusText.Text = "Populating ChatGPT composer...";
            var populated = await _chatGptController.TryPopulateComposerAsync(context.Package);
            if (populated)
            {
                StatusText.Text = "Context populated. Review it in ChatGPT, then press Send.";
            }
            else
            {
                Clipboard.SetText(context.Package);
                StatusText.Text = "Composer not found. Context copied to clipboard.";
                MessageBox.Show(
                    "Sidecar could not locate ChatGPT's composer. The prepared context was copied to your clipboard so no data was typed into an unknown field.",
                    "ChatGPT Sidecar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception exception)
        {
            StatusText.Text = "Could not prepare Codex context";
            MessageBox.Show(exception.Message, "ChatGPT Sidecar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PrepareButton.IsEnabled = true;
        }
    }

    private void PreviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_latestContextPackage))
            {
                var context = BuildContextPackage();
                _latestContextPackage = context.Package;
                ContextStatsText.Text = $"{context.Package.Length:N0} chars";
            }

            var preview = new Window
            {
                Title = "Sidecar context preview",
                Owner = this,
                Width = 760,
                Height = 760,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBox
                {
                    Text = _latestContextPackage,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    TextWrapping = TextWrapping.NoWrap,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono")
                }
            };
            preview.ShowDialog();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "ChatGPT Sidecar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private (string Package, CodexSession Session, RepositorySnapshot Repository) BuildContextPackage()
    {
        var session = _sessionReader.FindLatestRootSession()
            ?? throw new InvalidOperationException("No saved Codex conversation was found under CODEX_HOME/sessions. Open a Codex project and send at least one normal Codex message first.");
        var workingDirectory = Directory.Exists(session.WorkingDirectory)
            ? session.WorkingDirectory!
            : Environment.CurrentDirectory;
        var repository = _repositoryCollector.Collect(workingDirectory);
        var request = RequestBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(request))
        {
            throw new InvalidOperationException("Enter a request for ChatGPT.");
        }

        var mode = (ModeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "general";
        var package = _packageBuilder.Build(mode, request, session, repository);
        return (package, session, repository);
    }

    private void RefreshDetectedContext()
    {
        var session = _sessionReader.FindLatestRootSession();
        DetectedContextText.Text = session is null
            ? "No saved Codex conversation detected"
            : $"{session.Title} — {Path.GetFileName(session.WorkingDirectory ?? string.Empty)}";
    }

    private void FollowCodexToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        _dockController.IsFollowing = FollowCodexToggle.IsChecked == true;
        StatusText.Text = _dockController.IsFollowing ? "Following Codex window" : "Sidecar detached";
    }

    private void ReloadChatGpt_OnClick(object sender, RoutedEventArgs e)
    {
        _chatGptController.Reload();
        RefreshDetectedContext();
    }
}
