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
        // Confirmed via a live boss-list dump (NPCID.Sets.ShouldBeCountedAsBossForBestiary) that
        // these four were the only real bosses missing from this curated list — everything else
        // already matched.
        ("Vortex Pillar", "Vortex_Pillar", new[] { "Vortex Pillar" }),
        ("Nebula Pillar", "Nebula_Pillar", new[] { "Nebula Pillar" }),
        ("Solar Pillar", "Solar_Pillar", new[] { "Solar Pillar" }),
        ("Stardust Pillar", "Stardust_Pillar", new[] { "Stardust Pillar" }),
        // Event/invasion bosses: the wiki's "Boss NPCs" category lists these (confirmed via its
        // API), but neither npc.boss nor NPCID.Sets.ShouldBeCountedAsBossForBestiary flag them in
        // the actual game data (verified live) — see NpcCatalogue.ExtraWikiBosses. Both tiers of
        // Dark Mage/Ogre share one display name each, so one MatchNames entry covers both NPCIDs.
        // Brings the total to 33, matching the wiki's own count.
        ("Betsy", "Betsy", new[] { "Betsy" }),
        ("Ogre", "Ogre", new[] { "Ogre" }),
        ("Dark Mage", "Dark_Mage", new[] { "Dark Mage" }),
        ("Mourning Wood", "Mourning_Wood", new[] { "Mourning Wood" }),
        ("Pumpking", "Pumpking", new[] { "Pumpking" }),
        ("Everscream", "Everscream", new[] { "Everscream" }),
        ("Santa-NK1", "Santa-NK1", new[] { "Santa-NK1" }),
        ("Ice Queen", "Ice_Queen", new[] { "Ice Queen" }),
        ("Flying Dutchman", "Flying_Dutchman", new[] { "Flying Dutchman" }),
    };

    // Cross-checked against the wiki's own "Random events"/"Summoned events" categories
    // (confirmed via its API) rather than hand-guessed. Deliberately excludes the purely
    // cosmetic/ambient weather entries also in those categories (Rain, Windy Day, Thunderstorm,
    // Boulder/Coin/Meteor rain, Starfall) — those aren't the "how do I fight/survive this"
    // content this feature is for. Old One's Army isn't in either wiki category (it's tagged
    // differently there) but is unambiguously a real event, so it's added directly.
    private static readonly (string Name, string WikiPage)[] Events =
    {
        ("Blood Moon", "Blood_Moon"),
        ("Solar Eclipse", "Solar_Eclipse"),
        ("Goblin Army", "Goblin_Army"),
        ("Pirate Invasion", "Pirate_Invasion"),
        ("Frost Legion", "Frost_Legion"),
        ("Pumpkin Moon", "Pumpkin_Moon"),
        ("Frost Moon", "Frost_Moon"),
        ("Martian Madness", "Martian_Madness"),
        ("Lunar Events", "Lunar_Events"),
        ("Old One's Army", "Old_One's_Army"),
        ("Slime Rain", "Slime_Rain"),
        ("Sandstorm", "Sandstorm"),
        ("Party", "Party"),
    };

    // The full live town-NPC list (from NPC.townNPC), confirmed via a one-time in-game dump —
    // not guessed. A few of these (the town pets, the slime variants, Old Man, Traveling
    // Merchant) likely have no "Living preferences" table on their wiki page at all; that's
    // handled by ScrapeNpcPreferences simply omitting them from the output rather than by
    // curating this list down first.
    private static readonly string[] TownNpcs =
    {
        "Merchant", "Nurse", "Arms Dealer", "Dryad", "Guide", "Old Man", "Demolitionist", "Clothier",
        "Goblin Tinkerer", "Wizard", "Mechanic", "Santa Claus", "Truffle", "Steampunker", "Dye Trader",
        "Party Girl", "Cyborg", "Painter", "Witch Doctor", "Pirate", "Stylist", "Traveling Merchant",
        "Angler", "Tax Collector", "Tavernkeep", "Golfer", "Zoologist", "Cat", "Dog", "Bunny", "Princess",
        "Nerdy Slime", "Cool Slime", "Elder Slime", "Clumsy Slime", "Diva Slime", "Surly Slime",
        "Mystic Slime", "Squire Slime",
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
        await ScrapeNpcPreferences(outDir);
        await ScrapeEvents(outDir);

        Console.WriteLine("Done.");
    }

    /// <summary>
    /// Events have no live data source at all — confirmed via reflection: only Old One's Army
    /// has a dedicated NPCID.Sets array (BelongsToInvasionOldOnesArmy); every other event's enemy
    /// roster is imperative spawn logic, not queryable data (same pattern as shop contents and
    /// neighbor preferences). Wiki page structure varies more per event than per boss (e.g. "How
    /// it starts" is titled "Summoning" on some pages, "Criteria" on others), so multiple heading
    /// candidates are tried per field, same approach as the boss scraper's Summoning section.
    /// </summary>
    private static async Task ScrapeEvents(string outDir)
    {
        var results = new List<EventInfo>();

        foreach (var (name, wikiPage) in Events)
        {
            Console.WriteLine($"Fetching {name} ({wikiPage})...");
            try
            {
                string html = await FetchPageHtml(wikiPage);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var info = new EventInfo { Name = name, WikiPage = wikiPage };
                info.Summoning = ExtractSection(doc, "Summoning", "Summoning_and_spawning", "Criteria", "Trigger");
                info.Completing = ExtractSection(doc, "Completing", "Aftermath");
                info.Notes = ExtractSection(doc, "Notes");
                info.Tips = ExtractSection(doc, "Tips");

                results.Add(info);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAILED: {ex.Message}");
            }

            await Task.Delay(500);
        }

        string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(outDir, "events.json"), json);
        Console.WriteLine($"Wrote {results.Count} event entries.");
    }

    /// <summary>
    /// Town NPCs' Loves/Likes/Dislikes/Hates x Biome/Neighbor table. Not reflectable from the
    /// live game: biome preferences are (mostly) live-data-driven, but neighbor preferences turn
    /// out to be a single ~900-instruction hardcoded method (confirmed via IL disassembly of
    /// AllPersonalitiesModifier.ModifyShopPrice_Relationships) with no queryable data behind it,
    /// so the whole table is sourced from the wiki instead for consistency between both columns.
    /// </summary>
    private static async Task ScrapeNpcPreferences(string outDir)
    {
        var results = new List<NpcPreferencesInfo>();

        foreach (var name in TownNpcs)
        {
            string wikiPage = name.Replace(' ', '_');
            Console.WriteLine($"Fetching preferences for {name} ({wikiPage})...");
            try
            {
                string html = await FetchPageHtml(wikiPage);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var info = new NpcPreferencesInfo { Name = name, WikiPage = wikiPage };
                var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class,'living-preferences')]");
                if (table != null)
                {
                    (info.LoveBiomes, info.LoveNeighbors) = ParsePreferenceRow(table, "love");
                    (info.LikeBiomes, info.LikeNeighbors) = ParsePreferenceRow(table, "like");
                    (info.DislikeBiomes, info.DislikeNeighbors) = ParsePreferenceRow(table, "dislike");
                    (info.HateBiomes, info.HateNeighbors) = ParsePreferenceRow(table, "hate");
                }

                // "How do I get this NPC" (available house + move-in condition, e.g. Tavernkeep's
                // rescue-as-Unconscious-Man-after-EoW/BoC) lives only in the unstructured intro
                // text before any heading — there's no dedicated section for it at all.
                info.HowToGet = ExtractLeadParagraph(doc);

                int total = info.LoveBiomes.Length + info.LoveNeighbors.Length + info.LikeBiomes.Length
                    + info.LikeNeighbors.Length + info.DislikeBiomes.Length + info.DislikeNeighbors.Length
                    + info.HateBiomes.Length + info.HateNeighbors.Length
                    + (info.HowToGet?.Length ?? 0);
                if (total > 0) results.Add(info);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAILED: {ex.Message}");
            }

            await Task.Delay(500);
        }

        string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(outDir, "npc-preferences.json"), json);
        Console.WriteLine($"Wrote {results.Count} NPC preference entries.");
    }

    private static (string[] Biomes, string[] Neighbors) ParsePreferenceRow(HtmlNode table, string rowClass)
    {
        var row = table.SelectSingleNode($".//tr[contains(@class,'{rowClass}')]");
        var cells = row?.SelectNodes("./td");
        if (cells == null || cells.Count < 2) return (Array.Empty<string>(), Array.Empty<string>());
        return (ExtractPreferenceNames(cells[0]), ExtractPreferenceNames(cells[1]));
    }

    // Each entry is an <li> containing two <a title="X">-wrapped links to the same page — one
    // wrapping the icon <img>, one wrapping the visible text (nested a variable number of
    // <span>s deep depending on the entry type). Filtering to the <a> that has no <img>
    // descendant reliably picks just the text link regardless of nesting depth.
    private static string[] ExtractPreferenceNames(HtmlNode cell)
    {
        var links = cell.SelectNodes(".//li//a[not(.//img)]");
        if (links == null) return Array.Empty<string>();
        return links.Select(a => HtmlEntity.DeEntitize(a.InnerText).Trim()).Where(s => s.Length > 0).ToArray();
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
                info.Aftermath = ExtractSection(doc, "Aftermath");
                info.Tips = ExtractSection(doc, "Tips");

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
    /// Recursively collects list items, indenting nested sub-lists. Each &lt;li&gt;'s own text is
    /// taken with any nested &lt;ul&gt; removed first — a plain .InnerText on a &lt;li&gt; that
    /// contains a nested list recursively includes the nested items' text too, so selecting both
    /// the parent and the nested &lt;li&gt; via a single ".//li" query double-counts the nested
    /// content (confirmed: several bosses' Expert Mode text had entire sentences duplicated).
    /// </summary>
    private static void CollectListItems(HtmlNode ul, string indent, List<string> parts)
    {
        foreach (var li in ul.SelectNodes("./li") ?? Enumerable.Empty<HtmlNode>())
        {
            var clone = li.CloneNode(true);
            foreach (var nestedUl in (clone.SelectNodes(".//ul") ?? Enumerable.Empty<HtmlNode>()).ToList())
                nestedUl.Remove();

            string text = HtmlEntity.DeEntitize(clone.InnerText).Trim();
            if (text.Length > 0) parts.Add(indent + "- " + text);

            var childUl = li.SelectSingleNode("./ul");
            if (childUl != null) CollectListItems(childUl, indent + "  ", parts);
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
                CollectListItems(node, "", parts);
            }

            node = node.NextSibling;
        }

        if (parts.Count == 0) return null;
        return JoinAndCleanFootnotes(parts);
    }

    /// <summary>
    /// The unstructured intro text before the first heading — where "how do I get this NPC"
    /// info actually lives for town NPCs (e.g. Tavernkeep's house/rescue requirements), not
    /// under any named section at all. Stops at the first heading or the table-of-contents div,
    /// skipping the infobox/version-notice/quote blocks that precede it.
    /// </summary>
    private static string? ExtractLeadParagraph(HtmlDocument doc)
    {
        var root = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'mw-parser-output')]");
        if (root == null) return null;

        var parts = new List<string>();
        foreach (var node in root.ChildNodes)
        {
            if (node.Name == "h2") break;
            if (node.Name == "div" && node.GetAttributeValue("id", "") == "toc") break;

            if (node.Name == "p")
            {
                string text = HtmlEntity.DeEntitize(node.InnerText).Trim();
                if (text.Length > 0) parts.Add(text);
            }
            else if (node.Name == "ul")
            {
                CollectListItems(node, "", parts);
            }
            // Anything else (message-box version notice, infobox, quote blockquote) is skipped.
        }

        if (parts.Count == 0) return null;
        return JoinAndCleanFootnotes(parts);
    }

    // Strip "[edit]" section-edit links and "[3]"-style footnote reference markers that bleed
    // into InnerText from nested <span>/<sup> markup — noise, not content. Joined with newlines
    // (not spaces) so bullet points and paragraphs each land on their own line instead of
    // running together as one continuous paragraph.
    private static string JoinAndCleanFootnotes(List<string> parts)
    {
        string joined = string.Join("\n", parts);
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
    public string? Aftermath { get; set; }
    public string? Tips { get; set; }
}

public class NpcPreferencesInfo
{
    public string Name { get; set; } = "";
    public string WikiPage { get; set; } = "";
    public string? HowToGet { get; set; }
    public string[] LoveBiomes { get; set; } = Array.Empty<string>();
    public string[] LoveNeighbors { get; set; } = Array.Empty<string>();
    public string[] LikeBiomes { get; set; } = Array.Empty<string>();
    public string[] LikeNeighbors { get; set; } = Array.Empty<string>();
    public string[] DislikeBiomes { get; set; } = Array.Empty<string>();
    public string[] DislikeNeighbors { get; set; } = Array.Empty<string>();
    public string[] HateBiomes { get; set; } = Array.Empty<string>();
    public string[] HateNeighbors { get; set; } = Array.Empty<string>();
}

public class EventInfo
{
    public string Name { get; set; } = "";
    public string WikiPage { get; set; } = "";
    public string? Summoning { get; set; }
    public string? Completing { get; set; }
    public string? Notes { get; set; }
    public string? Tips { get; set; }
}
