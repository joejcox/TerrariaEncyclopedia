using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using TerrariaEncyclopedia.Content;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace TerrariaEncyclopedia.UI
{
    /// <summary>
    /// Phase 2: read-only item browser. Search + category tabs + scrollable icon grid +
    /// a details strip for the last-clicked item. No spawning, no writes — pure display.
    /// </summary>
    public class ItemBrowserPanel
    {
        private const int SlotSize = 44;
        private const int SlotOuter = SlotSize - 2;
        private const int IconPad = 4;
        private const int IconSize = SlotOuter - IconPad * 2;
        private const int DetailsHeight = 46;

        // null Category = "All" (rendered as text; there's no single item that reads as "everything").
        // Everything else shows an icon of a representative item, matching how the request wants
        // categories recognized visually rather than by an abbreviated label.
        private static readonly (ItemCategory? Category, string Label, int IconId)[] Tabs =
        {
            (null, "All", 0),
            (ItemCategory.Weapon, "Weapons", ItemID.CopperShortsword),
            (ItemCategory.Armor, "Armor", ItemID.CopperHelmet),
            (ItemCategory.Accessory, "Accessories", ItemID.HermesBoots),
            (ItemCategory.Tool, "Tools", ItemID.CopperPickaxe),
            (ItemCategory.Consumable, "Consumables", ItemID.LesserHealingPotion),
            (ItemCategory.Placeable, "Blocks & Furniture", ItemID.DirtBlock),
            (ItemCategory.Misc, "Miscellaneous", ItemID.Bone),
        };

        private readonly DraggablePanel _panel;
        private readonly TextInput _search = new TextInput("Search items...", 200);
        private readonly ScrollView _scroll = new ScrollView();

        private List<ItemEntry> _all = new List<ItemEntry>();
        private List<ItemEntry> _filtered = new List<ItemEntry>();
        private int _activeTab;
        private ItemEntry _selected;

        public ItemBrowserPanel()
        {
            _panel = new DraggablePanel("terraria-encyclopedia", "Terraria Encyclopedia", 520, 480);
            // Same UIScale/scissor-rect mismatch found in Phase 1 — see Mod.cs history.
            _panel.ClipContent = false;
            _panel.RegisterDrawCallback(OnDraw);
            _panel.OnClose = () => _search.Unfocus();
            _search.KeyBlockId = "terraria-encyclopedia";
        }

        public void Toggle()
        {
            if (_panel.IsOpen)
            {
                _panel.Close();
                return;
            }

            if (_all.Count == 0)
            {
                _all = ItemCatalogue.Build();
                Filter();
            }

            _panel.Open();
        }

        public void Close() => _panel.Close();

        public void Update()
        {
            if (!_panel.IsOpen) return;
            _search.Update();
        }

        public void Unload()
        {
            _panel.UnregisterDrawCallback();
            _search.Unfocus();
        }

        private void Filter()
        {
            IEnumerable<ItemEntry> source = _all;

            if (_activeTab > 0)
            {
                var category = (ItemCategory)(_activeTab - 1);
                source = source.Where(e => e.Category == category);
            }

            string search = _search.Text;
            if (!string.IsNullOrEmpty(search))
            {
                string lower = search.ToLowerInvariant();
                bool isIdSearch = int.TryParse(search, out int searchId);
                source = source.Where(e => (isIdSearch && e.Id == searchId) || e.Name.ToLowerInvariant().Contains(lower));
            }

            _filtered = source.ToList();
            _scroll.ResetScroll();
            _panel.Title = $"Terraria Encyclopedia ({_filtered.Count}/{_all.Count})";
        }

        private void OnDraw()
        {
            if (!_panel.BeginDraw()) return;
            try
            {
                var s = new StackLayout(_panel.ContentX, _panel.ContentY, _panel.ContentWidth);

                int searchY = s.Advance(28);
                _search.Draw(s.X, searchY, 220, 26);
                if (_search.HasChanged) Filter();

                int tabsY = s.Advance(34);
                DrawCategoryTabs(s.X, tabsY, s.Width);

                s.Advance(6);

                int gridX = s.X;
                int gridY = s.CurrentY;
                int gridWidth = s.Width;
                int gridHeight = _panel.ContentHeight - s.TotalHeight - DetailsHeight - 6;

                DrawGrid(gridX, gridY, gridWidth, gridHeight);
                DrawDetails(gridX, gridY + gridHeight + 6, gridWidth);
            }
            finally
            {
                _panel.EndDraw();
            }
        }

        private void DrawCategoryTabs(int x, int y, int width)
        {
            int tabWidth = width / Tabs.Length;

            for (int i = 0; i < Tabs.Length; i++)
            {
                int tabX = x + i * tabWidth;
                int tabW = (i == Tabs.Length - 1) ? (width - i * tabWidth) : tabWidth - 2;
                bool isActive = _activeTab == i;
                bool isHover = WidgetInput.IsMouseOver(tabX, y, tabW, 30);

                UIRenderer.DrawRect(tabX, y, tabW, 30,
                    isActive ? UIColors.ItemActiveBg : (isHover ? UIColors.SectionBg : UIColors.InputBg));
                if (isActive)
                    UIRenderer.DrawRect(tabX, y + 28, tabW, 2, UIColors.Accent);

                var tab = Tabs[i];
                if (tab.IconId > 0)
                {
                    UIRenderer.DrawItem(tab.IconId, tabX + (tabW - 24) / 2, y + 3, 24, 24);
                }
                else
                {
                    int textWidth = TextUtil.MeasureWidth(tab.Label);
                    UIRenderer.DrawText(tab.Label, tabX + (tabW - textWidth) / 2, y + 8,
                        isActive ? UIColors.AccentText : UIColors.TextDim);
                }

                if (isHover)
                {
                    Tooltip.Set(tab.Label);
                    if (WidgetInput.MouseLeftClick)
                    {
                        WidgetInput.ConsumeClick();
                        if (_activeTab != i)
                        {
                            _activeTab = i;
                            Filter();
                        }
                    }
                }
            }
        }

        private void DrawGrid(int gridX, int gridY, int gridWidth, int gridHeight)
        {
            int gridColumns = Math.Max(1, _scroll.ContentWidth > 0 ? _scroll.ContentWidth / SlotSize : gridWidth / SlotSize);
            int totalRows = (_filtered.Count + gridColumns - 1) / gridColumns;
            int totalContentHeight = totalRows * SlotSize;

            _scroll.Begin(gridX, gridY, gridWidth, gridHeight, totalContentHeight);

            int contentWidth = _scroll.ContentWidth;
            gridColumns = Math.Max(1, contentWidth / SlotSize);

            int visibleRows = gridHeight / SlotSize;
            int startRow = _scroll.ScrollOffset / SlotSize;
            int startIndex = startRow * gridColumns;

            for (int i = 0; i < (visibleRows + 2) * gridColumns && startIndex + i < _filtered.Count; i++)
            {
                int itemIdx = startIndex + i;
                int row = i / gridColumns;
                int col = i % gridColumns;

                int itemContentY = (startRow + row) * SlotSize;
                if (!_scroll.IsVisible(itemContentY, SlotSize)) continue;

                int slotX = gridX + col * SlotSize;
                int slotY = _scroll.ContentY + itemContentY;

                var entry = _filtered[itemIdx];
                bool inScrollBounds = WidgetInput.IsMouseOver(gridX, gridY, gridWidth, gridHeight);
                bool isHover = inScrollBounds && WidgetInput.IsMouseOver(slotX, slotY, SlotOuter, SlotOuter);

                UIRenderer.DrawRect(slotX, slotY, SlotOuter, SlotOuter, isHover ? UIColors.InputFocusBg : UIColors.ItemBg);
                if (isHover)
                    UIRenderer.DrawRectOutline(slotX, slotY, SlotOuter, SlotOuter, UIColors.Accent, 1);

                UIRenderer.DrawItem(entry.Id, slotX + IconPad, slotY + IconPad, IconSize, IconSize);

                if (isHover)
                {
                    ItemTooltip.Set(entry.Id);
                    if (WidgetInput.MouseLeftClick)
                    {
                        _selected = entry;
                        WidgetInput.ConsumeClick();
                    }
                }
            }

            _scroll.End();

            if (_filtered.Count == 0)
                UIRenderer.DrawText("No items found", gridX + 5, gridY + 10, UIColors.Warning);
        }

        private void DrawDetails(int x, int y, int width)
        {
            UIRenderer.DrawRect(x, y, width, DetailsHeight, UIColors.SectionBg);

            if (_selected == null)
            {
                UIRenderer.DrawText("Click an item to see details", x + 8, y + 14, UIColors.TextDim);
                return;
            }

            UIRenderer.DrawItem(_selected.Id, x + 6, y + 4, 38, 38);
            UIRenderer.DrawText(_selected.Name, x + 50, y + 4, UIColors.TextTitle);
            UIRenderer.DrawTextSmall(
                $"ID: {_selected.Id}    Category: {_selected.Category}    Max stack: {_selected.MaxStack}    Rarity: {_selected.Rarity}",
                x + 50, y + 24, UIColors.TextDim);
        }
    }
}
