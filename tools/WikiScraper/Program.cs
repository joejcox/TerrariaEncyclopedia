using System.Text.Json;
using HtmlAgilityPack;

namespace WikiScraper;

/// <summary>
/// One-time (well, run-it-whenever-Terraria-updates) dev tool that pulls supplementary boss
/// info and hardmode-exclusivity lists from terraria.wiki.gg's public MediaWiki API, and writes
/// them as JSON the mod loads at runtime. Not run during gameplay, not shipped as part of the
/// mod DLL. Re-run this after a Terraria/wiki update to refresh the data.
/// </summary>
public static class Program
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://terraria.wiki.gg/api.php"),
        Timeout = TimeSpan.FromSeconds(30),
    };

    // Boss name -> wiki page title. Grouped multi-segment bosses (Twins, Moon Lord, Eater of
    // Worlds' Head/Body/Tail) list every internal NPC display name they should match against.
    private static readonly (string Name, string WikiPage, string[] MatchNames)[] Bosses =
    {
        ("King Slime", "King Slime", new[] { "King Slime" }),
        ("Eye of Cthulhu", "Eye_of_Cthulhu", new[] { "Eye of Cthulhu" }),
        ("Eater of Worlds", "Eater_of_Worlds", new[] { "Eater of Worlds" }),
        ("Brain of Cthulhu", "Brain_of_Cthulhu", new[] { "Brain of Cthulhu" }),
        ("Queen Bee", "Queen_Bee", new[] { "Queen Bee" }),
        ("Skeletron", "Skeletron", new[] { "Skeletron" }),
        ("Deerclops", "Deerclops", new[] { "Deerclops" }),
        ("Wall of Flesh", "Wall_of_Flesh", new[] { "Wall of Flesh" }),
        ("Queen Slime", "Queen_Slime", new[] { "Queen Slime" }),
        ("The Twins", "The_Twins", new[] { "Retinazer", "Spazmatism" }),
        ("The Destroyer", "The_Destroyer", new[] { "The Destroyer" }),
        ("Skeletron Prime", "Skeletron_Prime", new[] { "Skeletron Prime" }),
        ("Plantera", "Plantera", new[] { "Plantera" }),
        ("Golem", "Golem", new[] { "Golem" }),
        ("Duke Fishron", "Duke_Fishron", new[] { "Duke Fishron" }),
        ("The Torch God", "The_Torch_God", new[] { "The Torch God" }),
        ("Lunatic Cultist", "Lunatic_Cultist", new[] { "Lunatic Cultist" }),
        ("Moon Lord", "Moon_Lord", new[] { "Moon Lord", "Moon Lord's Hand", "Moon Lord's Core" }),
        ("Martian Saucer", "Martian_Saucer", new[] { "Martian Saucer" }),
        ("Empress of Light", "Empress_of_Light", new[] { "Empress of Light" }),
    };

    public static async Task Main()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "TerrariaEncyclopediaWikiScraper/1.0 (dev tool, one-time data pull, github.com/Inidar1/terraria-modder ecosystem)");

        string outDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "TerrariaEncyclopedia", "WikiData");
        Directory.CreateDirectory(outDir);
        outDir = Path.GetFullPath(outDir);
        Console.WriteLine($"Output directory: {outDir}");

        await ScrapeBosses(outDir);
        await ScrapeHardmodeCategory("Hardmode-only items", Path.Combine(outDir, "hardmode-items.json"));
        await ScrapeHardmodeCategory("Hardmode-only NPCs", Path.Combine(outDir, "hardmode-npcs.json"));

        Console.WriteLine("Done.");
    }

    private static async Task ScrapeBosses(string outDir)
    {
        var results = new List<BossInfo>();

        foreach (var (name, wikiPage, matchNames) in Bosses)
        {
            Console.WriteLine($"Fetching {name} ({wikiPage})...");
            try
            {
                string html = await FetchPageHtml(wikiPage);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var info = new BossInfo { Name = name, WikiPage = wikiPage, MatchNames = matchNames };
                ParseStatisticsTable(doc, info);
                info.Summoning = ExtractSection(doc, "Summoning_and_spawning", "Summoning", "Spawning");
                info.ExpertMode = ExtractSection(doc, "Expert_Mode");

                results.Add(info);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAILED: {ex.Message}");
            }

            await Task.Delay(500); // politeness delay between requests
        }

        string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(outDir, "bosses.json"), json);
        Console.WriteLine($"Wrote {results.Count} boss entries.");
    }

    private static void ParseStatisticsTable(HtmlDocument doc, BossInfo info)
    {
        var rows = doc.DocumentNode.SelectNodes(
            "//div[contains(@class,'section statistics')]//table[contains(@class,'stat')]/tbody/tr");
        if (rows == null) return;

        foreach (var row in rows)
        {
            var th = row.SelectSingleNode("./th");
            var td = row.SelectSingleNode("./td");
            if (th == null || td == null) continue;

            string label = HtmlEntity.DeEntitize(th.InnerText).Trim();

            switch (label)
            {
                case "Type":
                    info.Types = td.SelectNodes(".//div[@class='tags']//a")
                        ?.Select(a => HtmlEntity.DeEntitize(a.InnerText).Trim()).ToArray() ?? Array.Empty<string>();
                    break;
                case "Environment":
                    info.Environments = td.SelectNodes(".//div[@class='tags']//a")
                        ?.Select(a => HtmlEntity.DeEntitize(a.InnerText).Trim()).ToArray() ?? Array.Empty<string>();
                    break;
                case "AI Type":
                    info.AiType = HtmlEntity.DeEntitize(td.InnerText).Trim();
                    break;
                case "KB Resist":
                    info.KbResist = HtmlEntity.DeEntitize(td.InnerText).Trim();
                    break;
                case "Immune to":
                    info.ImmuneTo = td.SelectNodes(".//a[@title]")
                        ?.Select(a => a.GetAttributeValue("title", "")).Where(t => t.Length > 0).ToArray()
                        ?? Array.Empty<string>();
                    break;
            }
        }
    }

    /// <summary>
    /// Plain-text content between a named heading (by its mw-headline id) and the next heading
    /// of equal-or-higher level. h2 sections often contain their own h3 sub-sections (e.g.
    /// "Summoning and spawning" > "Manual"/"Random") — those are included as sub-labels, not
    /// treated as the end of the section. Expert Mode details are frequently a bullet list
    /// rather than paragraphs, so both are collected.
    /// </summary>
    private static string? ExtractSection(HtmlDocument doc, params string[] headlineIdCandidates)
    {
        HtmlNode? heading = null;
        foreach (var id in headlineIdCandidates)
        {
            var headline = doc.DocumentNode.SelectSingleNode($"//span[@class='mw-headline' and @id='{id}']");
            if (headline != null) { heading = headline.ParentNode; break; }
        }
        if (heading == null) return null;

        string startTag = heading.Name;
        var parts = new List<string>();
        var node = heading.NextSibling;

        while (node != null)
        {
            if (node.Name == "h2") break;
            if (node.Name == "h3" && startTag == "h3") break;

            if (node.Name == "h3" && startTag == "h2")
            {
                string subLabel = HtmlEntity.DeEntitize(node.InnerText).Trim();
                if (subLabel.Length > 0) parts.Add(subLabel + ":");
            }
            else if (node.Name == "p")
            {
                string text = HtmlEntity.DeEntitize(node.InnerText).Trim();
                if (text.Length > 0) parts.Add(text);
            }
            else if (node.Name == "ul")
            {
                foreach (var li in node.SelectNodes(".//li") ?? Enumerable.Empty<HtmlNode>())
                {
                    string text = HtmlEntity.DeEntitize(li.InnerText).Trim();
                    if (text.Length > 0) parts.Add("- " + text);
                }
            }

            node = node.NextSibling;
        }

        if (parts.Count == 0) return null;

        // Strip "[edit]" section-edit links and "[3]"-style footnote reference markers that
        // bleed into InnerText from nested <span>/<sup> markup — noise, not content.
        string joined = string.Join(" ", parts);
        joined = System.Text.RegularExpressions.Regex.Replace(joined, @"\[edit\]", "");
        joined = System.Text.RegularExpressions.Regex.Replace(joined, @"\[\d+\]", "");
        return joined.Trim();
    }

    private static async Task<string> FetchPageHtml(string pageTitle)
    {
        string url = $"?action=parse&page={Uri.EscapeDataString(pageTitle)}&prop=text&format=json";
        string response = await Http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);
        return doc.RootElement.GetProperty("parse").GetProperty("text").GetProperty("*").GetString() ?? "";
    }

    private static async Task ScrapeHardmodeCategory(string categoryName, string outPath)
    {
        Console.WriteLine($"Fetching category members: {categoryName}...");
        var titles = new List<string>();
        string? cmContinue = null;

        do
        {
            string url = $"?action=query&list=categorymembers&cmtitle={Uri.EscapeDataString("Category:" + categoryName)}"
                + $"&cmlimit=500&format=json"
                + (cmContinue != null ? $"&cmcontinue={Uri.EscapeDataString(cmContinue)}" : "");

            string response = await Http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            foreach (var member in doc.RootElement.GetProperty("query").GetProperty("categorymembers").EnumerateArray())
                titles.Add(member.GetProperty("title").GetString() ?? "");

            cmContinue = doc.RootElement.TryGetProperty("continue", out var cont) && cont.TryGetProperty("cmcontinue", out var cc)
                ? cc.GetString()
                : null;

            await Task.Delay(300);
        } while (cmContinue != null);

        File.WriteAllText(outPath, JsonSerializer.Serialize(titles, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"  {titles.Count} pages -> {outPath}");
    }
}

public class BossInfo
{
    public string Name { get; set; } = "";
    public string WikiPage { get; set; } = "";
    public string[] MatchNames { get; set; } = Array.Empty<string>();
    public string[] Types { get; set; } = Array.Empty<string>();
    public string[] Environments { get; set; } = Array.Empty<string>();
    public string? AiType { get; set; }
    public string? KbResist { get; set; }
    public string[] ImmuneTo { get; set; } = Array.Empty<string>();
    public string? Summoning { get; set; }
    public string? ExpertMode { get; set; }
}
