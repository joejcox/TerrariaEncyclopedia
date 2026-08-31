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
    /// Phase 2: read-only item browser (search, category tabs, icon grid).
    /// Phase 3: clicking an item opens a full detail page (stats, recipe, "used in"),
    /// with ingredients/uses clickable to navigate between items, and Back/Home
    /// buttons to return. No spawning, no writes — pure display.
    /// </summary>
    public class ItemBrowserPanel
    {
        private enum ViewMode { Grid, Detail }

        private enum SortMode { NameAsc, NameDesc, RarityAsc, RarityDesc, Oldest, Newest, SetGrouped }

        private static readonly (SortMode Mode, string Label)[] SortModes =
        {
            (SortMode.NameAsc, "A-Z"),
            (SortMode.NameDesc, "Z-A"),
            (SortMode.RarityAsc, "Rarity: Low-High"),
            (SortMode.RarityDesc, "Rarity: High-Low"),
            // "Oldest"/"Newest" approximate release order using item ID — Terraria assigns IDs
            // roughly in the order items were added, though old removed/reused IDs mean it's not exact.
            (SortMode.Oldest, "Oldest First"),
            (SortMode.Newest, "Newest First"),
            (SortMode.SetGrouped, "By Set"),
        };

        private const int SlotSize = 44;
        private const int SlotOuter = SlotSize - 2;
        private const int IconPad = 4;
        private const int IconSize = SlotOuter - IconPad * 2;

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
        private readonly ScrollView _detailScroll = new ScrollView();

        private List<ItemEntry> _all = new List<ItemEntry>();
        private Dictionary<int, ItemEntry> _byId = new Dictionary<int, ItemEntry>();
        private List<ItemEntry> _filtered = new List<ItemEntry>();
        private int _activeTab;
        private int _sortModeIndex;
        private bool _sortDropdownOpen;
        private int _sortButtonX, _sortButtonY, _sortButtonWidth;

        private ViewMode _view = ViewMode.Grid;
        private ItemEntry _detailItem;
        private readonly Stack<int> _history = new Stack<int>();

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
                _byId = _all.ToDictionary(e => e.Id);
                RecipeCatalogue.EnsureBuilt(_all);
                Filter();
            }

            GoHome();
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

            _filtered = ApplySort(source).ToList();
            _scroll.ResetScroll();
            _panel.Title = $"Terraria Encyclopedia ({_filtered.Count}/{_all.Count})";
        }

        private IEnumerable<ItemEntry> ApplySort(IEnumerable<ItemEntry> source)
        {
            switch (SortModes[_sortModeIndex].Mode)
            {
                case SortMode.NameDesc: return source.OrderByDescending(e => e.Name);
                case SortMode.RarityAsc: return source.OrderBy(e => e.Rarity).ThenBy(e => e.Name);
                case SortMode.RarityDesc: return source.OrderByDescending(e => e.Rarity).ThenBy(e => e.Name);
                case SortMode.Oldest: return source.OrderBy(e => e.Id);
                case SortMode.Newest: return source.OrderByDescending(e => e.Id);
                case SortMode.SetGrouped:
                    return source.OrderBy(e => ItemCatalogue.TierIndex(e.Set)).ThenBy(e => e.Set).ThenBy(e => e.Name);
                default: return source.OrderBy(e => e.Name); // NameAsc
            }
        }

        private void GoHome()
        {
            _history.Clear();
            _detailItem = null;
            _view = ViewMode.Grid;
        }

        private void OpenDetail(ItemEntry entry)
        {
            if (_view == ViewMode.Detail && _detailItem != null)
                _history.Push(_detailItem.Id);

            _detailItem = entry;
            _view = ViewMode.Detail;
            _detailScroll.ResetScroll();
        }

        private void GoBack()
        {
            if (_history.Count == 0)
            {
                GoHome();
                return;
            }

            int prevId = _history.Pop();
            _byId.TryGetValue(prevId, out _detailItem);
            _detailScroll.ResetScroll();
        }

        private void OnDraw()
        {
            if (!_panel.BeginDraw()) return;
            try
            {
                if (_view == ViewMode.Grid)
                    DrawGridView();
                else
                    DrawDetailView();
            }
            finally
            {
                _panel.EndDraw();
            }
        }

        private void DrawGridView()
        {
            var s = new StackLayout(_panel.ContentX, _panel.ContentY, _panel.ContentWidth);

            int searchY = s.Advance(28);
            _search.Draw(s.X, searchY, 220, 26);
            if (_search.HasChanged) Filter();

            DrawSortButton(s.X + 230, searchY - 1, s.Width - 230);

            int tabsY = s.Advance(34);
            // While the sort dropdown is open, everything below it must ignore hover/click —
            // otherwise a click meant to close/select in the dropdown could fall through onto
            // whatever grid item or tab happens to be underneath it.
            bool interactive = !_sortDropdownOpen;
            DrawCategoryTabs(s.X, tabsY, s.Width, interactive);

            s.Advance(6);

            int gridX = s.X;
            int gridY = s.CurrentY;
            int gridWidth = s.Width;
            int gridHeight = _panel.ContentHeight - s.TotalHeight;

            DrawGrid(gridX, gridY, gridWidth, gridHeight, interactive);

            // Drawn last so it renders on top of the grid/tabs.
            if (_sortDropdownOpen) DrawSortDropdown();
        }

        private void DrawSortButton(int x, int y, int width)
        {
            _sortButtonX = x;
            _sortButtonY = y;
            _sortButtonWidth = width;

            string label = "Sort: " + SortModes[_sortModeIndex].Label + (_sortDropdownOpen ? "  ^" : "  v");
            bool isHover = WidgetInput.IsMouseOver(x, y, width, 28);

            UIRenderer.DrawRect(x, y, width, 28, isHover || _sortDropdownOpen ? UIColors.SectionBg : UIColors.InputBg);
            UIRenderer.DrawText(label, x + 6, y + 6, UIColors.Text);

            if (isHover)
            {
                Tooltip.Set("Choose a sort order");
                if (WidgetInput.MouseLeftClick)
                {
                    WidgetInput.ConsumeClick();
                    _sortDropdownOpen = !_sortDropdownOpen;
                }
            }
        }

        private void DrawSortDropdown()
        {
            int rowHeight = 26;
            int listY = _sortButtonY + 28;
            int listHeight = SortModes.Length * rowHeight;

            UIRenderer.DrawRect(_sortButtonX, listY, _sortButtonWidth, listHeight, UIColors.PanelBg);
            UIRenderer.DrawRectOutline(_sortButtonX, listY, _sortButtonWidth, listHeight, UIColors.Border);

            for (int i = 0; i < SortModes.Length; i++)
            {
                int rowY = listY + i * rowHeight;
                bool isActive = i == _sortModeIndex;
                bool isHover = WidgetInput.IsMouseOver(_sortButtonX, rowY, _sortButtonWidth, rowHeight);

                if (isActive || isHover)
                    UIRenderer.DrawRect(_sortButtonX, rowY, _sortButtonWidth, rowHeight,
                        isActive ? UIColors.ItemActiveBg : UIColors.SectionBg);

                UIRenderer.DrawText(SortModes[i].Label, _sortButtonX + 6, rowY + 5,
                    isActive ? UIColors.AccentText : UIColors.Text);

                if (isHover && WidgetInput.MouseLeftClick)
                {
                    WidgetInput.ConsumeClick();
                    _sortModeIndex = i;
                    _sortDropdownOpen = false;
                    Filter();
                }
            }

            // Click anywhere outside the button or the list closes it without changing anything.
            bool overButton = WidgetInput.IsMouseOver(_sortButtonX, _sortButtonY, _sortButtonWidth, 28);
            bool overList = WidgetInput.IsMouseOver(_sortButtonX, listY, _sortButtonWidth, listHeight);
            if (!overButton && !overList && WidgetInput.MouseLeftClick)
            {
                WidgetInput.ConsumeClick();
                _sortDropdownOpen = false;
            }
        }

        private void DrawCategoryTabs(int x, int y, int width, bool interactive)
        {
            int tabWidth = width / Tabs.Length;

            for (int i = 0; i < Tabs.Length; i++)
            {
                int tabX = x + i * tabWidth;
                int tabW = (i == Tabs.Length - 1) ? (width - i * tabWidth) : tabWidth - 2;
                bool isActive = _activeTab == i;
                bool isHover = interactive && WidgetInput.IsMouseOver(tabX, y, tabW, 30);

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

        private void DrawGrid(int gridX, int gridY, int gridWidth, int gridHeight, bool interactive)
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
                bool inScrollBounds = interactive && WidgetInput.IsMouseOver(gridX, gridY, gridWidth, gridHeight);
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
                        OpenDetail(entry);
                        WidgetInput.ConsumeClick();
                    }
                }
            }

            _scroll.End();

            if (_filtered.Count == 0)
                UIRenderer.DrawText("No items found", gridX + 5, gridY + 10, UIColors.Warning);
        }

        // ── Detail page ──────────────────────────────────────────────────────

        private const int NavBarHeight = 30;
        private const int RowHeight = 34;

        private void DrawDetailView()
        {
            var entry = _detailItem;
            if (entry == null) { GoHome(); return; }

            int x = _panel.ContentX;
            int y = _panel.ContentY;
            int width = _panel.ContentWidth;

            DrawNavBar(x, y, width);
            y += NavBarHeight + 6;

            // Header: icon, name, category
            UIRenderer.DrawItem(entry.Id, x, y, 48, 48);
            UIRenderer.DrawText(entry.Name, x + 58, y, UIColors.TextTitle);
            UIRenderer.DrawText($"{entry.Category}  •  ID {entry.Id}", x + 58, y + 24, UIColors.TextDim);
            y += 56;

            // Same full tooltip Terraria itself would show on hover (damage, use time, knockback,
            // defense, tags, flavor text, etc.) — embedded inline instead of only on hover.
            int tooltipHeight = ItemTooltip.DrawAt(entry.Id, x, y, width);
            y += tooltipHeight + 10;

            int bodyHeight = _panel.ContentHeight - (y - _panel.ContentY);
            DrawRecipeSection(x, y, width, bodyHeight, entry);
        }

        private void DrawNavBar(int x, int y, int width)
        {
            bool canGoBack = _history.Count > 0;

            UIRenderer.DrawRect(x, y, 60, NavBarHeight, canGoBack ? UIColors.InputBg : UIColors.SectionBg);
            UIRenderer.DrawText("< Back", x + 6, y + 7, canGoBack ? UIColors.Text : UIColors.TextDim);
            if (canGoBack && WidgetInput.IsMouseOver(x, y, 60, NavBarHeight))
            {
                Tooltip.Set("Back to the previous item");
                if (WidgetInput.MouseLeftClick) { GoBack(); WidgetInput.ConsumeClick(); }
            }

            int homeX = x + 66;
            UIRenderer.DrawRect(homeX, y, 60, NavBarHeight, UIColors.InputBg);
            UIRenderer.DrawText("Home", homeX + 12, y + 7, UIColors.Text);
            if (WidgetInput.IsMouseOver(homeX, y, 60, NavBarHeight))
            {
                Tooltip.Set("Back to the item grid");
                if (WidgetInput.MouseLeftClick) { GoHome(); WidgetInput.ConsumeClick(); }
            }
        }

        private void DrawRecipeSection(int x, int y, int width, int height, ItemEntry entry)
        {
            var recipes = RecipeCatalogue.RecipesFor(entry.Id);
            var usedIn = RecipeCatalogue.UsedIn(entry.Id);

            // Build a flat list of rows to render inside one scroll area: section headers + item rows.
            var rows = new List<(string Header, ItemEntry Item, string Suffix)>();

            if (recipes.Count == 0)
            {
                rows.Add(("Crafting", null, null));
                rows.Add((null, null, "Not craftable — found some other way (drop, purchase, world gen)."));
            }
            else
            {
                foreach (var recipe in recipes)
                {
                    string station = recipe.StationTileIds.Count == 0
                        ? "By Hand"
                        : string.Join(", ", recipe.StationTileIds.Select(RecipeCatalogue.StationName));
                    rows.Add(("Crafting", null, $"At: {station}"));
                    foreach (var ing in recipe.Ingredients)
                    {
                        if (_byId.TryGetValue(ing.ItemId, out var ingEntry))
                            rows.Add((null, ingEntry, $"x{ing.Stack}"));
                    }
                }
            }

            rows.Add(("Used to make", null, null));
            if (usedIn.Count == 0)
            {
                rows.Add((null, null, "Not used in any known recipe."));
            }
            else
            {
                foreach (var recipe in usedIn.Take(30))
                {
                    if (_byId.TryGetValue(recipe.ResultItemId, out var resultEntry))
                        rows.Add((null, resultEntry, null));
                }
                if (usedIn.Count > 30)
                    rows.Add((null, null, $"+{usedIn.Count - 30} more"));
            }

            int contentHeight = rows.Count * RowHeight;
            _detailScroll.Begin(x, y, width, height, contentHeight);

            int rowY = 0;
            foreach (var row in rows)
            {
                if (_detailScroll.IsVisible(rowY, RowHeight))
                {
                    int drawY = _detailScroll.ContentY + rowY;
                    DrawDetailRow(x, drawY, width, row.Header, row.Item, row.Suffix);
                }
                rowY += RowHeight;
            }

            _detailScroll.End();
        }

        private void DrawDetailRow(int x, int y, int width, string header, ItemEntry item, string suffix)
        {
            if (header != null)
            {
                UIRenderer.DrawText(header, x, y + 8, UIColors.TextTitle);
                return;
            }

            if (item == null)
            {
                UIRenderer.DrawText(suffix, x + 4, y + 8, UIColors.TextDim);
                return;
            }

            bool isHover = WidgetInput.IsMouseOver(x, y, width, RowHeight - 4);
            if (isHover)
                UIRenderer.DrawRect(x, y, width, RowHeight - 4, UIColors.InputFocusBg);

            UIRenderer.DrawItem(item.Id, x, y, 28, 28);
            UIRenderer.DrawText(item.Name, x + 34, y + 5, isHover ? UIColors.AccentText : UIColors.Text);
            if (suffix != null)
                UIRenderer.DrawText(suffix, x + width - 60, y + 5, UIColors.TextDim);

            if (isHover)
            {
                ItemTooltip.Set(item.Id);
                if (WidgetInput.MouseLeftClick)
                {
                    OpenDetail(item);
                    WidgetInput.ConsumeClick();
                }
            }
        }
    }
}
