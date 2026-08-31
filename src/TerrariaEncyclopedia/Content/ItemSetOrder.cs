using System;
using System.Collections.Generic;

namespace TerrariaEncyclopedia.Content
{
    public static class ItemSetOrder
    {
        private static readonly string[] Tiers =
        {
            "Wood", "Copper", "Tin", "Iron", "Lead", "Silver", "Tungsten", "Gold", "Platinum",
            "Meteor", "Shadow", "Necro", "Crimson", "Demonite", "Molten",
            "Cobalt", "Palladium", "Mythril", "Orichalcum", "Adamantite", "Titanium",
            "Hallowed", "Chlorophyte", "Turtle", "Beetle", "Spectre", "Shroomite",
            "Solar", "Vortex", "Nebula", "Stardust"
        };

        private static readonly HashSet<string> Known = new HashSet<string>(Tiers);

        public static int IndexOf(string set)
        {
            if (set == null)
                return int.MaxValue;
            int index = Array.IndexOf(Tiers, set);
            return index >= 0 ? index : int.MaxValue - 1;
        }

        public static string Detect(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            string first = name.Split(' ')[0];
            return Known.Contains(first) ? first : null;
        }
    }
}
