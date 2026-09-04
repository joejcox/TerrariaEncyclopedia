using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.Personalities;
using Terraria.Localization;

namespace TerrariaEncyclopedia.Content
{
    /// <summary>
    /// Reads Terraria's own living-preference data — the same PersonalityDatabase the game
    /// itself uses to price NPC shops based on nearby biomes/neighbors, not data we invented.
    /// Built once via PersonalityDatabasePopulator (the same populator the game runs at
    /// startup), independent of any particular player's current location.
    /// </summary>
    public static class NpcPersonalityCatalogue
    {
        // BiomePreferenceListTrait's own preference list is private — there's no public
        // accessor, so this one field read via reflection is unavoidable (same precedent as
        // NpcCatalogue's FlavorTextBestiaryInfoElement._key read).
        private static readonly FieldInfo BiomePreferencesField = typeof(BiomePreferenceListTrait)
            .GetField("_preferences", BindingFlags.NonPublic | BindingFlags.Instance);

        private static Dictionary<int, NpcPersonalityEntry> _byNpc;

        public static void EnsureBuilt(List<NpcEntry> npcs)
        {
            if (_byNpc != null) return;
            _byNpc = new Dictionary<int, NpcPersonalityEntry>();

            var db = new PersonalityDatabase();
            new PersonalityDatabasePopulator().Populate(db);

            var namesById = new Dictionary<int, string>();
            foreach (var npc in npcs) namesById[npc.Id] = npc.Name;

            foreach (var npc in npcs)
            {
                var profile = db.GetByNPCID(npc.Id);
                if (profile == null || profile.ShopModifiers.Count == 0) continue;

                var entry = new NpcPersonalityEntry();
                foreach (var trait in profile.ShopModifiers)
                {
                    if (trait is NPCPreferenceTrait npcPref)
                    {
                        if (namesById.TryGetValue(npcPref.NpcId, out string name))
                            entry.Neighbors.Add(new PersonalityPreference { Level = Convert(npcPref.Level), Label = name });
                    }
                    else if (trait is BiomePreferenceListTrait biomeTrait)
                    {
                        if (BiomePreferencesField?.GetValue(biomeTrait) is List<BiomePreferenceListTrait.BiomePreference> prefs)
                        {
                            foreach (var pref in prefs)
                            {
                                // Language.GetTextValue(pref.Biome.NameKey) alone returns the raw,
                                // untranslated key (e.g. "NormalUnderground") — ShopHelper's own
                                // BiomeNameByKey prefixes it with "TownNPCMoodBiomes." first
                                // (confirmed via IL disassembly), but that method is internal, so
                                // the one-line prefix is reproduced directly here.
                                string label = Language.GetTextValue("TownNPCMoodBiomes." + pref.Biome.NameKey);
                                entry.Biomes.Add(new PersonalityPreference { Level = Convert(pref.Affection), Label = label });
                            }
                        }
                    }
                    else if (trait is AllPersonalitiesModifier)
                    {
                        entry.HasUniqueBehavior = true;
                    }
                }

                if (entry.Biomes.Count > 0 || entry.Neighbors.Count > 0 || entry.HasUniqueBehavior)
                    _byNpc[npc.Id] = entry;
            }
        }

        public static NpcPersonalityEntry Get(int npcId) =>
            _byNpc != null && _byNpc.TryGetValue(npcId, out var entry) ? entry : null;

        private static Affection Convert(AffectionLevel level)
        {
            switch (level)
            {
                case AffectionLevel.Love: return Affection.Love;
                case AffectionLevel.Like: return Affection.Like;
                case AffectionLevel.Dislike: return Affection.Dislike;
                default: return Affection.Hate;
            }
        }
    }
}
