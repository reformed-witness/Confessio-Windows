using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Confessio.Views;

public sealed partial class ReaderPage : Page
{
    private ConfessionDocument? _document;

    public ReaderPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ConfessionDocument document) LoadDocument(document);
    }

    private void LoadDocument(ConfessionDocument? document)
    {
        _document = document;
        OutlineStack.Children.Clear();
        EntryStack.Children.Clear();

        if (document == null)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            return;
        }

        EmptyStatePanel.Visibility = Visibility.Collapsed;

        BuildOutline(document);

        DocumentTitleText.Text = document.DisplayTitle;
        MetadataYear.Text = document.Year;
        MetadataFormat.Text = document.DisplayFormat;
        MetadataAuthors.Text = document.Authors;
        MetadataLocation.Text = document.Location;

        string? lastGroupTitle = null;
        foreach (ConfessionEntry entry in document.Entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.GroupTitle) && entry.GroupTitle != lastGroupTitle)
            {
                lastGroupTitle = entry.GroupTitle;
                EntryStack.Children.Add(CreateChapterHeading(entry.GroupTitle));
            }
            EntryStack.Children.Add(CreateEntryElement(entry));
        }
    }

    private TextBlock CreateChapterHeading(string title)
    {
        return new TextBlock
        {
            Text = title,
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Georgia"),
            Margin = new Thickness(0, 16, 0, 8),
            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };
    }

    public void ReRender() => LoadDocument(_document);
    public void RefreshOutlineFromSearch(string query) => LoadDocument(_document);

    private void BuildOutline(ConfessionDocument document)
    {
        bool isCatechism = document.EntryLabel == "Questions";

        if (isCatechism)
        {
            foreach (ConfessionEntry entry in document.Entries)
            {
                string title = entry.NavigationTitle;
                OutlineStack.Children.Add(CreateOutlineButton(title, entry, isHeader: false, isNested: false));
            }
            return;
        }

        var chapters = document.Entries
            .Where(e => !string.IsNullOrWhiteSpace(e.GroupTitle))
            .Select(e => e.GroupTitle!)
            .Distinct()
            .ToList();

        if (chapters.Count > 0)
        {
            foreach (string chapter in chapters)
            {
                ConfessionEntry? firstEntryInChapter = document.Entries.FirstOrDefault(e => e.GroupTitle == chapter);
                if (firstEntryInChapter != null)
                {
                    OutlineStack.Children.Add(CreateOutlineButton(chapter, firstEntryInChapter, isHeader: true, isNested: false));
                }
            }
        }
        else
        {
            foreach (ConfessionEntry entry in document.Entries)
            {
                string title = entry.NavigationTitle;
                OutlineStack.Children.Add(CreateOutlineButton(title, entry, isHeader: false, isNested: false));
            }
        }
    }

    private Button CreateOutlineButton(string title, ConfessionEntry entry, bool isHeader, bool isNested)
    {
        Button button = new()
        {
            Tag = entry,
            Padding = new Thickness(isNested ? 32 : 12, isHeader ? 12 : 8, 12, isHeader ? 12 : 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Content = new TextBlock
            {
                Text = title,
                FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap
            }
        };
        button.Click += OutlineButton_Click;
        return button;
    }

    private void OutlineButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ConfessionEntry entry })
        {
            ScrollToEntry(entry);
        }
    }

    public void ScrollToEntry(ConfessionEntry entry)
    {
        double totalOffset = 0;
        
        // Add the header offset (title + metadata)
        totalOffset += 200; // Approximate header height
        
        foreach (var child in EntryStack.Children)
        {
            if (child is FrameworkElement fe && fe.Tag == entry)
            {
                ReaderScrollViewer.ChangeView(0, totalOffset, null, true);
                return;
            }
            if (child is FrameworkElement childFe)
            {
                totalOffset += childFe.ActualHeight + 16; // spacing
            }
        }
    }

    private FrameworkElement CreateEntryElement(ConfessionEntry entry)
    {
        StackPanel panel = new()
        {
            Spacing = 4,
            Margin = new Thickness(0, 0, 0, 20),
            Tag = entry
        };

        if (entry.ProofReferences.Count > 0)
        {
            AddContentWithFootnotes(panel, entry.Content, entry.ProofReferences);
        }
        else
        {
            AddContentParagraphs(panel, entry.Content);
        }

        return panel;
    }

    private void AddContentWithFootnotes(StackPanel panel, string content, IReadOnlyList<string> proofs)
    {
        // Just display the content - proofs are available via the expander toggle
        foreach (string paragraph in SplitParagraphs(content))
        {
            TextBlock textBlock = new()
            {
                FontFamily = new FontFamily(AppState.ReaderFontFamily),
                FontSize = AppState.ReaderFontSize,
                LineHeight = AppState.ReaderFontSize * AppState.LineSpacing,
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = paragraph
            };
            panel.Children.Add(textBlock);
        }
    }

    private static TextBlock CreateTextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(AppState.ReaderFontFamily),
            FontSize = AppState.ReaderFontSize,
            LineHeight = AppState.ReaderFontSize * AppState.LineSpacing,
            TextWrapping = TextWrapping.WrapWholeWords
        };
    }

    private static void AddContentParagraphs(StackPanel panel, string content)
    {
        foreach (string paragraph in SplitParagraphs(content))
        {
            panel.Children.Add(CreateTextBlock(paragraph));
        }
    }

    private static IEnumerable<string> SplitParagraphs(string content)
    {
        return content
            .ReplaceLineEndings("\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph));
    }

    private static Expander CreateProofsExpander(ConfessionEntry entry)
    {
        StackPanel referencesPanel = new()
        {
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        foreach (string reference in entry.ProofReferences)
        {
            referencesPanel.Children.Add(new TextBlock
            {
                Text = reference,
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                TextWrapping = TextWrapping.WrapWholeWords
            });
        }

        return new Expander
        {
            Header = entry.ProofReferences.Count == 1 ? "1 scripture proof" : $"{entry.ProofReferences.Count} scripture proofs",
            Content = referencesPanel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
    }

    private static IReadOnlyList<string> BuildMetadata(ConfessionDocument document)
    {
        return new[] { document.DisplayFormat, document.Year, document.Authors, document.Location, document.EntrySummary }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < 640;
        OutlineColumn.Width = new GridLength(narrow ? 0 : 300);
        OutlinePane.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
        ReaderScrollViewer.Padding = narrow ? new Thickness(24, 16, 24, 24) : new Thickness(48, 24, 48, 48);
    }
}
