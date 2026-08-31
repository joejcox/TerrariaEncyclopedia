using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrariaEncyclopedia.Content
{
    public enum ItemSortMode
    {
        NameAscending,
        NameDescending,
        RarityAscending,
        RarityDescending,
        Oldest,
        Newest,
        SetGrouped
    }

    public enum NpcFilter
    {
        All,
        Town,
        Enemies,
        Bosses
    }

    public enum NpcSortMode
    {
        NameAscending,
        NameDescending,
        LifeAscending,
        LifeDescending
    }

    public static class EncyclopediaQueries
    {
        public static IReadOnlyList<ItemEntry> FilterItems(
            IEnumerable<ItemEntry> items,
            ItemCategory? category,
            bool hardmodeOnly,
            string search,
            ItemSortMode sort,
            Func<string, bool> isHardmode)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (isHardmode == null)
                throw new ArgumentNullException(nameof(isHardmode));

            IEnumerable<ItemEntry> result = items;
            if (category.HasValue)
                result = result.Where(item => item.Category == category.Value);
            if (hardmodeOnly)
                result = result.Where(item => isHardmode(item.Name));

            if (!string.IsNullOrEmpty(search))
            {
                int id;
                bool idSearch = int.TryParse(search, out id);
                result = result.Where(item =>
                    (idSearch && item.Id == id)
                    || item.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            switch (sort)
            {
                case ItemSortMode.NameDescending:
                    result = result.OrderByDescending(item => item.Name);
                    break;
                case ItemSortMode.RarityAscending:
                    result = result.OrderBy(item => item.Rarity).ThenBy(item => item.Name);
                    break;
                case ItemSortMode.RarityDescending:
                    result = result.OrderByDescending(item => item.Rarity).ThenBy(item => item.Name);
                    break;
                case ItemSortMode.Oldest:
                    result = result.OrderBy(item => item.Id);
                    break;
                case ItemSortMode.Newest:
                    result = result.OrderByDescending(item => item.Id);
                    break;
                case ItemSortMode.SetGrouped:
                    result = result.OrderBy(item => ItemSetOrder.IndexOf(item.Set))
                        .ThenBy(item => item.Set)
                        .ThenBy(item => item.Name);
                    break;
                default:
                    result = result.OrderBy(item => item.Name);
                    break;
            }

            return result.ToList();
        }

        public static IReadOnlyList<NpcEntry> FilterNpcs(
            IEnumerable<NpcEntry> npcs,
            NpcFilter filter,
            bool hardmodeOnly,
            string search,
            NpcSortMode sort,
            Func<string, bool> isHardmode)
        {
            if (npcs == null)
                throw new ArgumentNullException(nameof(npcs));
            if (isHardmode == null)
                throw new ArgumentNullException(nameof(isHardmode));

            IEnumerable<NpcEntry> result = npcs;
            switch (filter)
            {
                case NpcFilter.Town:
                    result = result.Where(npc => npc.IsTownNpc);
                    break;
                case NpcFilter.Enemies:
                    result = result.Where(npc => !npc.IsTownNpc && !npc.IsBoss);
                    break;
                case NpcFilter.Bosses:
                    result = result.Where(npc => npc.IsBoss);
                    break;
            }

            if (hardmodeOnly)
                result = result.Where(npc => isHardmode(npc.Name));
            if (!string.IsNullOrEmpty(search))
            {
                int id;
                bool idSearch = int.TryParse(search, out id);
                result = result.Where(npc =>
                    (idSearch && npc.Id == id)
                    || npc.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            switch (sort)
            {
                case NpcSortMode.NameDescending:
                    result = result.OrderByDescending(npc => npc.Name);
                    break;
                case NpcSortMode.LifeAscending:
                    result = result.OrderBy(npc => npc.Life).ThenBy(npc => npc.Name);
                    break;
                case NpcSortMode.LifeDescending:
                    result = result.OrderByDescending(npc => npc.Life).ThenBy(npc => npc.Name);
                    break;
                default:
                    result = result.OrderBy(npc => npc.Name);
                    break;
            }

            return result.ToList();
        }

        public static IReadOnlyList<EventWikiInfo> FilterEvents(
            IEnumerable<EventWikiInfo> events,
            string search)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            IEnumerable<EventWikiInfo> result = events;
            if (!string.IsNullOrEmpty(search))
            {
                result = result.Where(item => item.Name != null
                    && item.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return result.OrderBy(item => item.Name).ToList();
        }

        public static int DisplayedCriticalChance(
            int itemCriticalChance,
            bool melee,
            bool ranged,
            bool magic,
            bool summon)
        {
            bool receivesPlayerBaseChance = !summon && (melee || ranged || magic);
            return itemCriticalChance + (receivesPlayerBaseChance ? 4 : 0);
        }
    }
}
