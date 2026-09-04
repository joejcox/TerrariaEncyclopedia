using System.Collections.Generic;

namespace TerrariaEncyclopedia.Content
{
    /// <summary>
    /// Holds the conditional shop inventory captured during one Encyclopedia browsing session.
    /// Kept separate from Terraria's live shop reader so reset behaviour is deterministic and
    /// testable without starting the game.
    /// </summary>
    internal sealed class ShopCatalogueSnapshot
    {
        private readonly Dictionary<int, List<ShopItemEntry>> shops =
            new Dictionary<int, List<ShopItemEntry>>();
        private readonly List<ShopSellerEntry> emptySellers = new List<ShopSellerEntry>();
        private Dictionary<int, List<ShopSellerEntry>> sellerIndex;

        public bool HasSellerIndex => sellerIndex != null;

        public bool TryGetShop(int npcId, out List<ShopItemEntry> items) =>
            shops.TryGetValue(npcId, out items);

        public void StoreShop(int npcId, List<ShopItemEntry> items)
        {
            shops[npcId] = items;
            sellerIndex = null;
        }

        public void InvalidateShop(int npcId)
        {
            if (shops.Remove(npcId))
                sellerIndex = null;
        }

        public void BuildSellerIndex(IEnumerable<int> npcIds)
        {
            var index = new Dictionary<int, List<ShopSellerEntry>>();
            foreach (int npcId in npcIds)
            {
                if (!shops.TryGetValue(npcId, out var items))
                    continue;

                foreach (var item in items)
                {
                    if (!index.TryGetValue(item.ItemId, out var sellers))
                        index[item.ItemId] = sellers = new List<ShopSellerEntry>();
                    sellers.Add(new ShopSellerEntry { NpcId = npcId, Cost = item.Cost });
                }
            }

            sellerIndex = index;
        }

        public List<ShopSellerEntry> SellersOf(int itemId)
        {
            if (sellerIndex != null && sellerIndex.TryGetValue(itemId, out var sellers))
                return sellers;

            return emptySellers;
        }

        public void Reset()
        {
            shops.Clear();
            sellerIndex = null;
        }
    }
}
