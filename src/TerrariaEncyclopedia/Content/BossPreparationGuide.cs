using System;
using System.Collections.Generic;

namespace TerrariaEncyclopedia.Content
{
    /// <summary>Read-only, concise goals to help prepare for each vanilla boss.</summary>
    public static class BossPreparationGuide
    {
        private static readonly Dictionary<string, string> Baselines =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["King Slime"] = "Craft a Slime Crown or prepare for Slime Rain; build a simple platform arena and bring healing potions.",
                ["Eye of Cthulhu"] = "Craft a Suspicious Looking Eye; prepare a long platform arena, mobility, and healing for the night fight.",
                ["Eater of Worlds / Brain of Cthulhu"] = "Craft Worm Food in Corruption or Bloody Spine in Crimson; prepare a multi-level evil-biome arena and piercing or area damage.",
                ["Queen Bee"] = "Find a Bee Larva in the Underground Jungle or craft an Abeemination; clear an arena and bring mobility plus healing.",
                ["Skeletron"] = "Speak to the Old Man at the Dungeon entrance at night; prepare a wide surface arena and enough mobility to dodge the hands.",
                ["Deerclops"] = "Use a Deer Thing in the Snow biome or wait for its conditions; prepare open space, warmth/defence buffs, and healing.",
                ["Wall of Flesh"] = "Prepare a long Underworld bridge and a Guide Voodoo Doll; bring strong sustained damage, mobility, and healing before entering Hardmode.",
                ["Queen Slime"] = "Use a Gelatin Crystal in the Underground Hallow; prepare a large arena with platforms and reliable aerial mobility.",
                ["The Destroyer"] = "Craft a Mechanical Worm and use it at night; prepare a long arena, piercing or area damage, and a way to handle probes.",
                ["The Twins"] = "Craft a Mechanical Eye and use it at night; prepare a long arena, strong mobility, and sustained single-target damage.",
                ["Skeletron Prime"] = "Craft a Mechanical Skull and use it at night; prepare a wide arena, mobility, and answers for several attack patterns.",
                ["Plantera"] = "Locate a Plantera's Bulb after all mechanical bosses; clear a large Underground Jungle arena before breaking it.",
                ["Golem"] = "Reach the Jungle Temple with a Lihzahrd Power Cell; clear the altar room and bring strong close-range mobility.",
                ["Duke Fishron"] = "Catch a Truffle Worm and fish in the Ocean; prepare a long multi-level ocean arena and fast mobility.",
                ["Empress of Light"] = "Catch or find a Prismatic Lacewing in the Surface Hallow after Plantera; prepare a clear arena and reliable dodging.",
                ["Lunatic Cultist"] = "Defeat the Cultists at the Dungeon after Golem; clear the entrance and bring adaptable damage for summoned threats.",
                ["Moon Lord"] = "Defeat the four Celestial Pillars or use a Celestial Sigil after Golem; prepare a large arena, healing access, and sustained endgame damage."
            };

        private static readonly Dictionary<PlayerClass, string> ClassFocus =
            new Dictionary<PlayerClass, string>
            {
                [PlayerClass.Melee] = "Melee focus: prioritise the best available melee weapon, matching armour, defence, and an accessory that improves survivability or mobility.",
                [PlayerClass.Ranged] = "Ranged focus: prioritise a high-DPS bow or gun, stock the correct ammunition, use matching armour, and keep distance with mobility.",
                [PlayerClass.Magic] = "Magic focus: prioritise a strong spell with a useful mana cost, matching armour, Mana Potions, and accessories that support mana or mobility.",
                [PlayerClass.Summoner] = "Summoner focus: maximise minion slots and summon damage, use the strongest whip or sentry available, and keep moving with mobility accessories.",
                [PlayerClass.Hybrid] = "Hybrid focus: choose one primary damage class for this fight, then keep the strongest shared mobility, defence, and healing tools.",
                [PlayerClass.Unknown] = "General focus: bring reliable damage, mobility, healing, useful buffs, and an arena suited to the summon conditions."
            };

        private static readonly HashSet<string> EarlyBosses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "King Slime", "Eye of Cthulhu", "Eater of Worlds / Brain of Cthulhu", "Queen Bee", "Skeletron", "Deerclops"
        };

        private static readonly HashSet<string> MechanicalBosses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "The Destroyer", "The Twins", "Skeletron Prime"
        };

        private static string ClassTargets(string bossName, PlayerClass playerClass)
        {
            string tier = EarlyBosses.Contains(bossName) ? "early" :
                string.Equals(bossName, "Wall of Flesh", StringComparison.OrdinalIgnoreCase) ? "wall" :
                MechanicalBosses.Contains(bossName) ? "mechanical" :
                string.Equals(bossName, "Queen Slime", StringComparison.OrdinalIgnoreCase) ? "early-hardmode" :
                string.Equals(bossName, "Plantera", StringComparison.OrdinalIgnoreCase) ? "post-mechanical" :
                (string.Equals(bossName, "Golem", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(bossName, "Duke Fishron", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(bossName, "Empress of Light", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(bossName, "Lunatic Cultist", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(bossName, "Moon Lord", StringComparison.OrdinalIgnoreCase)
                    ? "late-hardmode" : "unknown");

            if (tier == "unknown")
                return "Aim for the strongest available armour and weapon for your class, plus mobility, healing, and useful buffs.";

            switch (playerClass)
            {
                case PlayerClass.Melee:
                    if (tier == "early") return "Aim for Gold/Platinum or evil-biome armour, a strong sword such as the Blade of Grass or ball yoyo, Hermes Boots, and Ironskin/Regeneration Potions.";
                    if (tier == "wall") return "Aim for Molten armour and a strong Underworld weapon such as the Sunfury or Night's Edge, with Obsidian Skin and Ironskin Potions.";
                    if (tier == "early-hardmode") return "Aim for Adamantite/Titanium armour or a strong class set, a fast weapon, wings, and Ironskin/Swiftness Potions.";
                    if (tier == "mechanical") return "Aim for Adamantite/Titanium armour, wings, a high-DPS weapon such as Shadowflame Knife or Fetid Baghnakhs, and a long arena.";
                    if (tier == "post-mechanical") return "Aim for Turtle armour or a comparable upgrade, a strong yoyo or sword, wings, and endurance/ironskin buffs.";
                    return "Aim for the strongest available melee armour and weapon upgrade, wings or equivalent mobility, and Endurance, Wrath/Rage, and Lifeforce Potions.";
                case PlayerClass.Ranged:
                    if (tier == "early") return "Aim for Fossil or Necro armour, a bow or gun with suitable ammunition, Hermes Boots, and Archery/Ironskin Potions.";
                    if (tier == "wall") return "Aim for Necro or Fossil armour, the Hellwing Bow or Beenade with ample ammunition, Obsidian Skin, and a long bridge.";
                    if (tier == "early-hardmode") return "Aim for Adamantite/Titanium armour, a repeater or gun, wings, and a stocked ammunition supply.";
                    if (tier == "mechanical") return "Aim for Adamantite/Titanium armour, a Daedalus Stormbow or equivalent, Holy Arrows or matching ammunition, and wings.";
                    if (tier == "post-mechanical") return "Aim for Shroomite armour or a comparable upgrade, a Chlorophyte weapon, wings, and Endurance/Ammo Reservation Potions.";
                    return "Aim for the strongest available ranged armour and weapon, matching ammunition, wings, and Archery, Wrath/Rage, and Lifeforce Potions.";
                case PlayerClass.Magic:
                    if (tier == "early") return "Aim for Jungle or Meteor armour, a reliable spell such as Water Bolt or Space Gun, Mana Potions, Hermes Boots, and Magic Power Potions.";
                    if (tier == "wall") return "Aim for Meteor or Jungle upgrades such as the Demon Scythe or Flower of Fire, plenty of Mana Potions, and a long bridge.";
                    if (tier == "early-hardmode") return "Aim for Adamantite/Titanium or Crystal Assassin-compatible upgrades, wings, Mana Flower, and Magic Power/Regeneration Potions.";
                    if (tier == "mechanical") return "Aim for a strong hardmode magic set, a piercing spell such as Crystal Storm or Meteor Staff, wings, and Mana Potions.";
                    if (tier == "post-mechanical") return "Aim for Spectre armour or a comparable upgrade, a high-DPS spell, Mana Flower, wings, and Magic Power/Lifeforce Potions.";
                    return "Aim for the strongest available magic armour and spell, Mana Flower, wings, and Magic Power, Wrath/Rage, and Lifeforce Potions.";
                case PlayerClass.Summoner:
                    if (tier == "early") return "Aim for Flinx or Bee armour, the best available minion and whip, extra minion slots, Hermes Boots, and Summoning Potions.";
                    if (tier == "wall") return "Aim for Bee or Obsidian armour, a strong whip such as Snapthorn, maximum minion slots, and a long bridge.";
                    if (tier == "early-hardmode") return "Aim for Spider armour, a hardmode minion or sentry, wings, a whip upgrade, and Summoning/Endurance Potions.";
                    if (tier == "mechanical") return "Aim for Spider or Forbidden armour, a high-DPS minion, a strong whip, wings, and a Bewitching Table/Summoning Potion.";
                    if (tier == "post-mechanical") return "Aim for Spooky or Tiki armour, a powerful minion and whip, wings, and Summoning, Wrath/Rage, and Lifeforce Potions.";
                    return "Aim for the strongest available summon armour, minion, whip, and sentry combination, plus wings and Summoning/Lifeforce Potions.";
                case PlayerClass.Hybrid:
                    return "Choose one primary damage class for this fight and aim for its strongest available armour and weapon upgrade, plus wings, mobility, healing, and useful buffs.";
                default:
                    return ClassFocus[PlayerClass.Unknown];
            }
        }

        public static string For(string bossName, PlayerClass playerClass)
        {
            bossName = CanonicalName(bossName);
            string baseline;
            if (string.IsNullOrWhiteSpace(bossName) || !Baselines.TryGetValue(bossName, out baseline))
                baseline = "Prepare the summon item, a clear arena, mobility, healing, and useful buffs for this fight.";

            string focus;
            if (!ClassFocus.TryGetValue(playerClass, out focus))
                focus = ClassFocus[PlayerClass.Unknown];

            return focus + "\nAim for: " + ClassTargets(bossName, playerClass) + "\n" + baseline;
        }

        private static string CanonicalName(string bossName)
        {
            if (string.Equals(bossName, "Eater of Worlds", StringComparison.OrdinalIgnoreCase)
                || string.Equals(bossName, "Brain of Cthulhu", StringComparison.OrdinalIgnoreCase))
                return "Eater of Worlds / Brain of Cthulhu";
            if (string.Equals(bossName, "Retinazer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(bossName, "Spazmatism", StringComparison.OrdinalIgnoreCase))
                return "The Twins";
            return bossName;
        }
    }
}
