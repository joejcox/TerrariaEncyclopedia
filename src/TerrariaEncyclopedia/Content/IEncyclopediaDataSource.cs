using System.Collections.Generic;

namespace TerrariaEncyclopedia.Content
{
    public interface IEncyclopediaDataSource
    {
        void BeginBrowseSession();

        void RefreshShopFor(int npcId);

        IReadOnlyList<ItemEntry> LoadItems();

        IReadOnlyList<NpcEntry> LoadNpcs();

        IReadOnlyList<EventWikiInfo> LoadEvents();

        IReadOnlyList<RecipeInfo> RecipesFor(int itemId);

        IReadOnlyList<RecipeInfo> UsedIn(int itemId);

        IReadOnlyList<DropEntry> DropsFor(int npcId);

        IReadOnlyList<DropEntry> SourcesFor(int itemId);

        IReadOnlyList<ShopItemEntry> ShopFor(int npcId);

        IReadOnlyList<ShopSellerEntry> SellersOf(int itemId);

        NpcPersonalityEntry GetNpcPersonality(int npcId, string npcName);

        string GetNpcHowToGet(string npcName);

        string StationName(int tileId);

        bool IsHardmodeItem(string itemName);

        bool IsHardmodeNpc(string npcName);

        BossWikiInfo GetBossInfo(string npcName);

        IReadOnlyList<BossChecklistEntry> GetBossChecklist();
    }
}
