using Microsoft.UI.Xaml.Controls;

namespace Confessio.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();

        // Get app version from assembly
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version != null ? $"Version {version.Major}.{version.Minor}.{version.Build}" : "Version 1.0";

        // Display library stats
        DocumentCountText.Text = AppState.AllDocuments.Count.ToString();
        CollectionCountText.Text = AppState.Collections.Count.ToString();
    }
}