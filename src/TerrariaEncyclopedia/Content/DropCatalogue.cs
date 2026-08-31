using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent.ItemDropRules;

namespace TerrariaEncyclopedia.Content
{
    /// <summary>
    /// Reads Terraria's real, live drop-rule database (Main.ItemDropsDB) — the same rule tree
    /// the game itself uses to actually drop loot. ReportDroprates() already flattens the tree
    /// (chained/nested rules, luck scaling) into simple per-item chance entries; we don't
    /// re-implement any of that. Indexed both ways: what an NPC drops, and what drops an item
    /// (reverse "how do I get this").
    /// </summary>
    public static class DropCatalogue
    {
        private static Dictionary<int, List<DropEntry>> _byNpc;
        private static Dictionary<int, List<DropEntry>> _byItem;

        public static void EnsureBuilt(List<NpcEntry> npcs)
        {
            if (_byNpc != null) return;

            _byNpc = new Dictionary<int, List<DropEntry>>();
            _byItem = new Dictionary<int, List<DropEntry>>();

            foreach (var npc in npcs)
            {
                List<IItemDropRule> rules;
                try
                {
                    rules = Main.ItemDropsDB.GetRulesForNPCID(npc.Id, includeGlobalDrops: false);
                }
                catch
                {
                    continue;
                }

                if (rules == null || rules.Count == 0) continue;

                var rateInfos = new List<DropRateInfo>();
                var feed = new DropRateInfoChainFeed(1f);
                foreach (var rule in rules)
                {
                    try { rule.ReportDroprates(rateInfos, feed); }
                    catch { /* a handful of rules need live player/world state we don't have here */ }
                }

                var entries = rateInfos.Select(info => new DropEntry
                {
                    NpcId = npc.Id,
                    ItemId = info.itemId,
                    StackMin = info.stackMin,
                    StackMax = info.stackMax,
                    Chance = info.dropRate,
                    Conditions = info.conditions?.Select(DescribeCondition).Where(c => c != null).ToList() ?? new List<string>()
                }).ToList();

                if (entries.Count == 0) continue;
                _byNpc[npc.Id] = entries;

                foreach (var entry in entries)
                {
                    if (!_byItem.TryGetValue(entry.ItemId, out var list))
                        _byItem[entry.ItemId] = list = new List<DropEntry>();
                    list.Add(entry);
                }
            }
        }

        public static List<DropEntry> DropsFor(int npcId) =>
            _byNpc.TryGetValue(npcId, out var list) ? list : Empty;

        public static List<DropEntry> SourcesFor(int itemId) =>
            _byItem.TryGetValue(itemId, out var list) ? list : Empty;

        // These are internal Re-Logic implementation-detail conditions (e.g. a compatibility
        // shim tying an old boolean into the newer rule system) — trivially true for every
        // entry on a boss's own drop list, so they convey nothing a player didn't already know.
        private static readonly HashSet<string> NoiseConditions = new HashSet<string>
        {
            "LegacyHack_IsABoss", "LegacyHack_IsBossAndExpert", "LegacyHack_IsBossAndNotExpert",
            "MechanicalBossesDummyCondition", "NeverTrue",
        };

        // ponytail: condition classes are a closed, well-named set (DownedPlantera, IsHardmode,
        // IsExpert, ...) — humanizing the class name (space before each capital, underscores to
        // spaces) reads fine for the vast majority. A few (IsUsingSpecificAIValues, MissingTwin)
        // will read awkwardly; fix those with a curated override here if they turn out to matter.
        private static string DescribeCondition(IItemDropRuleCondition condition)
        {
            string name = condition.GetType().Name;
            if (NoiseConditions.Contains(name)) return null;

            name = name.Replace('_', ' ');
            return Regex.Replace(name, "(?<!^)([A-Z])", " $1").Replace("  ", " ").Trim();
        }

        private static readonly List<DropEntry> Empty = new List<DropEntry>();
    }
}
