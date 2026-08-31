using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrariaEncyclopedia.Content
{
    /// <summary>Maps Terraria's world flags onto a stable, vanilla-only progression order.</summary>
    public static class BossChecklistCatalogue
    {
        private sealed class Definition
        {
            public readonly string Name;
            public readonly string[] NpcNames;
            public readonly Func<BossProgressSnapshot, bool> IsDefeated;

            public Definition(string name, string[] npcNames, Func<BossProgressSnapshot, bool> isDefeated)
            {
                Name = name;
                NpcNames = npcNames;
                IsDefeated = isDefeated;
            }
        }

        private static readonly Definition[] Definitions =
        {
            new Definition("King Slime", new[] { "King Slime" }, p => p.KingSlime),
            new Definition("Eye of Cthulhu", new[] { "Eye of Cthulhu" }, p => p.EyeOfCthulhu),
            new Definition("Eater of Worlds / Brain of Cthulhu", new[] { "Eater of Worlds", "Brain of Cthulhu" }, p => p.EvilBoss),
            new Definition("Queen Bee", new[] { "Queen Bee" }, p => p.QueenBee),
            new Definition("Skeletron", new[] { "Skeletron" }, p => p.Skeletron),
            new Definition("Deerclops", new[] { "Deerclops" }, p => p.Deerclops),
            new Definition("Wall of Flesh", new[] { "Wall of Flesh" }, p => p.WallOfFlesh),
            new Definition("Queen Slime", new[] { "Queen Slime" }, p => p.QueenSlime),
            new Definition("The Destroyer", new[] { "The Destroyer", "Destroyer" }, p => p.Destroyer),
            new Definition("The Twins", new[] { "Retinazer", "Spazmatism" }, p => p.Twins),
            new Definition("Skeletron Prime", new[] { "Skeletron Prime" }, p => p.SkeletronPrime),
            new Definition("Plantera", new[] { "Plantera" }, p => p.Plantera),
            new Definition("Golem", new[] { "Golem" }, p => p.Golem),
            new Definition("Duke Fishron", new[] { "Duke Fishron" }, p => p.DukeFishron),
            new Definition("Empress of Light", new[] { "Empress of Light" }, p => p.EmpressOfLight),
            new Definition("Lunatic Cultist", new[] { "Lunatic Cultist" }, p => p.LunaticCultist),
            new Definition("Moon Lord", new[] { "Moon Lord" }, p => p.MoonLord),
        };

        public static IReadOnlyList<BossChecklistEntry> Build(
            IReadOnlyList<NpcEntry> npcs,
            BossProgressSnapshot progress)
        {
            if (npcs == null) throw new ArgumentNullException(nameof(npcs));
            progress = progress ?? new BossProgressSnapshot();

            var result = new List<BossChecklistEntry>(Definitions.Length);
            foreach (var definition in Definitions)
            {
                NpcEntry npc = definition.NpcNames
                    .Select(name => npcs.FirstOrDefault(candidate =>
                        candidate.IsBoss && string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault(candidate => candidate != null);

                // All definitions correspond to vanilla NPCs. Skipping a missing game entry is
                // safer than rendering a dead navigation row if a future game version changes it.
                if (npc == null) continue;

                result.Add(new BossChecklistEntry
                {
                    Name = definition.Name,
                    Npc = npc,
                    Defeated = progress.HasWorld && definition.IsDefeated(progress)
                });
            }

            return result;
        }
    }
}
