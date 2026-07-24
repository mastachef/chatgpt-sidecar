using System.Windows;
using System.Windows.Controls;
using ChatGPT.Sidecar.Dock.UI;

namespace ChatGPT.Sidecar.Dock;

public partial class MainWindow
{
    private bool _themePickerInitialized;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_themePickerInitialized)
        {
            return;
        }

        _themePickerInitialized = true;
        ThemeBox.ItemsSource = ThemeManager.Options;
        ThemeBox.SelectedItem = ThemeManager.Options.FirstOrDefault(option => option.Id == ThemeManager.CurrentThemeId)
            ?? ThemeManager.Options[0];
    }

    private void ThemeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_themePickerInitialized || ThemeBox.SelectedItem is not ThemeOption option)
        {
            return;
        }

        ThemeManager.Apply(option.Id);
        NativeWindowTheme.Apply(this);
        _diagnostics.Record("ui.theme.changed", ("theme", option.Id));
        StatusText.Text = $"Theme changed to {option.Name}.";
    }
}
