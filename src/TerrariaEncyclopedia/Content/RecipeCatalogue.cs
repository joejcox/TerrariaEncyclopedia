using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace TerrariaEncyclopedia.Content
{
    public class RecipeIngredient
    {
        public int ItemId;
        public int Stack;
    }

    public class RecipeInfo
    {
        public int ResultItemId;
        public int ResultStack;
        public List<RecipeIngredient> Ingredients = new List<RecipeIngredient>();
        public List<int> StationTileIds = new List<int>();
    }

    /// <summary>
    /// Reads Terraria's currently-loaded recipe list dynamically from Main.recipe — never
    /// hardcoded. Built once and indexed both ways: what makes an item, and what an item is
    /// used to make (reverse lookup), per project.md's Phase 3 scope.
    /// </summary>
    public static class RecipeCatalogue
    {
        private static List<RecipeInfo> _all;
        private static Dictionary<int, List<RecipeInfo>> _byResult;
        private static Dictionary<int, List<RecipeInfo>> _byIngredient;
        private static Dictionary<int, string> _stationNameByTile;

        public static void EnsureBuilt(List<ItemEntry> catalogue)
        {
            if (_all != null) return;

            _all = new List<RecipeInfo>();
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                var r = Main.recipe[i];
                if (r?.createItem == null || r.createItem.type <= 0) continue;

                var info = new RecipeInfo
                {
                    ResultItemId = r.createItem.type,
                    ResultStack = r.createItem.stack
                };

                foreach (var reqItem in r.requiredItem)
                {
                    if (reqItem == null || reqItem.type <= 0) break;
                    info.Ingredients.Add(new RecipeIngredient { ItemId = reqItem.type, Stack = reqItem.stack });
                }

                // Modern Terraria's Recipe.requiredTile is a single int, not an array —
                // one required station per recipe.
                if (r.requiredTile >= 0)
                    info.StationTileIds.Add(r.requiredTile);

                _all.Add(info);
            }

            _byResult = _all.GroupBy(r => r.ResultItemId).ToDictionary(g => g.Key, g => g.ToList());

            _byIngredient = new Dictionary<int, List<RecipeInfo>>();
            foreach (var recipe in _all)
            {
                foreach (var ing in recipe.Ingredients)
                {
                    if (!_byIngredient.TryGetValue(ing.ItemId, out var list))
                        _byIngredient[ing.ItemId] = list = new List<RecipeInfo>();
                    list.Add(recipe);
                }
            }

            // Best-effort tile -> display name: first item found that places this tile.
            _stationNameByTile = new Dictionary<int, string>();
            foreach (var entry in catalogue)
            {
                var item = new Item();
                item.SetDefaults(entry.Id);
                if (item.createTile >= 0 && !_stationNameByTile.ContainsKey(item.createTile))
                    _stationNameByTile[item.createTile] = entry.Name;
            }
        }

        public static List<RecipeInfo> RecipesFor(int itemId) =>
            _byResult.TryGetValue(itemId, out var list) ? list : EmptyRecipes;

        public static List<RecipeInfo> UsedIn(int itemId) =>
            _byIngredient.TryGetValue(itemId, out var list) ? list : EmptyRecipes;

        public static string StationName(int tileId) =>
            _stationNameByTile.TryGetValue(tileId, out var name) ? name : $"Tile #{tileId}";

        private static readonly List<RecipeInfo> EmptyRecipes = new List<RecipeInfo>();
    }
}
