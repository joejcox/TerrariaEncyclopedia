using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;

namespace TerrariaEncyclopedia.Content
{
    public class NpcEntry
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

    /// <summary>
    /// Reads Terraria's currently-loaded NPC definitions dynamically — same approach as
    /// ItemCatalogue. Descriptions come from the game's own Bestiary entries, not text we wrote.
    /// </summary>
    public static class NpcCatalogue
    {
        // FlavorTextBestiaryInfoElement stores its text as a private localization key, not a
        // public string — there is no public accessor for it, so this one field read via
        // reflection is unavoidable (documented per project.md's "prefer direct APIs, but
        // document why when reflection is required").
        private static readonly FieldInfo FlavorKeyField =
            typeof(FlavorTextBestiaryInfoElement).GetField("_key", BindingFlags.NonPublic | BindingFlags.Instance);

        public static List<NpcEntry> Build()
        {
            var list = new List<NpcEntry>();

            for (int i = 1; i < NPCID.Count; i++)
            {
                try
                {
                    var npc = new NPC();
                    npc.SetDefaults(i, default(NPCSpawnParams));

                    string name = npc.FullName;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    list.Add(new NpcEntry
                    {
                        Id = i,
                        Name = name,
                        Life = npc.lifeMax,
                        Defense = npc.defense,
                        Damage = npc.damage,
                        // Plain npc.boss undercounts real bosses — segmented "worm" bosses like
                        // Eater of Worlds don't set it on their segments. NPCID.Sets is Terraria's
                        // own Bestiary using exactly this array for the same "is this a boss"
                        // question, so it's the more authoritative signal.
                        IsBoss = npc.boss || (i < NPCID.Sets.ShouldBeCountedAsBossForBestiary.Length
                            && NPCID.Sets.ShouldBeCountedAsBossForBestiary[i]),
                        IsTownNpc = npc.townNPC,
                        Description = GetDescription(i)
                    });
                }
                catch
                {
                    // Some IDs are unused/reserved and throw on SetDefaults — skip them.
                }
            }

            return list;
        }

        private static string GetDescription(int npcId)
        {
            var entry = Main.BestiaryDB?.FindEntryByNPCID(npcId);
            if (entry == null) return null;

            foreach (var info in entry.Info)
            {
                if (info is FlavorTextBestiaryInfoElement flavor && FlavorKeyField != null)
                {
                    if (FlavorKeyField.GetValue(flavor) is string key)
                        return Language.GetTextValue(key);
                }
            }

            return null;
        }
    }
}
