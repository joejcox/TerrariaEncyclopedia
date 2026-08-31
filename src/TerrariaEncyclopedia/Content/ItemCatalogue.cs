using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace TerrariaEncyclopedia.Content
{
    public enum ItemCategory
    {
        Weapon,
        Armor,
        Accessory,
        Tool,
        Consumable,
        Placeable,
        Misc
    }

    public class ItemEntry
    {
        public int Id;
        public string Name;
        public int MaxStack;
        public int Rarity;
        public int Damage;
        public int Defense;
        public ItemCategory Category;
        public string Set; // material tier, e.g. "Gold" — null if not part of a recognized set

        public int SellValue; // copper; 0 = not sellable
        public bool IsExpertExclusive;
        public int BuffType; // 0 = grants no buff
        public string BuffName;
        public string BuffTooltip;
        public int BuffDurationSeconds;
        public int ProjectileType; // 0 = doesn't shoot/create a projectile
        public string ProjectileName;
        public int ResearchCount; // Journey Mode sacrifice-to-unlock count; 0 = not researchable
    }

    /// <summary>
    /// Reads Terraria's currently-loaded item definitions dynamically (never hardcoded) —
    /// same approach already proven working by the framework's own ItemSpawner mod.
    /// Categorization is intentionally basic per project.md's Phase 2 scope; no recipes yet.
    /// </summary>
    public static class ItemCatalogue
    {
        // ponytail: Terraria has no "set" field on Item — this is a curated, ordered guess at
        // the material-tier progression (used both to detect a set from the item's first name
        // word, and to sort sets in roughly the order you'd unlock them in-game). It will miss
        // anything not in this list and can't distinguish e.g. "Necro" (armor-only, no ore) from
        // a real ore tier. Refine this list based on what's missing/wrong once it's actually used.
        private static readonly string[] TierOrder =
        {
            "Wood", "Copper", "Tin", "Iron", "Lead", "Silver", "Tungsten", "Gold", "Platinum",
            "Meteor", "Shadow", "Necro", "Crimson", "Demonite", "Molten",
            "Cobalt", "Palladium", "Mythril", "Orichalcum", "Adamantite", "Titanium",
            "Hallowed", "Chlorophyte", "Turtle", "Beetle", "Spectre", "Shroomite",
            "Solar", "Vortex", "Nebula", "Stardust",
        };

        private static readonly HashSet<string> KnownSets = new HashSet<string>(TierOrder);

        /// <summary>Sort key for a set name — earlier tiers first, unrecognized/no set sorts last.</summary>
        public static int TierIndex(string set)
        {
            if (set == null) return int.MaxValue;
            int idx = Array.IndexOf(TierOrder, set);
            return idx >= 0 ? idx : int.MaxValue - 1;
        }

        private static string DetectSet(string name)
        {
            string first = name.Split(' ')[0];
            return KnownSets.Contains(first) ? first : null;
        }

        public static List<ItemEntry> Build()
        {
            var list = new List<ItemEntry>();
            var researchCounts = Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance
                ?.SacrificeCountNeededByItemId;

            for (int i = 1; i < ItemID.Count; i++)
            {
                try
                {
                    var item = new Item();
                    item.SetDefaults(i);

                    string name = item.Name;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var entry = new ItemEntry
                    {
                        Id = i,
                        Name = name,
                        MaxStack = item.maxStack,
                        Rarity = item.rare,
                        Damage = item.damage,
                        Defense = item.defense,
                        Category = Classify(item),
                        Set = DetectSet(name),
                        SellValue = item.value,
                        IsExpertExclusive = item.expert,
                        ProjectileType = item.shoot,
                    };

                    if (item.shoot > 0)
                        entry.ProjectileName = Lang.GetProjectileName(item.shoot)?.Value;

                    if (item.buffType > 0)
                    {
                        entry.BuffType = item.buffType;
                        entry.BuffName = Lang.GetBuffName(item.buffType);
                        entry.BuffTooltip = Lang.GetBuffDescription(item.buffType);
                        entry.BuffDurationSeconds = item.buffTime / 60;
                    }

                    if (researchCounts != null && researchCounts.TryGetValue(i, out int count))
                        entry.ResearchCount = count;

                    list.Add(entry);
                }
                catch
                {
                    // Some IDs are unused/reserved and throw on SetDefaults — skip them.
                }
            }

            // Natural order here is by ID; ItemBrowserPanel re-sorts per the user's chosen SortMode.
            return list;
        }

        private static ItemCategory Classify(Item item)
        {
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0) return ItemCategory.Tool;
            if (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) return ItemCategory.Armor;
            if (item.accessory) return ItemCategory.Accessory;
            if (item.damage > 0 && (item.melee || item.ranged || item.magic || item.summon)) return ItemCategory.Weapon;
            if (item.potion || item.healLife > 0 || item.healMana > 0 || item.buffType > 0) return ItemCategory.Consumable;
            if (item.createTile >= 0 || item.createWall >= 0) return ItemCategory.Placeable;
            return ItemCategory.Misc;
        }
    }
}
