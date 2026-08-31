using System.Collections.Generic;
using System.Linq;
using TerrariaEncyclopedia.Content;
using Xunit;

namespace TerrariaEncyclopedia.Tests
{
    public sealed class EncyclopediaBehaviourTests
    {
        private static readonly ItemEntry[] Items =
        {
            Item(42, "Copper Pickaxe", ItemCategory.Tool, rarity: 0, set: "Copper", research: 1),
            Item(7, "Nebula Blaze", ItemCategory.Weapon, rarity: 10, set: "Nebula", research: 1),
            Item(99, "Healing Potion", ItemCategory.Consumable, rarity: 1, research: 30),
            Item(12, "Copper Helmet", ItemCategory.Armor, rarity: 0, set: "Copper", research: 1)
        };

        private static readonly NpcEntry[] Npcs =
        {
            Npc(1, "Guide", 250, town: true),
            Npc(50, "Zombie", 45),
            Npc(13, "Eye of Cthulhu", 2800, boss: true),
            Npc(125, "Retinazer", 20000, boss: true)
        };

        [Fact]
        public void ItemSearchSupportsExactIdsAndCaseInsensitiveNames()
        {
            IReadOnlyList<ItemEntry> byId = EncyclopediaQueries.FilterItems(
                Items, null, false, "42", ItemSortMode.NameAscending, _ => false);
            IReadOnlyList<ItemEntry> byName = EncyclopediaQueries.FilterItems(
                Items, null, false, "pOtIoN", ItemSortMode.NameAscending, _ => false);

            Assert.Equal(new[] { 42 }, byId.Select(item => item.Id));
            Assert.Equal(new[] { 99 }, byName.Select(item => item.Id));
        }

        [Fact]
        public void ItemFiltersRetainCategoryHardmodeAndAllSortModes()
        {
            IReadOnlyList<ItemEntry> hardmodeWeapons = EncyclopediaQueries.FilterItems(
                Items,
                ItemCategory.Weapon,
                true,
                string.Empty,
                ItemSortMode.NameAscending,
                name => name == "Nebula Blaze");

            Assert.Equal(new[] { 7 }, hardmodeWeapons.Select(item => item.Id));
            Assert.Equal(99, FirstItem(ItemSortMode.Newest));
            Assert.Equal(7, FirstItem(ItemSortMode.RarityDescending));
            Assert.Equal(12, FirstItem(ItemSortMode.NameAscending));
            Assert.Equal(7, FirstItem(ItemSortMode.NameDescending));
            Assert.Equal(7, FirstItem(ItemSortMode.Oldest));
            Assert.Equal(12, FirstItem(ItemSortMode.RarityAscending));
            Assert.Equal(12, FirstItem(ItemSortMode.SetGrouped));
        }

        [Fact]
        public void NpcFiltersDistinguishTownEnemiesBossesAndHardmode()
        {
            Assert.Equal(new[] { 1 }, QueryNpcs(NpcFilter.Town).Select(npc => npc.Id));
            Assert.Equal(new[] { 50 }, QueryNpcs(NpcFilter.Enemies).Select(npc => npc.Id));
            Assert.Equal(new[] { 13, 125 }, QueryNpcs(NpcFilter.Bosses).Select(npc => npc.Id));

            IReadOnlyList<NpcEntry> hardmode = EncyclopediaQueries.FilterNpcs(
                Npcs,
                NpcFilter.Bosses,
                true,
                "ret",
                NpcSortMode.LifeDescending,
                name => name == "Retinazer");
            Assert.Equal(new[] { 125 }, hardmode.Select(npc => npc.Id));
        }

        [Fact]
        public void NpcSortingRetainsNameAndLifeModes()
        {
            Assert.Equal(13, QueryNpcs(NpcFilter.All, NpcSortMode.NameAscending).First().Id);
            Assert.Equal(50, QueryNpcs(NpcFilter.All, NpcSortMode.NameDescending).First().Id);
            Assert.Equal(50, QueryNpcs(NpcFilter.All, NpcSortMode.LifeAscending).First().Id);
            Assert.Equal(125, QueryNpcs(NpcFilter.All, NpcSortMode.LifeDescending).First().Id);
        }

        [Fact]
        public void NavigationSupportsLinkedEntriesBackAndHome()
        {
            var navigation = new EntryNavigation();
            navigation.Open(42);
            navigation.Open(7);

            Assert.True(navigation.CanGoBack);
            Assert.True(navigation.TryGoBack(out int previous));
            Assert.Equal(42, previous);
            Assert.Equal(42, navigation.CurrentId);

            navigation.Home();
            Assert.False(navigation.CanGoBack);
            Assert.Null(navigation.CurrentId);
        }

        [Fact]
        public void NarrowDataSourceCarriesResearchRecipesDropsAndBossDetails()
        {
            var source = new FakeDataSource();

            Assert.Equal(30, source.LoadItems().Single(item => item.Id == 99).ResearchCount);
            Assert.Equal(42, source.RecipesFor(99).Single().Ingredients.Single().ItemId);
            Assert.Equal(99, source.DropsFor(13).Single().ItemId);
            Assert.Equal("Demon Altar", source.StationName(26));
            Assert.Equal("Expert tactics", source.GetBossInfo("Eye of Cthulhu").ExpertMode);
        }

        private static int FirstItem(ItemSortMode mode)
        {
            return EncyclopediaQueries.FilterItems(Items, null, false, null, mode, _ => false)[0].Id;
        }

        private static IReadOnlyList<NpcEntry> QueryNpcs(
            NpcFilter filter,
            NpcSortMode sort = NpcSortMode.NameAscending)
        {
            return EncyclopediaQueries.FilterNpcs(Npcs, filter, false, null, sort, _ => false);
        }

        private static ItemEntry Item(
            int id,
            string name,
            ItemCategory category,
            int rarity,
            string set = null,
            int research = 0)
        {
            return new ItemEntry
            {
                Id = id,
                Name = name,
                Category = category,
                Rarity = rarity,
                Set = set,
                ResearchCount = research
            };
        }

        private static NpcEntry Npc(int id, string name, int life, bool town = false, bool boss = false)
        {
            return new NpcEntry { Id = id, Name = name, Life = life, IsTownNpc = town, IsBoss = boss };
        }

        private sealed class FakeDataSource : IEncyclopediaDataSource
        {
            public IReadOnlyList<ItemEntry> LoadItems() => Items;
            public IReadOnlyList<NpcEntry> LoadNpcs() => Npcs;
            public IReadOnlyList<RecipeInfo> RecipesFor(int itemId) => itemId == 99
                ? new[]
                {
                    new RecipeInfo
                    {
                        ResultItemId = 99,
                        Ingredients = { new RecipeIngredient { ItemId = 42, Stack = 1 } }
                    }
                }
                : new RecipeInfo[0];
            public IReadOnlyList<RecipeInfo> UsedIn(int itemId) => new RecipeInfo[0];
            public IReadOnlyList<DropEntry> DropsFor(int npcId) => npcId == 13
                ? new[] { new DropEntry { NpcId = 13, ItemId = 99, Chance = 0.25f, StackMin = 1, StackMax = 2 } }
                : new DropEntry[0];
            public IReadOnlyList<DropEntry> SourcesFor(int itemId) => new DropEntry[0];
            public string StationName(int tileId) => tileId == 26 ? "Demon Altar" : "Unknown";
            public bool IsHardmodeItem(string itemName) => itemName == "Nebula Blaze";
            public bool IsHardmodeNpc(string npcName) => npcName == "Retinazer";
            public BossWikiInfo GetBossInfo(string npcName) => new BossWikiInfo
            {
                Name = npcName,
                ExpertMode = "Expert tactics"
            };
        }
    }
}
