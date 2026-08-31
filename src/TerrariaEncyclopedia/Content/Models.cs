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

    public sealed class EventWikiInfo
    {
        public string Name { get; set; }
        public string WikiPage { get; set; }
        public string Summoning { get; set; }
        public string Completing { get; set; }
        public string Notes { get; set; }
        public string Tips { get; set; }
    }

    public sealed class NpcPreferencesWikiInfo
    {
        public string Name { get; set; }
        public string WikiPage { get; set; }
        public string[] LoveBiomes { get; set; }
        public string[] LoveNeighbors { get; set; }
        public string[] LikeBiomes { get; set; }
        public string[] LikeNeighbors { get; set; }
        public string[] DislikeBiomes { get; set; }
        public string[] DislikeNeighbors { get; set; }
        public string[] HateBiomes { get; set; }
        public string[] HateNeighbors { get; set; }
    }

    public enum Affection
    {
        Love,
        Like,
        Dislike,
        Hate
    }

    public sealed class PersonalityPreference
    {
        public Affection Level;
        public string Label;
    }

    public sealed class NpcPersonalityEntry
    {
        public List<PersonalityPreference> Biomes = new List<PersonalityPreference>();
        public List<PersonalityPreference> Neighbors = new List<PersonalityPreference>();

        // Marker for NPCs such as the Princess whose full behaviour cannot be expressed as
        // simple biome and neighbour rows.
        public bool HasUniqueBehavior;
    }

    public sealed class ShopItemEntry
    {
        public int ItemId;
        public int Cost;
    }

    public sealed class ShopSellerEntry
    {
        public int NpcId;
        public int Cost;
    }
}
