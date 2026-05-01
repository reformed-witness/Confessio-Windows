using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Confessio;

internal static class AppState
{
    public static List<ConfessionDocument> AllDocuments { get; } = [];
    public static ObservableCollection<ConfessionDocument> FilteredDocuments { get; } = [];

    public static HashSet<string> Collections { get; } = [];

    public static string CurrentCollection { get; set; } = "All";

    // Reader settings
    public static double ReaderFontSize { get; set; } = 17;
    public static double LineSpacing { get; set; } = 1.5;
    public static double ReaderWidth { get; set; } = 860;
    public static string ReaderFontFamily { get; set; } = "Segoe UI Variable Text";
    public static string CurrentTheme { get; set; } = "System";

    public static void ApplyFilters(string? query = null)
    {
        string q = query?.Trim() ?? string.Empty;
        FilteredDocuments.Clear();

        foreach (ConfessionDocument document in AllDocuments.Where(d => MatchesDocument(d, q)))
        {
            FilteredDocuments.Add(document);
        }
    }

    private static bool MatchesDocument(ConfessionDocument document, string query)
    {
        if (CurrentCollection != "All" && document.Collection != CurrentCollection)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return document.SearchText.Contains(query, System.StringComparison.CurrentCultureIgnoreCase);
    }

    public static void LoadCollections()
    {
        Collections.Clear();
        foreach (string collection in AllDocuments.Select(d => d.Collection).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().Order())
        {
            Collections.Add(collection);
        }
    }
}
