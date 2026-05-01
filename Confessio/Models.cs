using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Confessio;

public sealed class ConfessionDocument
{
    public ConfessionDocument(string title, string displayTitle, string format, string displayFormat, string year, string authors, string location, string originStory, string sourceAttribution, string searchText, IReadOnlyList<ConfessionEntry> entries)
    {
        Title = title;
        DisplayTitle = displayTitle;
        Format = format;
        DisplayFormat = displayFormat;
        Year = year;
        Authors = authors;
        Location = location;
        OriginStory = originStory;
        SourceAttribution = sourceAttribution;
        SearchText = searchText;
        Entries = entries;
    }

    public string Title { get; set; }
    public string DisplayTitle { get; set; }
    public string Format { get; set; }
    public string DisplayFormat { get; set; }
    public string Year { get; set; }
    public string Authors { get; set; }
    public string Location { get; set; }
    public string OriginStory { get; set; }
    public string SourceAttribution { get; set; }
    public string SearchText { get; set; }
    public IReadOnlyList<ConfessionEntry> Entries { get; set; }
    public string LibraryDetail => string.Join(" | ", new[] { DisplayFormat, Year }.Where(value => !string.IsNullOrWhiteSpace(value)));
    public string Collection => ClassifyCollection(DisplayTitle, DisplayFormat);
    public string EntrySummary => Entries.Count == 1 ? "1 text" : $"{Entries.Count} {EntryLabel.ToLowerInvariant()}";
    public string EntryLabel => DisplayFormat.Equals("Catechism", StringComparison.OrdinalIgnoreCase) || DisplayFormat.Contains("Catechism", StringComparison.OrdinalIgnoreCase) ? "Questions" : "Sections";

    public string CollectionIcon => GetCollectionIcon(Collection);

    public static string GetCollectionIcon(string collection) => collection switch
    {
        "Creeds" => "\uE8A5",
        "Westminster Standards" => "\uE7BE",
        "Baptist Standards" => "\uE8F1",
        "Three Forms of Unity" => "\uE8EC",
        "Catechisms" => "\uE8D2",
        "Reformation Confessions" => "\uEA51",
        "Articles" => "\uE7C3",
        "Collection" => "\uE8B7",
        _ => "\uE8A9"
    };

    public override string ToString() => DisplayTitle;

    private static string ClassifyCollection(string title, string format)
    {
        if (format == "Creed") return "Creeds";
        if (title.Contains("Westminster", StringComparison.OrdinalIgnoreCase) || title.Contains("Assembly", StringComparison.OrdinalIgnoreCase)) return "Westminster Standards";
        if (title.Contains("Baptist", StringComparison.OrdinalIgnoreCase) || title.Contains("Keach", StringComparison.OrdinalIgnoreCase)) return "Baptist Standards";
        if (title.Contains("Belgic", StringComparison.OrdinalIgnoreCase) || title.Contains("Heidelberg", StringComparison.OrdinalIgnoreCase) || title.Contains("Dort", StringComparison.OrdinalIgnoreCase)) return "Three Forms of Unity";
        if (format == "Catechism") return "Catechisms";
        if (format == "Confession" || format == "Articles" || format == "Consensus" || format == "Theses") return "Reformation Confessions";
        return format;
    }
}

public sealed class ConfessionEntry
{
    public ConfessionEntry(string label, string title, string content, string groupTitle = "", IReadOnlyList<string>? proofReferences = null, string? navigationTitle = null, string? navigationDetail = null, string? readerLabel = null, string? readerTitle = null)
    {
        Label = label;
        Title = title;
        Content = content;
        GroupTitle = groupTitle;
        ProofReferences = proofReferences ?? [];
        NavigationTitle = string.IsNullOrWhiteSpace(navigationTitle) ? BuildNavigationTitle(label, title, content) : navigationTitle;
        NavigationDetail = string.IsNullOrWhiteSpace(navigationDetail) ? BuildNavigationDetail(label, title, content) : navigationDetail;
        ReaderLabel = readerLabel ?? label;
        ReaderTitle = readerTitle ?? NormalizeReaderTitle(label, title);
    }

    public string Label { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string GroupTitle { get; set; }
    public IReadOnlyList<string> ProofReferences { get; set; }
    public string NavigationTitle { get; set; }
    public string NavigationDetail { get; set; }
    public string ReaderLabel { get; set; }
    public string ReaderTitle { get; set; }
    public string SearchText => $"{Label} {Title} {Content} {string.Join(" ", ProofReferences)}";

    private static string BuildNavigationTitle(string label, string title, string content)
    {
        if (!string.IsNullOrWhiteSpace(label) && !IsRedundantTitle(label, title))
            return string.IsNullOrWhiteSpace(title) ? label : $"{label}: {title}";
        if (!string.IsNullOrWhiteSpace(label)) return label;
        return string.IsNullOrWhiteSpace(title) ? "Full text" : title;
    }

    private static string BuildNavigationDetail(string label, string title, string content) => Preview(content);
    private static string NormalizeReaderTitle(string label, string title) => IsRedundantTitle(label, title) ? string.Empty : title;

    private static bool IsRedundantTitle(string label, string title)
    {
        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(title)) return false;
        string normalizedLabel = NormalizeHeading(label);
        string normalizedTitle = NormalizeHeading(title);
        return normalizedLabel == normalizedTitle || normalizedTitle.StartsWith(normalizedLabel, StringComparison.OrdinalIgnoreCase) || normalizedLabel.StartsWith(normalizedTitle, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHeading(string value) => value.Replace("Question", "Q", StringComparison.OrdinalIgnoreCase).Replace("Article", "Article", StringComparison.OrdinalIgnoreCase).Replace(".", string.Empty).Replace(":", string.Empty).Trim().ToUpperInvariant();
    private static string Preview(string content) { string preview = content.ReplaceLineEndings(" ").Trim(); return preview.Length <= 96 ? preview : $"{preview[..93]}..."; }
}

internal static class ConfessionLibrary
{
    public static List<ConfessionDocument> Load()
    {
        string dataPath = ResolveDataPath();
        return Directory.EnumerateFiles(dataPath, "*.json").Select(LoadDocument).OrderBy(document => document.Title).ToList();
    }

    private static string ResolveDataPath()
    {
        string baseDataPath = Path.Combine(AppContext.BaseDirectory, "data");
        if (Directory.Exists(baseDataPath)) return baseDataPath;
        string projectDataPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data");
        if (Directory.Exists(projectDataPath)) return Path.GetFullPath(projectDataPath);
        throw new DirectoryNotFoundException($"Could not find the data folder.");
    }

    private static ConfessionDocument LoadDocument(string path)
    {
        using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = jsonDocument.RootElement;
        JsonElement metadata = root.GetProperty("Metadata");
        JsonElement data = root.GetProperty("Data");

        string title = ReadString(metadata, "Title", Path.GetFileNameWithoutExtension(path));
        string format = ReadString(metadata, "CreedFormat", "Collection");
        string displayTitle = NormalizeDocumentTitle(Path.GetFileName(path), title);
        string displayFormat = NormalizeFormat(format);
        string year = ReadString(metadata, "Year");
        string authors = ReadStringArray(metadata, "Authors");
        string location = ReadString(metadata, "Location");
        string originStory = ReadString(metadata, "OriginStory");
        string sourceAttribution = ReadString(metadata, "SourceAttribution");
        List<ConfessionEntry> entries = FlattenData(data);

        string searchText = string.Join(" ", new[] { title, displayTitle, format, displayFormat, year, authors, location, originStory, string.Join(" ", entries.Select(entry => $"{entry.Label} {entry.Title} {entry.Content}")) });
        return new ConfessionDocument(title, displayTitle, format, displayFormat, year, authors, location, originStory, sourceAttribution, searchText, entries);
    }

    private static List<ConfessionEntry> FlattenData(JsonElement data)
    {
        List<ConfessionEntry> entries = [];
        if (data.ValueKind == JsonValueKind.Object) { entries.Add(ObjectToEntry(data, "Text")); return entries; }
        if (data.ValueKind != JsonValueKind.Array) return entries;

        foreach (JsonElement item in data.EnumerateArray())
        {
            if (item.TryGetProperty("Sections", out JsonElement sections) && sections.ValueKind == JsonValueKind.Array)
            {
                string chapter = ReadString(item, "Chapter");
                string chapterTitle = ReadString(item, "Title");
                foreach (JsonElement section in sections.EnumerateArray())
                {
                    string sectionNumber = ReadString(section, "Section");
                    string shortLabel = BuildShortSectionLabel(chapter, sectionNumber);
                    string readerLabel = BuildLabel("Chapter", chapter, "Section", sectionNumber);
                    string content = ReadContent(section);
                    string navigationTitle = shortLabel;
                    string navigationDetail = string.Empty;
                    string readerTitle = entries.Any(entry => entry.Label.StartsWith($"{chapter}.", StringComparison.OrdinalIgnoreCase)) ? string.Empty : chapterTitle;
                    entries.Add(new ConfessionEntry(shortLabel, chapterTitle, content, groupTitle: string.IsNullOrWhiteSpace(chapterTitle) ? string.Empty : $"Chapter {chapter}: {chapterTitle}", proofReferences: ReadProofReferences(section), navigationTitle: navigationTitle, navigationDetail: navigationDetail, readerLabel: readerLabel, readerTitle: readerTitle));
                }
            }
            else entries.Add(ObjectToEntry(item));
        }
        return entries;
    }

    private static ConfessionEntry ObjectToEntry(JsonElement item, string fallbackLabel = "")
    {
        string label = ReadLabel(item, fallbackLabel);
        string title = ReadString(item, "Title");
        string content = ReadContent(item);

        if (item.TryGetProperty("SubQuestions", out JsonElement subQuestions) && subQuestions.ValueKind == JsonValueKind.Array)
        {
            IEnumerable<string> childTexts = subQuestions.EnumerateArray().Select(subQuestion => $"Q{ReadString(subQuestion, "Number")}. {ReadString(subQuestion, "Question")}\n{ReadString(subQuestion, "Answer")}");
            content = string.Join("\n\n", new[] { content }.Concat(childTexts).Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        string navigationTitle = BuildStandaloneNavigationTitle(label, title, content);
        string navigationDetail = Preview(content);
        string readerTitle = ShouldSuppressTitle(label, title) ? string.Empty : title;
        return new ConfessionEntry(label, title, content, proofReferences: ReadProofReferences(item), navigationTitle: navigationTitle, navigationDetail: navigationDetail, readerLabel: label == "Text" ? string.Empty : label, readerTitle: readerTitle);
    }

    private static string ReadLabel(JsonElement item, string fallbackLabel)
    {
        string number = ReadString(item, "Number");
        if (!string.IsNullOrWhiteSpace(number)) return $"Q. {number}";
        string article = ReadString(item, "Article");
        if (!string.IsNullOrWhiteSpace(article)) return $"Article {article}";
        string chapter = ReadString(item, "Chapter");
        if (!string.IsNullOrWhiteSpace(chapter)) return $"Chapter {chapter}";
        return fallbackLabel;
    }

    private static string NormalizeDocumentTitle(string fileName, string title) => fileName switch
    {
        "1695_baptist_catechism.json" => "The Baptist Catechism (1695)",
        "belgic_confession_of_faith.json" => "Belgic Confession of Faith",
        "london_baptist_1689.json" => "Second London Baptist Confession of Faith (1689)",
        "scots_confession.json" => "Scots Confession of 1560",
        "exposition_of_the_assemblies_catechism.json" => "An Exposition of the Assembly's Catechism",
        "shorter_catechism_explained.json" => "The Assembly's Shorter Catechism Explained",
        "christian_shema.json" => "Christian Shema",
        _ => title
    };

    private static string NormalizeFormat(string format) => format switch { "HenrysCatechism" => "Catechism", "Canon" => "Articles", _ => format };
    private static string BuildStandaloneNavigationTitle(string label, string title, string content) => label == "Text" ? (string.IsNullOrWhiteSpace(title) ? "Full text" : title) : ShouldSuppressTitle(label, title) ? label : string.IsNullOrWhiteSpace(title) ? label : $"{label}: {title}";
    private static bool ShouldSuppressTitle(string label, string title) { if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(title)) return false; string normalizedLabel = NormalizeHeading(label); string normalizedTitle = NormalizeHeading(title); return normalizedLabel == normalizedTitle || normalizedTitle.StartsWith(normalizedLabel, StringComparison.OrdinalIgnoreCase) || normalizedLabel.StartsWith(normalizedTitle, StringComparison.OrdinalIgnoreCase); }
    private static string NormalizeHeading(string value) { string normalized = value.Replace(".", string.Empty).Replace(":", string.Empty).Trim().ToUpperInvariant(); return normalized.StartsWith("ARTICLE ", StringComparison.OrdinalIgnoreCase) ? $"ARTICLE {RomanOrNumberToInt(normalized["ARTICLE ".Length..])}" : normalized; }
    private static string RomanOrNumberToInt(string value)
    {
        value = value.Trim();
        if (int.TryParse(value, out int number)) return number.ToString();
        Dictionary<char, int> values = new() { ['I'] = 1, ['V'] = 5, ['X'] = 10, ['L'] = 50, ['C'] = 100 };
        int total = 0, previous = 0;
        foreach (char c in value.Reverse()) { if (!values.TryGetValue(c, out int current)) return value; total += current < previous ? -current : current; previous = current; }
        return total > 0 ? total.ToString() : value;
    }

    private static string BuildShortSectionLabel(string chapter, string section) => string.IsNullOrWhiteSpace(chapter) ? (string.IsNullOrWhiteSpace(section) ? "Section" : section) : string.IsNullOrWhiteSpace(section) ? chapter : $"{chapter}.{section}";
    private static string Preview(string content) { string preview = content.ReplaceLineEndings(" ").Trim(); return preview.Length <= 96 ? preview : $"{preview[..93]}..."; }
    private static string ReadContent(JsonElement item)
    {
        if (item.TryGetProperty("Question", out JsonElement question)) return $"{question.GetString()}\n\n{ReadString(item, "Answer")}".Trim();
        foreach (string propertyName in new[] { "Content", "Answer", "Text" }) { string value = ReadString(item, propertyName); if (!string.IsNullOrWhiteSpace(value)) return value; }
        return string.Empty;
    }

    private static IReadOnlyList<string> ReadProofReferences(JsonElement item)
    {
        List<string> references = [];
        AddProofReferences(item, references);
        if (item.TryGetProperty("SubQuestions", out JsonElement subQuestions) && subQuestions.ValueKind == JsonValueKind.Array)
            foreach (JsonElement subQuestion in subQuestions.EnumerateArray()) AddProofReferences(subQuestion, references);
        return references.Distinct().ToList();
    }

    private static void AddProofReferences(JsonElement item, List<string> references)
    {
        if (!item.TryGetProperty("Proofs", out JsonElement proofs) || proofs.ValueKind != JsonValueKind.Array) return;
        foreach (JsonElement proof in proofs.EnumerateArray())
        {
            if (!proof.TryGetProperty("References", out JsonElement proofReferences) || proofReferences.ValueKind != JsonValueKind.Array) continue;
            foreach (JsonElement reference in proofReferences.EnumerateArray())
            {
                string? value = reference.GetString();
                if (!string.IsNullOrWhiteSpace(value)) references.Add(value);
            }
        }
    }

    private static string BuildLabel(string firstName, string firstValue, string secondName, string secondValue) => string.Join(", ", new[] { string.IsNullOrWhiteSpace(firstValue) ? string.Empty : $"{firstName} {firstValue}", string.IsNullOrWhiteSpace(secondValue) ? string.Empty : $"{secondName} {secondValue}" }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string ReadString(JsonElement element, string propertyName, string fallback = "")
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) return fallback;
        return property.ValueKind switch { JsonValueKind.String => property.GetString() ?? fallback, JsonValueKind.Number => property.GetRawText(), _ => fallback };
    }

    private static string ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Array) return string.Empty;
        return string.Join(", ", property.EnumerateArray().Select(item => item.GetString()).Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
