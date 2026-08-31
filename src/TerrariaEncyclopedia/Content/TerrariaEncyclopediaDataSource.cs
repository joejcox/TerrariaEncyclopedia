using System;
using System.Collections.Generic;
using System.Linq;

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

        public void BeginBrowseSession() => NpcShopCatalogue.BeginBrowseSession();

        public void RefreshShopFor(int npcId) => NpcShopCatalogue.RefreshShopFor(npcId);

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

        public IReadOnlyList<EventWikiInfo> LoadEvents() => WikiEnrichment.GetAllEvents();

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

        public IReadOnlyList<ShopItemEntry> ShopFor(int npcId) => NpcShopCatalogue.ShopFor(npcId);

        public IReadOnlyList<ShopSellerEntry> SellersOf(int itemId) => NpcShopCatalogue.SellersOf(itemId);

        public NpcPersonalityEntry GetNpcPersonality(int npcId, string npcName)
        {
            EnsureNpcsLoaded();
            NpcPersonalityCatalogue.EnsureBuilt(npcs);
            return NpcPreferenceResolver.Resolve(
                WikiEnrichment.GetNpcPreferences(npcName),
                NpcPersonalityCatalogue.Get(npcId));
        }

        public string GetNpcHowToGet(string npcName) =>
            WikiEnrichment.GetNpcPreferences(npcName)?.HowToGet;

        public string StationName(int tileId)
        {
            EnsureItemsLoaded();
            return RecipeCatalogue.StationName(tileId);
        }

        public bool IsHardmodeItem(string itemName) => WikiEnrichment.IsHardmodeItem(itemName);

        public bool IsHardmodeNpc(string npcName) => WikiEnrichment.IsHardmodeNpc(npcName);

        public BossWikiInfo GetBossInfo(string npcName) => WikiEnrichment.GetBossInfo(npcName);

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
