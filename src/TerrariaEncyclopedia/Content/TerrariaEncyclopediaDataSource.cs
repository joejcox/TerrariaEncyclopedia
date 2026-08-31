using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace TerrariaEncyclopedia.Content
{
    /// <summary>
    /// Narrow read-only adapter over Terraria's already-loaded content databases.
    /// The browser depends on this contract rather than mutable game globals.
    /// </summary>
    public sealed class TerrariaEncyclopediaDataSource : IEncyclopediaDataSource
    {
        private List<ItemEntry> items;
        private List<NpcEntry> npcs;

        public IReadOnlyList<ItemEntry> LoadItems()
        {
            if (items == null)
            {
                items = ItemCatalogue.Build();
                RecipeCatalogue.EnsureBuilt(items);
            }

            return items;
        }

        public IReadOnlyList<NpcEntry> LoadNpcs()
        {
            if (npcs == null)
            {
                npcs = NpcCatalogue.Build();
                DropCatalogue.EnsureBuilt(npcs);
            }

            return npcs;
        }

        public IReadOnlyList<RecipeInfo> RecipesFor(int itemId)
        {
            EnsureItemsLoaded();
            return RecipeCatalogue.RecipesFor(itemId);
        }

        public IReadOnlyList<RecipeInfo> UsedIn(int itemId)
        {
            EnsureItemsLoaded();
            return RecipeCatalogue.UsedIn(itemId);
        }

        public IReadOnlyList<DropEntry> DropsFor(int npcId)
        {
            EnsureNpcsLoaded();
            return DropCatalogue.DropsFor(npcId);
        }

        public IReadOnlyList<DropEntry> SourcesFor(int itemId)
        {
            EnsureNpcsLoaded();
            return DropCatalogue.SourcesFor(itemId);
        }

        public string StationName(int tileId)
        {
            EnsureItemsLoaded();
            return RecipeCatalogue.StationName(tileId);
        }

        public bool IsHardmodeItem(string itemName) => WikiEnrichment.IsHardmodeItem(itemName);

        public bool IsHardmodeNpc(string npcName) => WikiEnrichment.IsHardmodeNpc(npcName);

        public BossWikiInfo GetBossInfo(string npcName) => WikiEnrichment.GetBossInfo(npcName);

        public IReadOnlyList<BossChecklistEntry> GetBossChecklist()
        {
            EnsureNpcsLoaded();
            return BossChecklistCatalogue.Build(npcs, ReadBossProgress());
        }

        private static BossProgressSnapshot ReadBossProgress()
        {
            // Terraria resets these fields while at the title screen. Never present old-world
            // state as progress there; the panel refreshes this snapshot every time it draws.
            if (Main.gameMenu) return new BossProgressSnapshot();

            return new BossProgressSnapshot
            {
                HasWorld = true,
                KingSlime = NPC.downedSlimeKing,
                EyeOfCthulhu = NPC.downedBoss1,
                EvilBoss = NPC.downedBoss2,
                QueenBee = NPC.downedQueenBee,
                Skeletron = NPC.downedBoss3,
                Deerclops = NPC.downedDeerclops,
                WallOfFlesh = Main.hardMode,
                QueenSlime = NPC.downedQueenSlime,
                Destroyer = NPC.downedMechBoss1,
                Twins = NPC.downedMechBoss2,
                SkeletronPrime = NPC.downedMechBoss3,
                Plantera = NPC.downedPlantBoss,
                Golem = NPC.downedGolemBoss,
                DukeFishron = NPC.downedFishron,
                EmpressOfLight = NPC.downedEmpressOfLight,
                LunaticCultist = NPC.downedAncientCultist,
                MoonLord = NPC.downedMoonlord,
            };
        }

        private void EnsureItemsLoaded()
        {
            if (items == null)
                LoadItems();
        }

        private void EnsureNpcsLoaded()
        {
            if (npcs == null)
                LoadNpcs();
        }
    }
}
