using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;

namespace TerrariaEncyclopedia.Content
{
    public class BossWikiInfo
    {
        public string Name { get; set; }
        public string WikiPage { get; set; }
        public string[] MatchNames { get; set; }
        public string[] Types { get; set; }
        public string[] Environments { get; set; }
        public string AiType { get; set; }
        public string KbResist { get; set; }
        public string[] ImmuneTo { get; set; }
        public string Summoning { get; set; }
        public string ExpertMode { get; set; }
        public string Aftermath { get; set; }
        public string Tips { get; set; }
    }

    /// <summary>
    /// Loads the wiki-scraper's one-time data pull (tools/WikiScraper — run manually, not at
    /// gameplay time). Supplements the live game data with things Terraria's own data has no
    /// field for at all: boss Type/Environment/AI Type/immunities, summoning/expert-mode prose,
    /// and hardmode-exclusivity (confirmed via reflection: no such flag exists anywhere in
    /// Terraria's NPC/Item data or its own Bestiary filters — see project-plan.md Phase 4).
    /// Best-effort: if the JSON files are missing, everything here just returns nothing found,
    /// same fail-safe spirit as GameVersionGuard.
    /// </summary>
    public static class WikiEnrichment
    {
        private static bool _loaded;
        private static List<BossWikiInfo> _bosses = new List<BossWikiInfo>();
        private static HashSet<string> _hardmodeItems = new HashSet<string>();
        private static HashSet<string> _hardmodeNpcs = new HashSet<string>();

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            string dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "WikiData");
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

            _bosses = ReadJson<List<BossWikiInfo>>(serializer, Path.Combine(dir, "bosses.json")) ?? new List<BossWikiInfo>();
            _hardmodeItems = new HashSet<string>(ReadJson<List<string>>(serializer, Path.Combine(dir, "hardmode-items.json")) ?? new List<string>());
            _hardmodeNpcs = new HashSet<string>(ReadJson<List<string>>(serializer, Path.Combine(dir, "hardmode-npcs.json")) ?? new List<string>());
        }

        private static T ReadJson<T>(JavaScriptSerializer serializer, string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                return serializer.Deserialize<T>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public static BossWikiInfo GetBossInfo(string npcName)
        {
            EnsureLoaded();
            return _bosses.FirstOrDefault(b => b.MatchNames != null && b.MatchNames.Contains(npcName));
        }

        // Exact-match against wiki page titles — some misses are expected (e.g. armor sets are
        // one wiki page covering several of our individual item entries) but no false positives.
        public static bool IsHardmodeItem(string itemName)
        {
            EnsureLoaded();
            return _hardmodeItems.Contains(itemName);
        }

        public static bool IsHardmodeNpc(string npcName)
        {
            EnsureLoaded();
            return _hardmodeNpcs.Contains(npcName);
        }
    }
}
