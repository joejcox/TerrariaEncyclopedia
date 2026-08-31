using System.Collections.Generic;

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

    public sealed class ItemEntry
    {
        public int Id;
        public string Name;
        public int MaxStack;
        public int Rarity;
        public int Damage;
        public int Defense;
        public ItemCategory Category;
        public string Set;
        public int SellValue;
        public bool IsExpertExclusive;
        public int BuffType;
        public string BuffName;
        public string BuffTooltip;
        public int BuffDurationSeconds;
        public int ProjectileType;
        public string ProjectileName;
        public int ResearchCount;
    }

    public sealed class NpcEntry
    {
        public int Id;
        public string Name;
        public int Life;
        public int Defense;
        public int Damage;
        public bool IsBoss;
        public bool IsTownNpc;
        public string Description;
    }

    public sealed class RecipeIngredient
    {
        public int ItemId;
        public int Stack;
    }

    public sealed class RecipeInfo
    {
        public int ResultItemId;
        public int ResultStack;
        public List<RecipeIngredient> Ingredients = new List<RecipeIngredient>();
        public List<int> StationTileIds = new List<int>();
    }

    public sealed class DropEntry
    {
        public int NpcId;
        public int ItemId;
        public int StackMin;
        public int StackMax;
        public float Chance;
        public List<string> Conditions = new List<string>();
    }

    public sealed class BossWikiInfo
    {
        public string Name { get; set; }
        public string WikiPage { get; set; }
        public string[] MatchNames { get; set; }
        public string[] Types { get; set; }
        public string[] Environments { get; set; }
        public string AiType { get; set; }
        public string KbResist { get; set; }
        public string[] ImmuneTo { get; set; }
        public string Summoning { get; set; }
        public string ExpertMode { get; set; }
        public string Aftermath { get; set; }
        public string Tips { get; set; }
    }
}
