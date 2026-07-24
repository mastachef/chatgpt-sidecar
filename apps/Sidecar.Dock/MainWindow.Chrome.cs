using System.Windows;
using System.Windows.Input;

namespace ChatGPT.Sidecar.Dock;

public partial class MainWindow
{
    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximizedState();
            e.Handled = true;
            return;
        }

        try
        {
            // Restore first so dragging a maximized Sidecar behaves like a normal desktop window.
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }

            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove can throw when the mouse button is released between the event and the call.
        }

        e.Handled = true;
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximizedState();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        UpdateCaptionState();
    }

    private void ToggleMaximizedState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        UpdateCaptionState();
    }

    private void UpdateCaptionState()
    {
        if (MaximizeButton is null)
        {
            return;
        }

        var maximized = WindowState == WindowState.Maximized;
        MaximizeButton.Content = maximized ? "❐" : "□";
        MaximizeButton.ToolTip = maximized ? "Restore" : "Maximize";
    }
}
