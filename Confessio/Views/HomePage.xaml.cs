using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

namespace Confessio.Views;

public sealed partial class HomePage : Page
{
    public ObservableCollection<ConfessionDocument> FilteredDocuments { get; } = [];

    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string collection)
        {
            AppState.CurrentCollection = collection == "All" ? "All" : collection;
        }

        RefreshView();
    }

    public void RefreshView()
    {
        string query = (App.Current as App)?.MainWindow?.GetSearchQuery() ?? string.Empty;
        AppState.ApplyFilters(query);

        FilteredDocuments.Clear();
        foreach (ConfessionDocument doc in AppState.FilteredDocuments)
        {
            FilteredDocuments.Add(doc);
        }

        PageTitleText.Text = AppState.CurrentCollection == "All" ? "All confessions" : AppState.CurrentCollection;
        CountText.Text = $"{FilteredDocuments.Count} documents";

        EmptyStatePanel.Visibility = FilteredDocuments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DocumentGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ConfessionDocument document)
        {
            (App.Current as App)?.MainWindow?.NavigateToReader(document);
        }
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < 640;
        SummaryPanel.Margin = narrow ? new Thickness(16, 24, 16, 0) : new Thickness(36, 24, 36, 0);
        DocumentGridView.Padding = narrow ? new Thickness(16, 16, 16, 24) : new Thickness(36, 16, 36, 36);
    }
}
