using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        private static readonly EventWikiInfo[] Events =
        {
            new EventWikiInfo { Name = "Goblin Army", Summoning = "Use a Goblin Battle Standard." },
            new EventWikiInfo { Name = "Blood Moon", Summoning = "May begin at night." },
            new EventWikiInfo { Name = "Frost Moon", Summoning = "Use a Naughty Present." }
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
        public void EventSearchIsCaseInsensitiveAndAlwaysAlphabetical()
        {
            Assert.Equal(
                new[] { "Blood Moon", "Frost Moon" },
                EncyclopediaQueries.FilterEvents(Events, "mOoN").Select(item => item.Name));
            Assert.Equal(
                new[] { "Blood Moon", "Frost Moon", "Goblin Army" },
                EncyclopediaQueries.FilterEvents(Events, null).Select(item => item.Name));
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
            Assert.Equal("Blood Moon", source.LoadEvents().Single().Name);
            Assert.Equal(99, source.ShopFor(1).Single().ItemId);
            Assert.Equal(1, source.SellersOf(99).Single().NpcId);
            Assert.True(source.GetNpcPersonality(1, "Guide").HasUniqueBehavior);
            Assert.Contains("vacant house", source.GetNpcHowToGet("Guide"));
            Assert.Null(source.GetNpcHowToGet("Zombie"));
            Assert.Equal("Demon Altar", source.StationName(26));
            Assert.Equal("Expert tactics", source.GetBossInfo("Eye of Cthulhu").ExpertMode);
        }

        [Fact]
        public void MixedAcquisitionRetainsCraftingDropsAndSellers()
        {
            var recipe = new RecipeInfo { ResultItemId = 99 };
            var drop = new DropEntry { NpcId = 13, ItemId = 99, Chance = 0.25f };
            var seller = new ShopSellerEntry { NpcId = 1, Cost = 100 };

            ItemAcquisitionDetails acquisition = EncyclopediaQueries.ComposeItemAcquisition(
                new[] { recipe },
                new[] { drop },
                new[] { seller });

            Assert.Same(recipe, acquisition.Recipes.Single());
            Assert.Same(drop, acquisition.Drops.Single());
            Assert.Same(seller, acquisition.Sellers.Single());
            Assert.False(acquisition.NeedsUnknownSourceFallback);
        }

        [Fact]
        public void ShopSnapshotResetDiscardsConditionalInventoryAndSellerIndex()
        {
            var snapshot = new ShopCatalogueSnapshot();
            snapshot.StoreShop(1, new List<ShopItemEntry>
            {
                new ShopItemEntry { ItemId = 99, Cost = 100 }
            });
            snapshot.BuildSellerIndex(new[] { 1 });

            Assert.True(snapshot.TryGetShop(1, out _));
            Assert.Equal(1, snapshot.SellersOf(99).Single().NpcId);

            snapshot.Reset();

            Assert.False(snapshot.TryGetShop(1, out _));
            Assert.False(snapshot.HasSellerIndex);
            snapshot.StoreShop(1, new List<ShopItemEntry>
            {
                new ShopItemEntry { ItemId = 42, Cost = 250 }
            });
            snapshot.BuildSellerIndex(new[] { 1 });
            Assert.Empty(snapshot.SellersOf(99));
            Assert.Equal(1, snapshot.SellersOf(42).Single().NpcId);
        }

        [Fact]
        public void ShopSnapshotInvalidationRefreshesOneShopAndSellerIndex()
        {
            var snapshot = new ShopCatalogueSnapshot();
            snapshot.StoreShop(1, new List<ShopItemEntry>
            {
                new ShopItemEntry { ItemId = 99, Cost = 100 }
            });
            snapshot.StoreShop(2, new List<ShopItemEntry>
            {
                new ShopItemEntry { ItemId = 7, Cost = 200 }
            });
            snapshot.BuildSellerIndex(new[] { 1, 2 });

            snapshot.InvalidateShop(1);

            Assert.False(snapshot.TryGetShop(1, out _));
            Assert.True(snapshot.TryGetShop(2, out _));
            Assert.False(snapshot.HasSellerIndex);

            snapshot.StoreShop(1, new List<ShopItemEntry>
            {
                new ShopItemEntry { ItemId = 42, Cost = 250 }
            });
            snapshot.BuildSellerIndex(new[] { 1, 2 });

            Assert.Empty(snapshot.SellersOf(99));
            Assert.Equal(1, snapshot.SellersOf(42).Single().NpcId);
            Assert.Equal(2, snapshot.SellersOf(7).Single().NpcId);
        }

        [Fact]
        public void WikiPreferencesTakePrecedenceAndRetainLiveUniqueBehaviour()
        {
            var live = new NpcPersonalityEntry { HasUniqueBehavior = true };
            live.Biomes.Add(new PersonalityPreference { Level = Affection.Hate, Label = "Live biome" });
            var wiki = new NpcPreferencesWikiInfo
            {
                LikeBiomes = new[] { "Forest" },
                LoveNeighbors = new[] { "Wizard" }
            };

            NpcPersonalityEntry resolved = NpcPreferenceResolver.Resolve(wiki, live);

            Assert.True(resolved.HasUniqueBehavior);
            Assert.Equal("Forest", resolved.Biomes.Single().Label);
            Assert.Equal(Affection.Love, resolved.Neighbors.Single().Level);
            Assert.DoesNotContain(resolved.Biomes, item => item.Label == "Live biome");
            Assert.Same(live, NpcPreferenceResolver.Resolve(null, live));
        }

        [Fact]
        public void AcquisitionOnlyWikiEntryFallsBackToLivePreferences()
        {
            var live = new NpcPersonalityEntry { HasUniqueBehavior = true };
            live.Biomes.Add(new PersonalityPreference { Level = Affection.Like, Label = "Forest" });
            var acquisitionOnly = new NpcPreferencesWikiInfo
            {
                HowToGet = "Arrives after a vacant house becomes available."
            };

            Assert.Same(live, NpcPreferenceResolver.Resolve(acquisitionOnly, live));
            Assert.Null(NpcPreferenceResolver.Resolve(acquisitionOnly, null));
        }

        [Fact]
        public void NpcPreferenceJsonContainsCleanAcquisitionGuidance()
        {
            string path = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "src", "TerrariaEncyclopedia", "WikiData", "npc-preferences.json"));
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement[] entries = document.RootElement.EnumerateArray().ToArray();

            string[] names = entries.Select(entry => entry.GetProperty("Name").GetString()).ToArray();
            Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
            Assert.Equal(names.Length, names.Distinct().Count());
            Assert.DoesNotContain(entries, entry => entry.GetProperty("WikiPage").GetString() == "Bunny");

            foreach (JsonElement entry in entries)
            {
                if (!entry.TryGetProperty("HowToGet", out JsonElement guidance))
                    continue;

                string text = guidance.GetString();
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.True(
                    text.IndexOf("Verify", StringComparison.OrdinalIgnoreCase) < 0,
                    $"{entry.GetProperty("Name").GetString()} contains a verification artefact.");
                Assert.True(text.IndexOf("[edit]", StringComparison.OrdinalIgnoreCase) < 0);
                Assert.DoesNotMatch(@"\[\d+\]", text);
                Assert.DoesNotContain("harmless critter", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("vendor", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("sells", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("shop", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("defend", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("attack", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("dropping", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("loot", text, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Equal(
                "Merchant will move in when a vacant house is available and all players have more than 50 Silver in their inventories.",
                GuidanceFor(entries, "Merchant"));
            Assert.Equal(
                "Tavernkeep will move in when a vacant house is available after rescuing the Unconscious Man once the Eater of Worlds or Brain of Cthulhu has been defeated.",
                GuidanceFor(entries, "Tavernkeep"));
            Assert.Equal(
                "Traveling Merchant randomly visits for one in-game day when at least two other NPCs are present. He does not require a house and can appear between 4:30 AM and 12:00 PM.",
                GuidanceFor(entries, "Traveling Merchant"));
        }

        [Theory]
        [InlineData(0, true, false, false, false, 4)]
        [InlineData(2, false, true, false, false, 6)]
        [InlineData(2, false, false, true, false, 6)]
        [InlineData(2, false, false, false, true, 2)]
        public void DisplayedCritAddsPlayerBaseOnlyToStandardCritClasses(
            int itemCriticalChance,
            bool melee,
            bool ranged,
            bool magic,
            bool summon,
            int expected)
        {
            Assert.Equal(
                expected,
                EncyclopediaQueries.DisplayedCriticalChance(
                    itemCriticalChance,
                    melee,
                    ranged,
                    magic,
                    summon));
        }

        private static int FirstItem(ItemSortMode mode)
        {
            return EncyclopediaQueries.FilterItems(Items, null, false, null, mode, _ => false)[0].Id;
        }

        private static string GuidanceFor(IEnumerable<JsonElement> entries, string name) =>
            entries.Single(entry => entry.GetProperty("Name").GetString() == name)
                .GetProperty("HowToGet")
                .GetString();

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
            public void BeginBrowseSession() { }
            public void RefreshShopFor(int npcId) { }
            public IReadOnlyList<ItemEntry> LoadItems() => Items;
            public IReadOnlyList<NpcEntry> LoadNpcs() => Npcs;
            public IReadOnlyList<EventWikiInfo> LoadEvents() => new[]
            {
                new EventWikiInfo { Name = "Blood Moon" }
            };
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
            public IReadOnlyList<ShopItemEntry> ShopFor(int npcId) => npcId == 1
                ? new[] { new ShopItemEntry { ItemId = 99, Cost = 100 } }
                : new ShopItemEntry[0];
            public IReadOnlyList<ShopSellerEntry> SellersOf(int itemId) => itemId == 99
                ? new[] { new ShopSellerEntry { NpcId = 1, Cost = 100 } }
                : new ShopSellerEntry[0];
            public NpcPersonalityEntry GetNpcPersonality(int npcId, string npcName) =>
                new NpcPersonalityEntry { HasUniqueBehavior = npcId == 1 };
            public string GetNpcHowToGet(string npcName) => npcName == "Guide"
                ? "Moves in when a vacant house is available."
                : null;
            public string StationName(int tileId) => tileId == 26 ? "Demon Altar" : "Unknown";
            public bool IsHardmodeItem(string itemName) => itemName == "Nebula Blaze";
            public bool IsHardmodeNpc(string npcName) => npcName == "Retinazer";
            public BossWikiInfo GetBossInfo(string npcName) => new BossWikiInfo
            {
                Name = npcName,
                ExpertMode = "Expert tactics"
            };
            public IReadOnlyList<BossChecklistEntry> GetBossChecklist() => new BossChecklistEntry[0];
        }
    }
}
