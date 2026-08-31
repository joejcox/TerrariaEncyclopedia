using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace TerrariaEncyclopedia.Content
{
    /// <summary>
    /// Reads Terraria's real shop contents via Chest.SetupShop(shopIndex) — the same method the
    /// game itself calls when a shop opens. Item identity and cost are live; there's no clean
    /// "Availability" condition data to pull alongside it (SetupShop is one big method with
    /// inline if-branches on the current player's progress, not the tidy condition-object system
    /// item drops use), so that column isn't sourced here.
    ///
    /// Crucially, SetupShop's parameter is NOT the NPC's own type ID — it's a small sequential
    /// "shop index" assigned per-NPC by NPCInteractions.Initialize(), completely unrelated to
    /// NPCID (confirmed via IL disassembly of NPCInteractions::Shop's call sites: e.g.
    /// Demolitionist is NPCID 38 but shop index 4). Passing the raw NPCID instead silently fell
    /// through the switch to a generic/near-empty branch — hence "only 1 item" for every NPC.
    /// A handful of town NPCs (Nurse, Guide, Angler, Tax Collector) never call NPCInteractions.Shop
    /// at all — they don't have a Chest-based shop in vanilla (Nurse/Guide never sell anything;
    /// Angler's "shop" is a quest-reward picker, not a Chest) — those are correctly absent below
    /// rather than guessed at.
    ///
    /// Looked up lazily, one NPC at a time, rather than eagerly for every town NPC up front —
    /// SetupShop can trigger texture loads for items not yet seen this session, and doing that
    /// for ~200 NPCs at once when the panel first opens caused a multi-second stall. One NPC at
    /// a time (only when its detail page is actually opened) is both fast and touches far less
    /// live game state.
    /// </summary>
    public static class NpcShopCatalogue
    {
        private static readonly Dictionary<int, int> ShopIndexByNpcId = new Dictionary<int, int>
        {
            { NPCID.Merchant, 1 }, { NPCID.ArmsDealer, 2 }, { NPCID.Dryad, 3 },
            { NPCID.Demolitionist, 4 }, { NPCID.Clothier, 5 }, { NPCID.GoblinTinkerer, 6 },
            { NPCID.Wizard, 7 }, { NPCID.Mechanic, 8 }, { NPCID.SantaClaus, 9 },
            { NPCID.Truffle, 10 }, { NPCID.Steampunker, 11 }, { NPCID.DyeTrader, 12 },
            { NPCID.PartyGirl, 13 }, { NPCID.Cyborg, 14 }, { NPCID.Painter, 15 },
            { NPCID.WitchDoctor, 16 }, { NPCID.Pirate, 17 }, { NPCID.Stylist, 18 },
            { NPCID.TravellingMerchant, 19 },
            // "Tavernkeep" and "Zoologist" are their in-game display names — the NPCID constants
            // still carry their original dev codenames from when they were added (verified via
            // reflection: no field literally named Tavernkeep/Zoologist exists).
            { NPCID.DD2Bartender, 21 }, { NPCID.Golfer, 22 },
            { NPCID.BestiaryGirl, 23 }, { NPCID.Princess, 24 },
        };

        private static readonly Dictionary<int, List<ShopItemEntry>> Cache = new Dictionary<int, List<ShopItemEntry>>();
        private static readonly List<ShopItemEntry> Empty = new List<ShopItemEntry>();

        public static List<ShopItemEntry> ShopFor(int npcId)
        {
            if (!ShopIndexByNpcId.TryGetValue(npcId, out int shopIndex)) return Empty;
            if (Cache.TryGetValue(npcId, out var cached)) return cached;

            var items = new List<ShopItemEntry>();
            try
            {
                // Chest's only constructor is internal — reuse one of the game's own
                // pre-allocated shop-slot Chests (Main.instance.shop, exactly what Main.OpenShop
                // itself uses) instead of trying to construct a new one.
                var chest = Main.instance.shop[0];
                var originalItems = chest.item;
                try
                {
                    // SetupShop mutates every slot. Give it a private scratch array and restore
                    // the live shop array even when setup or enumeration throws, so browsing the
                    // Encyclopedia cannot alter an already-open vendor UI.
                    var scratchItems = new Item[originalItems.Length];
                    for (int i = 0; i < scratchItems.Length; i++)
                        scratchItems[i] = new Item();
                    chest.item = scratchItems;
                    chest.SetupShop(shopIndex);

                    foreach (var item in chest.item)
                    {
                        if (item != null && item.type > 0)
                            items.Add(new ShopItemEntry { ItemId = item.type, Cost = item.value });
                    }
                }
                finally
                {
                    chest.item = originalItems;
                }
            }
            catch
            {
                // Defensive only — every entry in ShopIndexByNpcId is verified to have a real shop.
            }

            Cache[npcId] = items;
            return items;
        }

        // Reverse index (item -> which NPCs sell it) for the item detail page's "Sold by"
        // section — built lazily on first use by calling ShopFor for the small, fixed set of
        // known shop NPCs (~23), not all ~200 town+enemy NPCs, so it stays cheap.
        private static Dictionary<int, List<ShopSellerEntry>> _sellerIndex;
        private static readonly List<ShopSellerEntry> EmptySellers = new List<ShopSellerEntry>();

        public static List<ShopSellerEntry> SellersOf(int itemId)
        {
            if (_sellerIndex == null)
            {
                _sellerIndex = new Dictionary<int, List<ShopSellerEntry>>();
                foreach (var npcId in ShopIndexByNpcId.Keys)
                {
                    foreach (var item in ShopFor(npcId))
                    {
                        if (!_sellerIndex.TryGetValue(item.ItemId, out var list))
                            _sellerIndex[item.ItemId] = list = new List<ShopSellerEntry>();
                        list.Add(new ShopSellerEntry { NpcId = npcId, Cost = item.Cost });
                    }
                }
            }

            return _sellerIndex.TryGetValue(itemId, out var sellers) ? sellers : EmptySellers;
        }
    }
}
