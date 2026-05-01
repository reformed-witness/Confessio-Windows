using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Confessio.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Confessio;

public sealed partial class MainWindow : Window
{
    private List<SearchSuggestion> _suggestions = [];

    public MainWindow()
    {
        InitializeComponent();
        ConfigureAppWindowTitleBar();
        LoadLibraryAsync();
    }

    public string? GetSearchQuery() => SearchBox.Text;

    private async void LoadLibraryAsync()
    {
        AppState.AllDocuments.Clear();
        AppState.AllDocuments.AddRange(await Task.Run(ConfessionLibrary.Load));
        AppState.LoadCollections();

        foreach (string collection in AppState.Collections)
        {
            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = collection,
                Tag = collection,
                Icon = new FontIcon { Glyph = ConfessionDocument.GetCollectionIcon(collection) }
            });
        }

        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(HomePage), "All");
        Bindings.Update();
    }

    public void NavigateToReader(ConfessionDocument document)
    {
        AppState.CurrentCollection = document.Collection;
        SelectCollection(document.Collection);
        UpdateHeader(document.DisplayTitle);
        ContentFrame.Navigate(typeof(ReaderPage), document);
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        // Check if Settings was clicked
        if (args.InvokedItem?.ToString() == "Settings")
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateToCollection(tag);
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        UpdateSuggestions(sender.Text);
        RefreshActivePage();
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SearchSuggestion suggestion)
        {
            if (suggestion.IsDocument)
            {
                NavigateToReader(suggestion.Document!);
            }
            else if (suggestion.Entry != null && suggestion.Document != null)
            {
                NavigateToReader(suggestion.Document);
                DispatcherQueue.TryEnqueue(() => ScrollToEntryInReader(suggestion.Entry));
            }
        }
        else
        {
            RefreshActivePage();
        }
    }

    private void UpdateSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            SearchBox.ItemsSource = null;
            return;
        }

        _suggestions.Clear();

        var matchingDocs = AppState.AllDocuments
            .Where(d => d.DisplayTitle.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(5);

        foreach (var doc in matchingDocs)
        {
            _suggestions.Add(new SearchSuggestion
            {
                DisplayText = doc.DisplayTitle,
                IsDocument = true,
                Document = doc
            });
        }

        foreach (var doc in AppState.AllDocuments)
        {
            var matchingEntries = doc.Entries
                .Where(e => e.NavigationTitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           (e.GroupTitle?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(3);

            foreach (var entry in matchingEntries)
            {
                string displayText = entry.GroupTitle != null
                    ? $"{entry.NavigationTitle} ({doc.DisplayTitle})"
                    : $"{entry.NavigationTitle} in {doc.DisplayTitle}";

                _suggestions.Add(new SearchSuggestion
                {
                    DisplayText = displayText,
                    IsDocument = false,
                    Document = doc,
                    Entry = entry
                });
            }

            if (_suggestions.Count >= 10) break;
        }

        SearchBox.ItemsSource = _suggestions.Take(10);
    }

    public void ScrollToEntryInReader(ConfessionEntry entry)
    {
        if (ContentFrame.Content is ReaderPage readerPage)
        {
            readerPage.ScrollToEntry(entry);
        }
    }

    private class SearchSuggestion
    {
        public string DisplayText { get; set; } = "";
        public bool IsDocument { get; set; }
        public ConfessionDocument? Document { get; set; }
        public ConfessionEntry? Entry { get; set; }
    }

    private void SearchKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SearchBox.Focus(FocusState.Keyboard);
        args.Handled = true;
    }

    private void RefreshActivePage()
    {
        if (ContentFrame.Content is HomePage homePage)
        {
            homePage.RefreshView();
        }
        else if (ContentFrame.Content is ReaderPage readerPage)
        {
            readerPage.RefreshOutlineFromSearch(SearchBox.Text);
        }
    }

    private void UpdateHeader(string text)
    {
    }

    private void NavigateToCollection(string collection)
    {
        AppState.CurrentCollection = collection;
        SelectCollection(collection);
        UpdateHeader(collection == "All" ? "All confessions" : collection);
        ContentFrame.Navigate(typeof(HomePage), collection);
    }

    private void SelectCollection(string collection)
    {
        foreach (object menuItem in NavView.MenuItems)
        {
            if (menuItem is NavigationViewItem item && item.Tag as string == collection)
            {
                NavView.SelectedItem = item;
                return;
            }
        }
    }

    private void ConfigureAppWindowTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

        if (ExtendsContentIntoTitleBar)
        {
            SetTitleBarHeightOption("Standard");
        }
    }

    private void SetTitleBarHeightOption(string optionName)
    {
        var heightOptionProperty = AppWindow.TitleBar.GetType().GetProperty("HeightOption");
        if (heightOptionProperty?.PropertyType is not { IsEnum: true } heightOptionType)
        {
            return;
        }

        object option = Enum.Parse(heightOptionType, optionName);
        heightOptionProperty.SetValue(AppWindow.TitleBar, option);
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        Bindings.Update();

        if (ContentFrame.Content is ReaderPage && e.Parameter is ConfessionDocument document)
        {
            UpdateHeader(document.DisplayTitle);
        }
        else
        {
            string collection = AppState.CurrentCollection == "All" ? "All" : AppState.CurrentCollection;
            UpdateHeader(collection == "All" ? "All confessions" : collection);
        }
    }
}
