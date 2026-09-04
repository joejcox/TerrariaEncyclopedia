using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;

namespace TerrariaEncyclopedia.Content
{
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
        private static Dictionary<string, NpcPreferencesWikiInfo> _npcPreferences = new Dictionary<string, NpcPreferencesWikiInfo>();
        private static List<EventWikiInfo> _events = new List<EventWikiInfo>();

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            string dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "WikiData");
            _bosses = ReadJson<List<BossWikiInfo>>(Path.Combine(dir, "bosses.json")) ?? new List<BossWikiInfo>();
            _hardmodeItems = new HashSet<string>(ReadJson<List<string>>(Path.Combine(dir, "hardmode-items.json")) ?? new List<string>());
            _hardmodeNpcs = new HashSet<string>(ReadJson<List<string>>(Path.Combine(dir, "hardmode-npcs.json")) ?? new List<string>());
            var preferences = ReadJson<List<NpcPreferencesWikiInfo>>(Path.Combine(dir, "npc-preferences.json"))
                ?? new List<NpcPreferencesWikiInfo>();
            _npcPreferences = preferences
                .Where(preference => !string.IsNullOrEmpty(preference.Name))
                .ToDictionary(preference => preference.Name);
            _events = ReadJson<List<EventWikiInfo>>(Path.Combine(dir, "events.json")) ?? new List<EventWikiInfo>();
        }

        private static T ReadJson<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                using (var stream = File.OpenRead(path))
                    return new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
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

        public static NpcPreferencesWikiInfo GetNpcPreferences(string npcName)
        {
            EnsureLoaded();
            return _npcPreferences.TryGetValue(npcName, out var info) ? info : null;
        }

        public static IReadOnlyList<EventWikiInfo> GetAllEvents()
        {
            EnsureLoaded();
            return _events;
        }
    }
}
