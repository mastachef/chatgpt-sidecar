using System.Windows;
using System.Windows.Controls;
using ChatGPT.Sidecar.Dock.Updates;

namespace ChatGPT.Sidecar.Dock;

public partial class MainWindow
{
    private readonly UpdateService _updateService = new();
    private Button? _updateButton;
    private SidecarUpdate? _availableUpdate;
    private bool _updateCheckRunning;

    private void InitializeUpdaterUi()
    {
        if (_updateButton is not null || ThemeBox.Parent is not Grid headerGrid)
        {
            return;
        }

        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _updateButton = new Button
        {
            Content = "Updates",
            Margin = new Thickness(2, 0, 0, 0),
            ToolTip = $"Check for Sidecar updates (current {SidecarVersion.Current})"
        };
        _updateButton.SetResourceReference(FrameworkElement.StyleProperty, "QuietButtonStyle");
        Grid.SetColumn(_updateButton, headerGrid.ColumnDefinitions.Count - 1);
        _updateButton.Click += UpdateButton_OnClick;
        headerGrid.Children.Add(_updateButton);

        Loaded += async (_, _) => await CheckForUpdatesAsync(interactive: false);
    }

    private async void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_availableUpdate is null)
            {
                await CheckForUpdatesAsync(interactive: true);
            }

            if (_availableUpdate is null)
            {
                return;
            }

            var update = _availableUpdate;
            var choice = MessageBox.Show(
                $"Sidecar {update.VersionText} is available.\n\nDownload the GitHub release, verify its SHA-256 digest and trusted Windows signature, then replace this copy and restart Sidecar?",
                "Sidecar update available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (choice != MessageBoxResult.Yes)
            {
                return;
            }

            SetUpdateButtonState($"Installing {update.VersionText}…", enabled: false);
            StatusText.Text = $"Downloading and verifying Sidecar {update.VersionText}…";
            _diagnostics.Record("update.install.started", ("version", update.VersionText));

            await _updateService.StageAndLaunchUpdateAsync(update);

            _diagnostics.Record("update.install.staged", ("version", update.VersionText));
            StatusText.Text = "Update verified. Restarting Sidecar…";
            await Task.Delay(250);
            Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            _diagnostics.RecordException("update.install.failed", exception);
            SetUpdateButtonState(_availableUpdate is null ? "Updates" : $"Update {_availableUpdate.VersionText}", enabled: true);
            StatusText.Text = "Sidecar update failed";
            MessageBox.Show(
                $"Sidecar could not install the update. Nothing was replaced.\n\n{exception.Message}",
                "Sidecar update failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task CheckForUpdatesAsync(bool interactive)
    {
        if (_updateCheckRunning || _updateButton is null)
        {
            return;
        }

        _updateCheckRunning = true;
        try
        {
            SetUpdateButtonState("Checking…", enabled: false);
            var update = await _updateService.CheckForUpdateAsync();
            _availableUpdate = update;

            if (update is null)
            {
                SetUpdateButtonState("Updates", enabled: true);
                _updateButton.ToolTip = $"Sidecar {SidecarVersion.Current} is up to date";
                _diagnostics.Record("update.check.current", ("version", SidecarVersion.Current));
                if (interactive)
                {
                    MessageBox.Show(
                        $"You're up to date. Sidecar {SidecarVersion.Current} is the newest published release.",
                        "Sidecar updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return;
            }

            SetUpdateButtonState($"Update {update.VersionText}", enabled: true, primary: true);
            _updateButton.ToolTip = $"Sidecar {update.VersionText} is available — click to install";
            _diagnostics.Record("update.available", ("current", SidecarVersion.Current), ("available", update.VersionText));
        }
        catch (Exception exception)
        {
            SetUpdateButtonState("Updates", enabled: true);
            _updateButton.ToolTip = "Could not check GitHub for updates. Click to retry.";
            _diagnostics.RecordException("update.check.failed", exception);
            if (interactive)
            {
                MessageBox.Show(
                    $"Sidecar could not check GitHub for updates.\n\n{exception.Message}",
                    "Sidecar updates",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        finally
        {
            _updateCheckRunning = false;
        }
    }

    private void SetUpdateButtonState(string content, bool enabled, bool primary = false)
    {
        if (_updateButton is null)
        {
            return;
        }

        _updateButton.Content = content;
        _updateButton.IsEnabled = enabled;
        _updateButton.SetResourceReference(
            FrameworkElement.StyleProperty,
            primary ? "PrimaryButtonStyle" : "QuietButtonStyle");
    }
}
