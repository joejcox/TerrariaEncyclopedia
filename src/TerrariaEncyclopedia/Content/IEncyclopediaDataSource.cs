using System.Collections.Generic;

namespace TerrariaEncyclopedia.Content
{
    public interface IEncyclopediaDataSource
    {
        IReadOnlyList<ItemEntry> LoadItems();

        IReadOnlyList<NpcEntry> LoadNpcs();

        IReadOnlyList<RecipeInfo> RecipesFor(int itemId);

        IReadOnlyList<RecipeInfo> UsedIn(int itemId);

        IReadOnlyList<DropEntry> DropsFor(int npcId);

        IReadOnlyList<DropEntry> SourcesFor(int itemId);

        string StationName(int tileId);

        bool IsHardmodeItem(string itemName);

        bool IsHardmodeNpc(string npcName);

        BossWikiInfo GetBossInfo(string npcName);

        IReadOnlyList<BossChecklistEntry> GetBossChecklist();
    }
}
