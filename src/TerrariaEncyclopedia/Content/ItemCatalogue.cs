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
    }

    /// <summary>
    /// Reads Terraria's currently-loaded item definitions dynamically (never hardcoded) —
    /// same approach already proven working by the framework's own ItemSpawner mod.
    /// Categorization is intentionally basic per project.md's Phase 2 scope; no recipes yet.
    /// </summary>
    public static class ItemCatalogue
    {
        public static List<ItemEntry> Build()
        {
            var list = new List<ItemEntry>();

            for (int i = 1; i < ItemID.Count; i++)
            {
                try
                {
                    var item = new Item();
                    item.SetDefaults(i);

                    string name = item.Name;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    list.Add(new ItemEntry
                    {
                        Id = i,
                        Name = name,
                        MaxStack = item.maxStack,
                        Rarity = item.rare,
                        Damage = item.damage,
                        Defense = item.defense,
                        Category = Classify(item)
                    });
                }
                catch
                {
                    // Some IDs are unused/reserved and throw on SetDefaults — skip them.
                }
            }

            return list.OrderBy(e => e.Name).ToList();
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
