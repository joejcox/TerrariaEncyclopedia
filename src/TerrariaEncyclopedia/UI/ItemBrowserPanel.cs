using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using TerrariaEncyclopedia.Content;

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
        private const int SectionToggleHeight = 30;

        private enum ViewMode { Grid, Detail }

        private static readonly (ItemSortMode Mode, string Label)[] SortModes =
        {
            (ItemSortMode.NameAscending, "A-Z"),
            (ItemSortMode.NameDescending, "Z-A"),
            (ItemSortMode.RarityAscending, "Rarity: Low-High"),
            (ItemSortMode.RarityDescending, "Rarity: High-Low"),
            // "Oldest"/"Newest" approximate release order using item ID — Terraria assigns IDs
            // roughly in the order items were added, though old removed/reused IDs mean it's not exact.
            (ItemSortMode.Oldest, "Oldest First"),
            (ItemSortMode.Newest, "Newest First"),
            (ItemSortMode.SetGrouped, "By Set"),
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

        private readonly IEncyclopediaDataSource _data;
        private readonly DraggablePanel _panel;
        private readonly TextInput _search;
        private readonly ScrollView _scroll = new ScrollView();
        private readonly ScrollView _detailScroll = new ScrollView();

        private List<ItemEntry> _all = new List<ItemEntry>();
        private Dictionary<int, ItemEntry> _byId = new Dictionary<int, ItemEntry>();
        private List<ItemEntry> _filtered = new List<ItemEntry>();
        private int _activeTab;
        private bool _hardmodeOnly;
        private int _sortModeIndex;
        private bool _sortDropdownOpen;
        private int _sortButtonX, _sortButtonY, _sortButtonWidth;

        private ViewMode _view = ViewMode.Grid;
        private ItemEntry _detailItem;
        private readonly EntryNavigation _navigation = new EntryNavigation();

        private readonly EncyclopediaSectionSelection _section;

        // ── NPC section state ──
        private static readonly (NpcFilter Tab, string Label)[] NpcTabs =
        {
            (NpcFilter.All, "All"), (NpcFilter.Town, "Town NPCs"), (NpcFilter.Enemies, "Enemies"), (NpcFilter.Bosses, "Bosses"),
        };
        private static readonly (NpcSortMode Mode, string Label)[] NpcSortModes =
        {
            (NpcSortMode.NameAscending, "A-Z"), (NpcSortMode.NameDescending, "Z-A"),
            (NpcSortMode.LifeAscending, "Life: Low-High"), (NpcSortMode.LifeDescending, "Life: High-Low"),
        };

        private List<NpcEntry> _allNpcs = new List<NpcEntry>();
        private List<NpcEntry> _filteredNpcs = new List<NpcEntry>();
        private readonly TextInput _npcSearch;
        private readonly ScrollView _npcScroll = new ScrollView();
        private readonly ScrollView _npcDetailScroll = new ScrollView();
        private int _npcActiveTab;
        private bool _npcHardmodeOnly;
        private int _npcSortModeIndex;
        private bool _npcSortDropdownOpen;
        private int _npcSortButtonX, _npcSortButtonY, _npcSortButtonWidth;
        private ViewMode _npcView = ViewMode.Grid;
        private NpcEntry _npcDetailItem;

        // ── Events section state ──
        // Events have no live base data at all (see WikiEnrichment/EventWikiInfo) — the wiki
        // entry itself is the display entry, no separate catalogue+enrichment merge needed.
        private List<EventWikiInfo> _events = new List<EventWikiInfo>();
        private List<EventWikiInfo> _filteredEvents = new List<EventWikiInfo>();
        private readonly TextInput _eventSearch;
        private readonly ScrollView _eventScroll = new ScrollView();
        private readonly ScrollView _eventDetailScroll = new ScrollView();
        private ViewMode _eventView = ViewMode.Grid;
        private EventWikiInfo _eventDetailItem;

        public ItemBrowserPanel(IEncyclopediaDataSource data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _search = new TextInput("terraria-encyclopedia-items", "Search items...", 200);
            _npcSearch = new TextInput("terraria-encyclopedia-npcs", "Search NPCs...", 200);
            _eventSearch = new TextInput("terraria-encyclopedia-events", "Search events...", 200);
            _section = new EncyclopediaSectionSelection(
                _search.Unfocus,
                _npcSearch.Unfocus,
                _eventSearch.Unfocus);
            _panel = new DraggablePanel("terraria-encyclopedia", "Terraria Encyclopedia", 1200, 750);
            // Same UIScale/scissor-rect mismatch found in Phase 1 — see Mod.cs history.
            _panel.ClipContent = false;
            _panel.RegisterDrawCallback(OnDraw);
            _panel.OnClose = () => { _search.Unfocus(); _npcSearch.Unfocus(); _eventSearch.Unfocus(); };
        }

        public bool IsOpen => _panel.IsOpen;

        public bool IsTextEntryFocused => _search.IsFocused || _npcSearch.IsFocused || _eventSearch.IsFocused;

        public int LoadedItemCount => _all.Count;

        public int LoadedNpcCount => _allNpcs.Count;

        public void Toggle()
        {
            if (_panel.IsOpen)
            {
                _panel.Close();
                return;
            }

            _data.BeginBrowseSession();

            if (_all.Count == 0)
            {
                _all = _data.LoadItems().ToList();
                _byId = _all.ToDictionary(e => e.Id);
                Filter();
            }

            if (_allNpcs.Count == 0)
            {
                _allNpcs = _data.LoadNpcs().ToList();
                FilterNpcs();
            }

            if (_events.Count == 0)
            {
                _events = _data.LoadEvents().ToList();
                FilterEvents();
            }

            GoHome();
            _panel.Open();
        }

        public void Close() => _panel.Close();

        public void Update()
        {
            _panel.Update();
            if (!_panel.IsOpen) return;
            _search.Update();
            _npcSearch.Update();
            _eventSearch.Update();
        }

        public void Render() => _panel.Draw();

        public void Unload()
        {
            _panel.UnregisterDrawCallback();
            _search.Unfocus();
            _npcSearch.Unfocus();
            _eventSearch.Unfocus();
        }

        private void Filter()
        {
            ItemCategory? category = _activeTab == 0
                ? (ItemCategory?)null
                : (ItemCategory)(_activeTab - 1);
            _filtered = EncyclopediaQueries.FilterItems(
                _all,
                category,
                _hardmodeOnly,
                _search.Text,
                SortModes[_sortModeIndex].Mode,
                _data.IsHardmodeItem).ToList();
            _scroll.ResetScroll();
            _panel.Title = $"Terraria Encyclopedia ({_filtered.Count}/{_all.Count})";
        }

        private void GoHome()
        {
            _navigation.Home();
            _detailItem = null;
            _view = ViewMode.Grid;
            _npcDetailItem = null;
            _npcView = ViewMode.Grid;
            _eventDetailItem = null;
            _eventView = ViewMode.Grid;
        }

        private void OpenDetail(ItemEntry entry)
        {
            _navigation.Open(entry.Id);
            _detailItem = entry;
            _view = ViewMode.Detail;
            _detailScroll.ResetScroll();
        }

        private void GoBack()
        {
            int prevId;
            if (!_navigation.TryGoBack(out prevId))
            {
                GoHome();
                return;
            }
            _byId.TryGetValue(prevId, out _detailItem);
            _detailScroll.ResetScroll();
        }

        private void OnDraw()
        {
            if (!_panel.BeginDraw()) return;
            try
            {
                DrawSectionToggle(_panel.ContentX, _panel.ContentY, _panel.ContentWidth);

                if (_section.Current == EncyclopediaSection.Items)
                {
                    if (_view == ViewMode.Grid) DrawGridView();
                    else DrawDetailView();
                }
                else if (_section.Current == EncyclopediaSection.Npcs)
                {
                    if (_npcView == ViewMode.Grid) DrawNpcGridView();
                    else DrawNpcDetailView();
                }
                else
                {
                    if (_eventView == ViewMode.Grid) DrawEventListView();
                    else DrawEventDetailView();
                }
            }
            finally
            {
                _panel.EndDraw();
            }
        }

        private void DrawSectionToggle(int x, int y, int width)
        {
            int thirdWidth = width / 3;
            DrawSectionButton(x, y, thirdWidth - 2, "Items", EncyclopediaSection.Items);
            DrawSectionButton(x + thirdWidth + 2, y, thirdWidth - 2, "NPCs", EncyclopediaSection.Npcs);
            DrawSectionButton(
                x + thirdWidth * 2 + 4,
                y,
                width - thirdWidth * 2 - 4,
                "Events",
                EncyclopediaSection.Events);
        }

        private void DrawSectionButton(int x, int y, int width, string label, EncyclopediaSection section)
        {
            bool isActive = _section.Current == section;
            bool isHover = WidgetInput.IsMouseOver(x, y, width, SectionToggleHeight - 4);

            UIRenderer.DrawRect(x, y, width, SectionToggleHeight - 4,
                isActive ? UIColors.ItemActiveBg : (isHover ? UIColors.SectionBg : UIColors.InputBg));
            if (isHover)
                UIRenderer.DrawRectOutline(
                    x, y, width, SectionToggleHeight - 4, UIColors.Accent);
            if (isActive)
                UIRenderer.DrawRect(
                    x, y + SectionToggleHeight - 6, width, 2, UIColors.Accent);

            int textWidth = TextUtil.MeasureWidth(label);
            UIRenderer.DrawText(label, x + (width - textWidth) / 2, y + 5,
                isActive || isHover ? UIColors.AccentText : UIColors.TextDim);

            if (isHover && WidgetInput.MouseLeftClick)
            {
                WidgetInput.ConsumeClick();
                _section.SwitchTo(section);
            }
        }

        private void DrawGridView()
        {
            var s = new StackLayout(_panel.ContentX, _panel.ContentY + SectionToggleHeight, _panel.ContentWidth);

            int searchY = s.Advance(28);
            _search.Draw(s.X, searchY, 200, 26);
            if (_search.HasChanged) Filter();

            DrawSortButton(s.X + 210, searchY - 1, 190);

            if (Checkbox.DrawWithLabel(s.X + 410, searchY - 1, s.Width - 410, 28, "Hardmode only", _hardmodeOnly))
            {
                _hardmodeOnly = !_hardmodeOnly;
                Filter();
            }

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
            int gridHeight = _panel.ContentHeight - SectionToggleHeight - s.TotalHeight;

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
                if (isHover)
                    UIRenderer.DrawRectOutline(tabX, y, tabW, 30, UIColors.Accent);
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
                        isActive || isHover ? UIColors.AccentText : UIColors.TextDim);
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
        private const int DetailPadX = 14;

        private void DrawDetailView()
        {
            var entry = _detailItem;
            if (entry == null) { GoHome(); return; }

            int x = _panel.ContentX + DetailPadX;
            int y = _panel.ContentY + SectionToggleHeight;
            int width = _panel.ContentWidth - DetailPadX * 2;

            DrawNavBar(x, y, width, GoBack, GoHome, _navigation.CanGoBack);
            string badge = entry.IsExpertExclusive ? "EXPERT" : null;
            if (_data.IsHardmodeItem(entry.Name)) badge = badge == null ? "HARDMODE" : badge + "  HARDMODE";
            if (badge != null) DrawHardmodeMarker(x, y, width, badge);
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

            y += DrawItemFacts(x, y, width, entry) + 6;

            int bodyHeight = _panel.ContentHeight - (y - _panel.ContentY);
            DrawRecipeSection(x, y, width, bodyHeight, entry);
        }

        /// <summary>Sell price, research count, granted buff (+tooltip/duration), and created projectile — all read live from the game, not the wiki.</summary>
        private int DrawItemFacts(int x, int y, int width, ItemEntry entry)
        {
            int startY = y;

            if (entry.SellValue > 0)
            {
                UIRenderer.DrawText("Sell: ", x, y, UIColors.Text);
                int labelW = TextUtil.MeasureWidth("Sell: ");
                DrawCoinValue(x + labelW, y, entry.SellValue);
                y += Math.Max(UIRenderer.TextLineHeight, CoinIconSize);
            }

            if (entry.ResearchCount > 0)
            {
                UIRenderer.DrawText($"Research: {entry.ResearchCount} required", x, y, UIColors.Text);
                y += UIRenderer.TextLineHeight;
            }

            if (entry.BuffType > 0)
            {
                int iconSize = Math.Max(UIRenderer.TextLineHeight, 20);
                UIRenderer.DrawBuff(entry.BuffType, x, y, iconSize, iconSize);
                UIRenderer.DrawText("Grants Buff: ", x + iconSize + 6, y + 2, LabelBlue);
                int labelW = TextUtil.MeasureWidth("Grants Buff: ");
                UIRenderer.DrawText(entry.BuffName ?? "", x + iconSize + 6 + labelW, y + 2, UIColors.Text);
                y += iconSize + 2;

                if (!string.IsNullOrEmpty(entry.BuffTooltip))
                {
                    y += DrawWrappedText(entry.BuffTooltip, x, y, width, UIColors.TextDim);
                }

                if (entry.BuffDurationSeconds > 0)
                {
                    UIRenderer.DrawText($"Duration: {entry.BuffDurationSeconds}s", x, y, UIColors.TextDim);
                    y += UIRenderer.TextLineHeight;
                }
            }

            if (entry.ProjectileType > 0 && !string.IsNullOrEmpty(entry.ProjectileName))
            {
                UIRenderer.DrawText("Creates Projectile: ", x, y, LabelGreen);
                int labelW = TextUtil.MeasureWidth("Creates Projectile: ");
                UIRenderer.DrawText(entry.ProjectileName, x + labelW, y, UIColors.Text);
                y += UIRenderer.TextLineHeight;
            }

            return y - startY;
        }

        private const int CoinIconSize = 18;
        private static readonly int[] CoinIconIds = { ItemID.PlatinumCoin, ItemID.GoldCoin, ItemID.SilverCoin, ItemID.CopperCoin };

        /// <summary>Plain-text "15g 3s" style coin formatting, for plain-text row contexts that can't draw inline icons (unlike DrawCoinValue).</summary>
        private static string FormatCoinsText(int copper)
        {
            int platinum = copper / 1000000;
            int gold = copper / 10000 % 100;
            int silver = copper / 100 % 100;
            int c = copper % 100;

            var parts = new List<string>();
            if (platinum > 0) parts.Add($"{platinum}p");
            if (gold > 0) parts.Add($"{gold}g");
            if (silver > 0) parts.Add($"{silver}s");
            if (c > 0 || parts.Count == 0) parts.Add($"{c}c");
            return string.Join(" ", parts);
        }

        /// <summary>Draws "&lt;n&gt;[coin icon]" per nonzero denomination, highest first — matches the game's own coin display instead of a "15g" letter suffix. Returns width used.</summary>
        private static int DrawCoinValue(int x, int y, int copper)
        {
            int[] amounts =
            {
                copper / 1000000,       // platinum
                copper / 10000 % 100,   // gold
                copper / 100 % 100,     // silver
                copper % 100,           // copper
            };

            int curX = x;
            bool any = false;
            for (int i = 0; i < amounts.Length; i++)
            {
                if (amounts[i] == 0) continue;
                any = true;
                string label = amounts[i].ToString();
                UIRenderer.DrawText(label, curX, y + (CoinIconSize - UIRenderer.TextLineHeight) / 2, UIColors.Text);
                curX += TextUtil.MeasureWidth(label) + 2;
                UIRenderer.DrawItem(CoinIconIds[i], curX, y, CoinIconSize, CoinIconSize);
                curX += CoinIconSize + 6;
            }

            if (!any)
            {
                UIRenderer.DrawText("0", curX, y + (CoinIconSize - UIRenderer.TextLineHeight) / 2, UIColors.Text);
                curX += TextUtil.MeasureWidth("0") + 2;
                UIRenderer.DrawItem(ItemID.CopperCoin, curX, y, CoinIconSize, CoinIconSize);
                curX += CoinIconSize;
            }

            return curX - x;
        }

        private void DrawNavBar(int x, int y, int width, Action onBack, Action onHome, bool canGoBack)
        {
            UIRenderer.DrawRect(x, y, 60, NavBarHeight, canGoBack ? UIColors.InputBg : UIColors.SectionBg);
            UIRenderer.DrawText("< Back", x + 6, y + 7, canGoBack ? UIColors.Text : UIColors.TextDim);
            if (canGoBack && WidgetInput.IsMouseOver(x, y, 60, NavBarHeight))
            {
                Tooltip.Set("Back to the previous entry");
                if (WidgetInput.MouseLeftClick) { onBack(); WidgetInput.ConsumeClick(); }
            }

            int homeX = x + 66;
            UIRenderer.DrawRect(homeX, y, 60, NavBarHeight, UIColors.InputBg);
            UIRenderer.DrawText("Home", homeX + 12, y + 7, UIColors.Text);
            if (WidgetInput.IsMouseOver(homeX, y, 60, NavBarHeight))
            {
                Tooltip.Set("Back to the grid");
                if (WidgetInput.MouseLeftClick) { onHome(); WidgetInput.ConsumeClick(); }
            }
        }

        private const int MaxCraftingTreeDepth = 4;

        private void DrawRecipeSection(int x, int y, int width, int height, ItemEntry entry)
        {
            var acquisition = EncyclopediaQueries.ComposeItemAcquisition(
                _data.RecipesFor(entry.Id),
                _data.SourcesFor(entry.Id),
                _data.SellersOf(entry.Id));
            var usedIn = _data.UsedIn(entry.Id);

            // Build a flat list of rows to render inside one scroll area: section headers + item rows.
            var rows = new List<(string Header, ItemEntry Item, string Suffix, int IndentX)>();

            if (acquisition.Recipes.Count == 0)
            {
                rows.Add(("Crafting", null, null, 0));
            }
            else
            {
                foreach (var recipe in acquisition.Recipes)
                {
                    rows.Add(("Crafting", null, null, 0));
                    if (recipe.StationTileIds.Count > 0)
                    {
                        string station = string.Join(", ", recipe.StationTileIds.Select(_data.StationName));
                        rows.Add((null, null, $"At: {station}", 0));
                    }
                    foreach (var ing in recipe.Ingredients)
                    {
                        if (_byId.TryGetValue(ing.ItemId, out var ingEntry))
                            AddIngredientTreeRows(rows, ingEntry, ing.Stack, 0, new HashSet<int> { entry.Id });
                    }
                }
            }

            // Reuses the same live NPC drop-rule data (Main.ItemDropsDB) the NPC pages
            // already show — condition text here is exact (e.g. "In Master Mode", not
            // "Hardmode") since it comes from the real drop rule, not a guess. Acquisition
            // rows are independent of craftability because an item can have several sources.
            foreach (var drop in acquisition.Drops)
            {
                var dropNpc = _allNpcs.FirstOrDefault(n => n.Id == drop.NpcId);
                if (dropNpc == null) continue;

                string chance = $"{drop.Chance * 100:0.##}%";
                string conditions = drop.Conditions.Count == 0 ? "" : $" ({string.Join(", ", drop.Conditions)})";
                rows.Add((null, null, $"Dropped by {dropNpc.Name} — {chance}{conditions}", 0));
            }

            foreach (var seller in acquisition.Sellers)
            {
                var sellerNpc = _allNpcs.FirstOrDefault(n => n.Id == seller.NpcId);
                if (sellerNpc == null) continue;
                rows.Add((null, null, $"Sold by {sellerNpc.Name} — {FormatCoinsText(seller.Cost)}", 0));
            }

            if (acquisition.NeedsUnknownSourceFallback)
                rows.Add((null, null, "Not craftable — found some other way (drop, purchase, world gen).", 0));

            rows.Add(("Used to make", null, null, 0));
            if (usedIn.Count == 0)
            {
                rows.Add((null, null, "Not used in any known recipe.", 0));
            }
            else
            {
                foreach (var recipe in usedIn.Take(30))
                {
                    if (_byId.TryGetValue(recipe.ResultItemId, out var resultEntry))
                        rows.Add((null, resultEntry, null, 0));
                }
                if (usedIn.Count > 30)
                    rows.Add((null, null, $"+{usedIn.Count - 30} more", 0));
            }

            int contentHeight = rows.Count * RowHeight;
            _detailScroll.Begin(x, y, width, height, contentHeight);

            int rowY = 0;
            foreach (var row in rows)
            {
                if (_detailScroll.IsVisible(rowY, RowHeight))
                {
                    int drawY = _detailScroll.ContentY + rowY;
                    DrawDetailRow(x, drawY, width, row.Header, row.Item, row.Suffix, row.IndentX);
                }
                rowY += RowHeight;
            }

            _detailScroll.End();
        }

        /// <summary>
        /// Recursively lists what each ingredient is itself crafted from — a flattened, indented
        /// "crafting tree" rather than the wiki's boxed diagram. Only the first recipe per item is
        /// followed (many items have several) to keep it readable, depth-capped, and cycle-guarded
        /// via a path-scoped visited set (add before recursing, remove after) so the same
        /// intermediate can still appear under two different branches without looping forever.
        /// </summary>
        private void AddIngredientTreeRows(List<(string Header, ItemEntry Item, string Suffix, int IndentX)> rows,
            ItemEntry ingEntry, int stack, int depth, HashSet<int> visited)
        {
            rows.Add((null, ingEntry, $"x{stack}", depth));

            if (depth >= MaxCraftingTreeDepth || !visited.Add(ingEntry.Id)) return;

            var subRecipes = _data.RecipesFor(ingEntry.Id);
            if (subRecipes.Count > 0)
            {
                foreach (var subIng in subRecipes[0].Ingredients)
                {
                    if (_byId.TryGetValue(subIng.ItemId, out var subEntry))
                        AddIngredientTreeRows(rows, subEntry, subIng.Stack, depth + 1, visited);
                }
            }

            visited.Remove(ingEntry.Id);
        }

        private void DrawDetailRow(int x, int y, int width, string header, ItemEntry item, string suffix, int indentX = 0)
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

            int itemX = x + indentX * 20;
            int rowWidth = width - indentX * 20;
            bool isHover = WidgetInput.IsMouseOver(itemX, y, rowWidth, RowHeight - 4);
            if (isHover)
                UIRenderer.DrawRect(itemX, y, rowWidth, RowHeight - 4, UIColors.InputFocusBg);

            UIRenderer.DrawItem(item.Id, itemX, y, 28, 28);
            UIRenderer.DrawText(item.Name, itemX + 34, y + 5, isHover ? UIColors.AccentText : UIColors.Text);
            if (suffix != null)
            {
                int nameWidth = TextUtil.MeasureWidth(item.Name);
                UIRenderer.DrawText(suffix, itemX + 34 + nameWidth + 10, y + 5, UIColors.TextDim);
            }

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

        // ── NPC section ──────────────────────────────────────────────────────

        private void FilterNpcs()
        {
            _filteredNpcs = EncyclopediaQueries.FilterNpcs(
                _allNpcs,
                NpcTabs[_npcActiveTab].Tab,
                _npcHardmodeOnly,
                _npcSearch.Text,
                NpcSortModes[_npcSortModeIndex].Mode,
                _data.IsHardmodeNpc).ToList();
            _npcScroll.ResetScroll();
            _panel.Title = $"Terraria Encyclopedia ({_filteredNpcs.Count}/{_allNpcs.Count})";
        }

        private static readonly string[] DefaultCollapsedSections =
            { "Drops", "Items Sold", "Living Preferences", "Summoning & Spawning", "Expert Mode", "Aftermath", "Tips" };

        private void OpenNpcDetail(NpcEntry entry)
        {
            _npcDetailItem = entry;
            _npcView = ViewMode.Detail;
            _npcDetailScroll.ResetScroll();

            _npcCollapsedSections.Clear();
            foreach (var key in DefaultCollapsedSections) _npcCollapsedSections.Add(key);
        }

        private void DrawNpcGridView()
        {
            var s = new StackLayout(_panel.ContentX, _panel.ContentY + SectionToggleHeight, _panel.ContentWidth);

            int searchY = s.Advance(28);
            _npcSearch.Draw(s.X, searchY, 200, 26);
            if (_npcSearch.HasChanged) FilterNpcs();

            DrawNpcSortButton(s.X + 210, searchY - 1, 190);

            if (Checkbox.DrawWithLabel(s.X + 410, searchY - 1, s.Width - 410, 28, "Hardmode only", _npcHardmodeOnly))
            {
                _npcHardmodeOnly = !_npcHardmodeOnly;
                FilterNpcs();
            }

            int tabsY = s.Advance(34);
            bool interactive = !_npcSortDropdownOpen;
            DrawNpcTabs(s.X, tabsY, s.Width, interactive);

            s.Advance(6);

            int gridX = s.X;
            int gridY = s.CurrentY;
            int gridWidth = s.Width;
            int gridHeight = _panel.ContentHeight - SectionToggleHeight - s.TotalHeight;

            DrawNpcGrid(gridX, gridY, gridWidth, gridHeight, interactive);

            if (_npcSortDropdownOpen) DrawNpcSortDropdown();
        }

        private void DrawNpcTabs(int x, int y, int width, bool interactive)
        {
            int tabWidth = width / NpcTabs.Length;

            for (int i = 0; i < NpcTabs.Length; i++)
            {
                int tabX = x + i * tabWidth;
                int tabW = (i == NpcTabs.Length - 1) ? (width - i * tabWidth) : tabWidth - 2;
                bool isActive = _npcActiveTab == i;
                bool isHover = interactive && WidgetInput.IsMouseOver(tabX, y, tabW, 30);

                UIRenderer.DrawRect(tabX, y, tabW, 30,
                    isActive ? UIColors.ItemActiveBg : (isHover ? UIColors.SectionBg : UIColors.InputBg));
                if (isHover)
                    UIRenderer.DrawRectOutline(tabX, y, tabW, 30, UIColors.Accent);
                if (isActive)
                    UIRenderer.DrawRect(tabX, y + 28, tabW, 2, UIColors.Accent);

                string label = NpcTabs[i].Label;
                int textWidth = TextUtil.MeasureWidth(label);
                UIRenderer.DrawText(label, tabX + (tabW - textWidth) / 2, y + 8,
                    isActive || isHover ? UIColors.AccentText : UIColors.TextDim);

                if (isHover && WidgetInput.MouseLeftClick)
                {
                    WidgetInput.ConsumeClick();
                    if (_npcActiveTab != i)
                    {
                        _npcActiveTab = i;
                        FilterNpcs();
                    }
                }
            }
        }

        private void DrawNpcSortButton(int x, int y, int width)
        {
            _npcSortButtonX = x;
            _npcSortButtonY = y;
            _npcSortButtonWidth = width;

            string label = "Sort: " + NpcSortModes[_npcSortModeIndex].Label + (_npcSortDropdownOpen ? "  ^" : "  v");
            bool isHover = WidgetInput.IsMouseOver(x, y, width, 28);

            UIRenderer.DrawRect(x, y, width, 28, isHover || _npcSortDropdownOpen ? UIColors.SectionBg : UIColors.InputBg);
            UIRenderer.DrawText(label, x + 6, y + 6, UIColors.Text);

            if (isHover && WidgetInput.MouseLeftClick)
            {
                WidgetInput.ConsumeClick();
                _npcSortDropdownOpen = !_npcSortDropdownOpen;
            }
        }

        private void DrawNpcSortDropdown()
        {
            int rowHeight = 26;
            int listY = _npcSortButtonY + 28;
            int listHeight = NpcSortModes.Length * rowHeight;

            UIRenderer.DrawRect(_npcSortButtonX, listY, _npcSortButtonWidth, listHeight, UIColors.PanelBg);
            UIRenderer.DrawRectOutline(_npcSortButtonX, listY, _npcSortButtonWidth, listHeight, UIColors.Border);

            for (int i = 0; i < NpcSortModes.Length; i++)
            {
                int rowY = listY + i * rowHeight;
                bool isActive = i == _npcSortModeIndex;
                bool isHover = WidgetInput.IsMouseOver(_npcSortButtonX, rowY, _npcSortButtonWidth, rowHeight);

                if (isActive || isHover)
                    UIRenderer.DrawRect(_npcSortButtonX, rowY, _npcSortButtonWidth, rowHeight,
                        isActive ? UIColors.ItemActiveBg : UIColors.SectionBg);

                UIRenderer.DrawText(NpcSortModes[i].Label, _npcSortButtonX + 6, rowY + 5,
                    isActive ? UIColors.AccentText : UIColors.Text);

                if (isHover && WidgetInput.MouseLeftClick)
                {
                    WidgetInput.ConsumeClick();
                    _npcSortModeIndex = i;
                    _npcSortDropdownOpen = false;
                    FilterNpcs();
                }
            }

            bool overButton = WidgetInput.IsMouseOver(_npcSortButtonX, _npcSortButtonY, _npcSortButtonWidth, 28);
            bool overList = WidgetInput.IsMouseOver(_npcSortButtonX, listY, _npcSortButtonWidth, listHeight);
            if (!overButton && !overList && WidgetInput.MouseLeftClick)
            {
                WidgetInput.ConsumeClick();
                _npcSortDropdownOpen = false;
            }
        }

        private void DrawNpcGrid(int gridX, int gridY, int gridWidth, int gridHeight, bool interactive)
        {
            int gridColumns = Math.Max(1, _npcScroll.ContentWidth > 0 ? _npcScroll.ContentWidth / SlotSize : gridWidth / SlotSize);
            int totalRows = (_filteredNpcs.Count + gridColumns - 1) / gridColumns;
            int totalContentHeight = totalRows * SlotSize;

            _npcScroll.Begin(gridX, gridY, gridWidth, gridHeight, totalContentHeight);

            int contentWidth = _npcScroll.ContentWidth;
            gridColumns = Math.Max(1, contentWidth / SlotSize);

            int visibleRows = gridHeight / SlotSize;
            int startRow = _npcScroll.ScrollOffset / SlotSize;
            int startIndex = startRow * gridColumns;

            for (int i = 0; i < (visibleRows + 2) * gridColumns && startIndex + i < _filteredNpcs.Count; i++)
            {
                int npcIdx = startIndex + i;
                int row = i / gridColumns;
                int col = i % gridColumns;

                int itemContentY = (startRow + row) * SlotSize;
                if (!_npcScroll.IsVisible(itemContentY, SlotSize)) continue;

                int slotX = gridX + col * SlotSize;
                int slotY = _npcScroll.ContentY + itemContentY;

                var entry = _filteredNpcs[npcIdx];
                bool inScrollBounds = interactive && WidgetInput.IsMouseOver(gridX, gridY, gridWidth, gridHeight);
                bool isHover = inScrollBounds && WidgetInput.IsMouseOver(slotX, slotY, SlotOuter, SlotOuter);

                UIRenderer.DrawRect(slotX, slotY, SlotOuter, SlotOuter, isHover ? UIColors.InputFocusBg : UIColors.ItemBg);
                if (isHover)
                    UIRenderer.DrawRectOutline(slotX, slotY, SlotOuter, SlotOuter, UIColors.Accent, 1);

                UIRenderer.DrawNPC(entry.Id, slotX + IconPad, slotY + IconPad, IconSize, IconSize);

                if (isHover)
                {
                    Tooltip.Set(entry.Name, entry.IsBoss ? "Boss" : entry.IsTownNpc ? "Town NPC" : "Enemy");
                    if (WidgetInput.MouseLeftClick)
                    {
                        OpenNpcDetail(entry);
                        WidgetInput.ConsumeClick();
                    }
                }
            }

            _npcScroll.End();

            if (_filteredNpcs.Count == 0)
                UIRenderer.DrawText("No NPCs found", gridX + 5, gridY + 10, UIColors.Warning);
        }

        private void DrawNpcDetailView()
        {
            var entry = _npcDetailItem;
            if (entry == null) { GoHome(); return; }

            int x = _panel.ContentX + DetailPadX;
            int y = _panel.ContentY + SectionToggleHeight;
            int width = _panel.ContentWidth - DetailPadX * 2;

            DrawNavBar(x, y, width, GoHome, GoHome, false); // no cross-NPC nav chain — Back == Home here
            if (_data.IsHardmodeNpc(entry.Name)) DrawHardmodeMarker(x, y, width);
            y += NavBarHeight + 6;

            UIRenderer.DrawNPC(entry.Id, x, y, 48, 48);
            UIRenderer.DrawText(entry.Name, x + 58, y, UIColors.TextTitle);
            string kind = entry.IsBoss ? "Boss" : entry.IsTownNpc ? "Town NPC" : "Enemy";
            UIRenderer.DrawText($"{kind}  •  ID {entry.Id}", x + 58, y + 24, UIColors.TextDim);
            y += 56;

            var stats = new List<string> { $"Life: {entry.Life}", $"Defense: {entry.Defense}", $"Damage: {entry.Damage}" };
            UIRenderer.DrawText(string.Join("    ", stats), x, y, UIColors.Text);
            y += 26;

            if (entry.IsBoss)
            {
                var wiki = _data.GetBossInfo(entry.Name);
                if (wiki != null) y += DrawBossInfoLine(x, y, width, wiki) + 6;
            }

            int bodyHeight = _panel.ContentHeight - (y - _panel.ContentY);
            DrawNpcBody(x, y, width, bodyHeight, entry);
        }

        /// <summary>
        /// Boss Type/Environment/KB Resist/Immune-to below the stats row — not a collapsible
        /// section, always visible. Split into two related groups (Type+Environment, then
        /// KB Resist+Immune to) instead of one crammed line: each group only wraps onto extra
        /// lines if its own content overflows, so most bosses still render in exactly two lines.
        /// AI Type is omitted: it's just "&lt;Boss Name&gt; AI" for almost every boss and conveys nothing.
        /// </summary>
        private int DrawBossInfoLine(int x, int y, int maxWidth, BossWikiInfo wiki)
        {
            int h1 = DrawFactGroup(x, y, maxWidth, new List<(string, Color4, string[])>
            {
                ("Type", LabelPurple, wiki.Types),
                ("Environment", LabelGreen, wiki.Environments),
            });

            int h2 = DrawFactGroup(x, y + h1, maxWidth, new List<(string, Color4, string[])>
            {
                ("KB Resist", LabelTeal, string.IsNullOrEmpty(wiki.KbResist) ? null : new[] { wiki.KbResist }),
                ("Immune to", LabelRed, wiki.ImmuneTo),
            });

            return h1 + h2;
        }

        /// <summary>Draws one wrapping, color-coded line (or more, if it overflows maxWidth) from label:value facts, skipping empty ones. Returns height used.</summary>
        private int DrawFactGroup(int x, int y, int maxWidth, List<(string Label, Color4 LabelColor, string[] Values)> facts)
        {
            var segments = new List<(string Text, Color4 Color)>();
            foreach (var fact in facts)
            {
                if (fact.Values == null || fact.Values.Length == 0) continue;
                if (segments.Count > 0) segments.Add((" - ", UIColors.TextDim));
                segments.Add((fact.Label + ": ", fact.LabelColor));
                segments.Add((string.Join(" | ", fact.Values), UIColors.Text));
            }

            if (segments.Count == 0) return 0;

            int lineHeight = UIRenderer.TextLineHeight;
            int curX = x, curY = y, lineCount = 1;
            foreach (var seg in segments)
            {
                int segWidth = TextUtil.MeasureWidth(seg.Text);
                if (curX + segWidth > x + maxWidth && curX > x)
                {
                    curX = x;
                    curY += lineHeight;
                    lineCount++;
                }
                UIRenderer.DrawText(seg.Text, curX, curY, seg.Color);

                if (seg.Text.StartsWith("KB Resist") && WidgetInput.IsMouseOver(curX, curY, segWidth, lineHeight))
                    Tooltip.Set("Knockback Resistance");

                curX += segWidth;
            }
            return lineCount * lineHeight;
        }

        private void DrawHardmodeMarker(int x, int y, int width, string label = "HARDMODE")
        {
            int textWidth = TextUtil.MeasureWidth(label);
            UIRenderer.DrawText(label, x + width - textWidth, y + 7, UIColors.Error);
        }

        /// <summary>Minimal word-wrap for the Bestiary description (plain UIRenderer.DrawText has none). Returns height used.</summary>
        private int DrawWrappedText(string text, int x, int y, int maxWidth, Color4 color)
        {
            var lines = WrapTextLines(text, maxWidth);
            int lineHeight = UIRenderer.TextLineHeight;
            for (int i = 0; i < lines.Count; i++)
                UIRenderer.DrawText(lines[i].Text, x + lines[i].IndentX, y + i * lineHeight, color);
            return lines.Count * lineHeight;
        }

        /// <summary>
        /// Word-wraps text, splitting on "\n" for forced breaks (bullet points/paragraphs from
        /// the wiki scraper). A leading "- " or "  - " bullet marker gets a hanging indent —
        /// continuation lines line up under the first word after the dash, not under the dash
        /// itself, matching how the request wants "clearer separation" between bullets.
        /// </summary>
        private List<(string Text, int IndentX)> WrapTextLines(string text, int maxWidth)
        {
            var result = new List<(string, int)>();

            foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
            {
                int dashIdx = 0;
                while (dashIdx < paragraph.Length && paragraph[dashIdx] == ' ') dashIdx++;
                bool hasBullet = dashIdx < paragraph.Length - 1 && paragraph[dashIdx] == '-' && paragraph[dashIdx + 1] == ' ';
                string bulletPrefix = hasBullet ? paragraph.Substring(0, dashIdx + 2) : "";
                int indentPx = hasBullet ? TextUtil.MeasureWidth(bulletPrefix) : 0;

                var words = paragraph.Split(' ');
                string line = "";
                bool firstLine = true;
                foreach (var word in words)
                {
                    int availableWidth = firstLine ? maxWidth : maxWidth - indentPx;
                    string candidate = line.Length == 0 ? word : line + " " + word;
                    if (TextUtil.MeasureWidth(candidate) > availableWidth && line.Length > 0)
                    {
                        result.Add((line, firstLine ? 0 : indentPx));
                        firstLine = false;
                        line = word;
                    }
                    else
                    {
                        line = candidate;
                    }
                }
                result.Add((line, firstLine ? 0 : indentPx)); // even an empty paragraph preserves the blank line
            }
            return result;
        }

        // Chance/stack/conditions text can run long ("Not Expert, First Time Killing Plantera")
        // — its own row beneath the name avoids overflowing the panel instead of a fixed-width
        // column, which is what the plain single-line RowHeight rows use for short "x1"/"x99".
        // Computed from the game's real font height (UIRenderer.TextLineHeight), not a guessed
        // constant — a hardcoded 18-20px guess overlapped badly on long wrapped text (worse the
        // more lines stacked), since the actual rendered font is taller than that.
        private static int DropRowHeight => 24 + UIRenderer.TextLineHeight + 8;

        private static int TextRowHeight => UIRenderer.TextLineHeight;

        private static readonly Color4 LabelPurple = new Color4(190, 130, 230);
        private static readonly Color4 LabelGreen = new Color4(120, 200, 120);
        private static readonly Color4 LabelBlue = new Color4(120, 170, 230);
        private static readonly Color4 LabelTeal = new Color4(90, 200, 190);
        private static readonly Color4 LabelRed = new Color4(220, 100, 100);

        private class BodyRow
        {
            public string Header;
            public string SectionKey; // non-null on a header row that should be collapsible
            public ItemEntry Item;
            public string Suffix;
            public int IndentX;
            public int Height;
            public int? ShopCost; // non-null (with Item also set) => draw as an icon+name+coin-cost shop row
            public NpcPersonalityEntry PersonalityTable; // non-null => draw the Living Preferences table
            public HashSet<string> CollapseState;
        }

        private readonly HashSet<string> _npcCollapsedSections = new HashSet<string>();
        private readonly HashSet<string> _eventCollapsedSections = new HashSet<string>();

        private void AddWrappedRows(List<BodyRow> rows, string text, int width)
        {
            foreach (var line in WrapTextLines(text, width))
                rows.Add(new BodyRow { Suffix = line.Text, IndentX = line.IndentX, Height = TextRowHeight });
        }

        /// <summary>Adds a clickable, collapsible section header, then the body rows (via addBody) unless collapsed.</summary>
        private static void AddSection(
            List<BodyRow> rows,
            HashSet<string> collapseState,
            string key,
            Action addBody)
        {
            rows.Add(new BodyRow
            {
                Header = key,
                SectionKey = key,
                Height = RowHeight,
                CollapseState = collapseState
            });
            if (!collapseState.Contains(key)) addBody();
        }

        private void DrawNpcBody(int x, int y, int width, int height, NpcEntry entry)
        {
            var rows = new List<BodyRow>();

            // Description is a plain heading + text, not collapsible — always visible.
            if (!string.IsNullOrEmpty(entry.Description))
            {
                rows.Add(new BodyRow { Header = "Description", Height = RowHeight });
                AddWrappedRows(rows, entry.Description, width);
                rows.Add(new BodyRow { Suffix = "", Height = 8 }); // spacer
            }

            // Town NPCs can't normally be killed, so a drop table isn't meaningful for them —
            // a handful can still drop something if killed by an enemy while defenseless, but
            // the odds are negligible enough not to show.
            if (!entry.IsTownNpc)
            {
                var drops = _data.DropsFor(entry.Id).OrderByDescending(d => d.Chance).ToList();
                AddSection(rows, _npcCollapsedSections, "Drops", () =>
                {
                    if (drops.Count == 0)
                    {
                        rows.Add(new BodyRow { Suffix = "No known item drops (may drop coins only, or nothing).", Height = RowHeight });
                        return;
                    }

                    foreach (var drop in drops)
                    {
                        if (!_byId.TryGetValue(drop.ItemId, out var itemEntry)) continue;

                        string chance = $"{drop.Chance * 100:0.##}%";
                        string stack = drop.StackMax > drop.StackMin ? $"x{drop.StackMin}-{drop.StackMax}" : $"x{drop.StackMin}";
                        string suffix = drop.Conditions.Count == 0
                            ? $"{chance}  {stack}"
                            : $"{chance}  {stack}  ({string.Join(", ", drop.Conditions)})";

                        rows.Add(new BodyRow { Item = itemEntry, Suffix = suffix, Height = DropRowHeight });
                    }
                });
            }

            var shop = _data.ShopFor(entry.Id);
            if (shop.Count > 0)
            {
                AddSection(rows, _npcCollapsedSections, "Items Sold", () =>
                {
                    foreach (var shopItem in shop)
                    {
                        if (_byId.TryGetValue(shopItem.ItemId, out var itemEntry))
                            rows.Add(new BodyRow { Item = itemEntry, ShopCost = shopItem.Cost, Height = RowHeight });
                    }
                });
            }

            // The data source resolves the complete wiki table over the live fallback while
            // retaining Terraria's unique-behaviour marker for NPCs such as the Princess.
            var displayPersonality = _data.GetNpcPersonality(entry.Id, entry.Name);
            bool hasUniqueBehavior = displayPersonality?.HasUniqueBehavior ?? false;

            if (displayPersonality != null || hasUniqueBehavior)
            {
                AddSection(rows, _npcCollapsedSections, "Living Preferences", () =>
                {
                    if (displayPersonality != null)
                        rows.Add(new BodyRow { PersonalityTable = displayPersonality, Height = PrefTableHeight });
                    if (hasUniqueBehavior)
                    {
                        AddWrappedRows(rows, "Also has unique living-preference behavior beyond simple "
                            + "biome/neighbor rules (e.g. Princess-style NPCs) — not fully captured here.", width);
                    }
                });
            }

            if (entry.IsBoss)
            {
                var wiki = _data.GetBossInfo(entry.Name);
                if (wiki != null)
                {
                    rows.Add(new BodyRow { Suffix = "", Height = 8 });

                    if (!string.IsNullOrEmpty(wiki.Summoning))
                    {
                        AddSection(rows, _npcCollapsedSections, "Summoning & Spawning", () => AddWrappedRows(rows, wiki.Summoning, width));
                        rows.Add(new BodyRow { Suffix = "", Height = 6 });
                    }

                    if (!string.IsNullOrEmpty(wiki.ExpertMode))
                    {
                        AddSection(rows, _npcCollapsedSections, "Expert Mode", () => AddWrappedRows(rows, wiki.ExpertMode, width));
                        rows.Add(new BodyRow { Suffix = "", Height = 6 });
                    }

                    if (!string.IsNullOrEmpty(wiki.Aftermath))
                    {
                        AddSection(rows, _npcCollapsedSections, "Aftermath", () => AddWrappedRows(rows, wiki.Aftermath, width));
                        rows.Add(new BodyRow { Suffix = "", Height = 6 });
                    }

                    if (!string.IsNullOrEmpty(wiki.Tips))
                    {
                        AddSection(rows, _npcCollapsedSections, "Tips", () => AddWrappedRows(rows, wiki.Tips, width));
                        rows.Add(new BodyRow { Suffix = "", Height = 6 });
                    }
                }
            }

            int contentHeight = rows.Sum(r => r.Height);
            _npcDetailScroll.Begin(x, y, width, height, contentHeight);

            int rowY = 0;
            foreach (var row in rows)
            {
                if (_npcDetailScroll.IsVisible(rowY, row.Height))
                {
                    int drawY = _npcDetailScroll.ContentY + rowY;
                    DrawNpcBodyRow(x, drawY, width, row);
                }
                rowY += row.Height;
            }

            _npcDetailScroll.End();
        }

        private const int PrefTableHeaderHeight = 20;
        private const int PrefTableRowPadding = 8; // top+bottom padding around each row's text

        // Computed properties, not static fields — both depend on UIRenderer.TextLineHeight,
        // which reads real font measurement, so they need to stay live rather than get baked in
        // once at class-load time (also sidesteps the static-init ordering class of bug entirely).
        private static int PrefTableRowHeight => UIRenderer.TextLineHeight + PrefTableRowPadding * 2;
        private static int PrefTableHeight => PrefTableHeaderHeight + PrefTableRows.Length * PrefTableRowHeight;

        private static readonly (string Label, Affection Level, Color4 Bg)[] PrefTableRows =
        {
            (Affection.Love.ToString() + "s", Affection.Love, new Color4(45, 80, 45)),
            (Affection.Like.ToString() + "s", Affection.Like, new Color4(80, 80, 45)),
            (Affection.Dislike.ToString() + "s", Affection.Dislike, new Color4(90, 65, 35)),
            (Affection.Hate.ToString() + "s", Affection.Hate, new Color4(90, 45, 45)),
        };

        /// <summary>Loves/Likes/Dislikes/Hates x Biome/Neighbor, matching the wiki's own Living Preferences table layout. Neighbor names are clickable, navigating to that NPC's own page.</summary>
        private void DrawPreferenceTable(int x, int y, int width, NpcPersonalityEntry personality)
        {
            int col2 = x + width / 2;

            UIRenderer.DrawText("Biome", x + 4, y, UIColors.TextDim);
            UIRenderer.DrawText("Neighbor", col2 + 4, y, UIColors.TextDim);

            int rowY = y + PrefTableHeaderHeight;
            foreach (var row in PrefTableRows)
            {
                UIRenderer.DrawRect(x, rowY, width, PrefTableRowHeight - 2, row.Bg);
                int textY = rowY + PrefTableRowPadding;
                UIRenderer.DrawText(row.Label, x + 4, textY, UIColors.TextTitle);

                string biomeText = string.Join(", ", personality.Biomes.Where(p => p.Level == row.Level).Select(p => p.Label));
                UIRenderer.DrawText(string.IsNullOrEmpty(biomeText) ? "n/a" : biomeText, x + 70, textY, UIColors.Text);

                var neighborNames = personality.Neighbors.Where(p => p.Level == row.Level).Select(p => p.Label).ToList();
                if (neighborNames.Count == 0)
                    UIRenderer.DrawText("n/a", col2 + 4, textY, UIColors.Text);
                else
                    DrawClickableNameList(neighborNames, col2 + 4, textY);

                rowY += PrefTableRowHeight;
            }
        }

        /// <summary>Draws a comma-separated list of NPC names, each individually clickable to jump to that NPC's own page (plain text if the name doesn't match a known NPC).</summary>
        private void DrawClickableNameList(List<string> names, int x, int y)
        {
            int curX = x;
            for (int i = 0; i < names.Count; i++)
            {
                string suffix = i < names.Count - 1 ? ", " : "";
                int nameWidth = TextUtil.MeasureWidth(names[i]);

                var target = _allNpcs.FirstOrDefault(n => n.Name == names[i]);
                bool isHover = target != null && WidgetInput.IsMouseOver(curX, y - 2, nameWidth, PrefTableRowHeight - 4);

                UIRenderer.DrawText(names[i], curX, y, isHover ? UIColors.AccentText : UIColors.Text);
                curX += nameWidth;

                if (isHover)
                {
                    Tooltip.Set("View " + names[i]);
                    if (WidgetInput.MouseLeftClick)
                    {
                        OpenNpcDetail(target);
                        WidgetInput.ConsumeClick();
                    }
                }

                if (suffix.Length > 0)
                {
                    UIRenderer.DrawText(suffix, curX, y, UIColors.Text);
                    curX += TextUtil.MeasureWidth(suffix);
                }
            }
        }

        private void DrawNpcBodyRow(int x, int y, int width, BodyRow row)
        {
            if (row.PersonalityTable != null)
            {
                DrawPreferenceTable(x, y, width, row.PersonalityTable);
                return;
            }

            if (row.Item != null && row.ShopCost.HasValue)
            {
                bool isShopRowHover = WidgetInput.IsMouseOver(x, y, width, RowHeight - 4);
                if (isShopRowHover)
                    UIRenderer.DrawRect(x, y, width, RowHeight - 4, UIColors.InputFocusBg);

                UIRenderer.DrawItem(row.Item.Id, x, y, 28, 28);
                UIRenderer.DrawText(row.Item.Name, x + 34, y + 8, isShopRowHover ? UIColors.AccentText : UIColors.Text);
                int shopNameWidth = TextUtil.MeasureWidth(row.Item.Name);
                DrawCoinValue(x + 34 + shopNameWidth + 16, y + 5, row.ShopCost.Value);

                if (isShopRowHover)
                {
                    ItemTooltip.Set(row.Item.Id);
                    if (WidgetInput.MouseLeftClick)
                    {
                        _section.SwitchTo(EncyclopediaSection.Items);
                        _navigation.Home();
                        OpenDetail(row.Item);
                        WidgetInput.ConsumeClick();
                    }
                }
                return;
            }

            if (row.SectionKey != null)
            {
                bool collapsed = row.CollapseState.Contains(row.SectionKey);
                bool isHover = WidgetInput.IsMouseOver(x, y, width, RowHeight);
                if (isHover) UIRenderer.DrawRect(x, y, width, RowHeight, UIColors.SectionBg);

                string arrow = collapsed ? "> " : "v ";
                UIRenderer.DrawText(arrow + row.Header, x, y + 8, UIColors.TextTitle);

                if (isHover && WidgetInput.MouseLeftClick)
                {
                    WidgetInput.ConsumeClick();
                    if (collapsed) row.CollapseState.Remove(row.SectionKey);
                    else row.CollapseState.Add(row.SectionKey);
                }
                return;
            }

            if (row.Header != null)
            {
                // Non-collapsible plain heading (currently just "Description").
                UIRenderer.DrawText(row.Header, x, y + 8, UIColors.TextTitle);
                return;
            }

            if (row.Item == null)
            {
                UIRenderer.DrawText(row.Suffix, x + 4 + row.IndentX, y + 4, UIColors.TextDim);
                return;
            }

            bool isRowHover = WidgetInput.IsMouseOver(x, y, width, DropRowHeight - 4);
            if (isRowHover)
                UIRenderer.DrawRect(x, y, width, DropRowHeight - 4, UIColors.InputFocusBg);

            UIRenderer.DrawItem(row.Item.Id, x, y, 28, 28);
            UIRenderer.DrawText(row.Item.Name, x + 34, y + 4, isRowHover ? UIColors.AccentText : UIColors.Text);
            if (row.Suffix != null)
                DrawWrappedText(row.Suffix, x + 34, y + 24, width - 34, UIColors.TextDim);

            if (isRowHover)
            {
                ItemTooltip.Set(row.Item.Id);
                if (WidgetInput.MouseLeftClick)
                {
                    // Cross-section navigation: jump straight to the item's own detail page.
                    _section.SwitchTo(EncyclopediaSection.Items);
                    _navigation.Home();
                    OpenDetail(row.Item);
                    WidgetInput.ConsumeClick();
                }
            }
        }

        // ── Events section ───────────────────────────────────────────────────
        // No live base data exists for events at all (confirmed via reflection — only Old One's
        // Army has a dedicated NPCID.Sets array; every other event's enemy roster and triggers
        // are imperative spawn logic, same pattern as shop contents and neighbor preferences), so
        // this section is wiki-sourced end to end rather than a live-catalogue-plus-enrichment
        // merge like Items/NPCs.

        private void FilterEvents()
        {
            _filteredEvents = EncyclopediaQueries.FilterEvents(_events, _eventSearch.Text).ToList();
            _eventScroll.ResetScroll();
        }

        private static readonly string[] DefaultCollapsedEventSections = { "Completing", "Notes", "Tips" };

        private void OpenEventDetail(EventWikiInfo evt)
        {
            _eventDetailItem = evt;
            _eventView = ViewMode.Detail;
            _eventDetailScroll.ResetScroll();

            _eventCollapsedSections.Clear();
            foreach (var key in DefaultCollapsedEventSections) _eventCollapsedSections.Add(key);
        }

        private const int EventRowHeight = 36;

        private void DrawEventListView()
        {
            var s = new StackLayout(_panel.ContentX, _panel.ContentY + SectionToggleHeight, _panel.ContentWidth);

            int searchY = s.Advance(28);
            _eventSearch.Draw(s.X, searchY, 200, 26);
            if (_eventSearch.HasChanged) FilterEvents();
            s.Advance(10);

            int listX = s.X;
            int listY = s.CurrentY;
            int listWidth = s.Width;
            int listHeight = _panel.ContentHeight - SectionToggleHeight - s.TotalHeight;

            int contentHeight = _filteredEvents.Count * EventRowHeight;
            _eventScroll.Begin(listX, listY, listWidth, listHeight, contentHeight);

            int rowY = 0;
            foreach (var evt in _filteredEvents)
            {
                if (_eventScroll.IsVisible(rowY, EventRowHeight))
                {
                    int drawY = _eventScroll.ContentY + rowY;
                    bool isHover = WidgetInput.IsMouseOver(listX, drawY, listWidth, EventRowHeight - 4);
                    if (isHover)
                        UIRenderer.DrawRect(listX, drawY, listWidth, EventRowHeight - 4, UIColors.InputFocusBg);

                    UIRenderer.DrawText(evt.Name, listX + 8, drawY + 8, isHover ? UIColors.AccentText : UIColors.Text);

                    if (isHover && WidgetInput.MouseLeftClick)
                    {
                        OpenEventDetail(evt);
                        WidgetInput.ConsumeClick();
                    }
                }
                rowY += EventRowHeight;
            }

            _eventScroll.End();

            if (_filteredEvents.Count == 0)
                UIRenderer.DrawText("No events found", listX + 5, listY + 10, UIColors.Warning);
        }

        private void DrawEventDetailView()
        {
            var evt = _eventDetailItem;
            if (evt == null) { GoHome(); return; }

            int x = _panel.ContentX + DetailPadX;
            int y = _panel.ContentY + SectionToggleHeight;
            int width = _panel.ContentWidth - DetailPadX * 2;

            DrawNavBar(x, y, width, GoHome, GoHome, false); // events don't cross-navigate to each other
            y += NavBarHeight + 6;

            UIRenderer.DrawText(evt.Name, x, y, UIColors.TextTitle);
            y += 32;

            int bodyHeight = _panel.ContentHeight - (y - _panel.ContentY);
            DrawEventBody(x, y, width, bodyHeight, evt);
        }

        private void DrawEventBody(int x, int y, int width, int height, EventWikiInfo evt)
        {
            var rows = new List<BodyRow>();

            // Summoning is a plain non-collapsible heading, like NPC Description — it's the
            // single most useful field on a first read ("how do I actually start this").
            if (!string.IsNullOrEmpty(evt.Summoning))
            {
                rows.Add(new BodyRow { Header = "Summoning", Height = RowHeight });
                AddWrappedRows(rows, evt.Summoning, width);
                rows.Add(new BodyRow { Suffix = "", Height = 8 });
            }

            if (!string.IsNullOrEmpty(evt.Completing))
            {
                AddSection(rows, _eventCollapsedSections, "Completing", () => AddWrappedRows(rows, evt.Completing, width));
                rows.Add(new BodyRow { Suffix = "", Height = 6 });
            }

            if (!string.IsNullOrEmpty(evt.Notes))
            {
                AddSection(rows, _eventCollapsedSections, "Notes", () => AddWrappedRows(rows, evt.Notes, width));
                rows.Add(new BodyRow { Suffix = "", Height = 6 });
            }

            if (!string.IsNullOrEmpty(evt.Tips))
            {
                AddSection(rows, _eventCollapsedSections, "Tips", () => AddWrappedRows(rows, evt.Tips, width));
                rows.Add(new BodyRow { Suffix = "", Height = 6 });
            }

            if (rows.Count == 0)
                rows.Add(new BodyRow { Suffix = "No wiki data available for this event.", Height = RowHeight });

            int contentHeight = rows.Sum(r => r.Height);
            _eventDetailScroll.Begin(x, y, width, height, contentHeight);

            int rowY = 0;
            foreach (var row in rows)
            {
                if (_eventDetailScroll.IsVisible(rowY, row.Height))
                {
                    int drawY = _eventDetailScroll.ContentY + rowY;
                    DrawNpcBodyRow(x, drawY, width, row);
                }
                rowY += row.Height;
            }

            _eventDetailScroll.End();
        }
    }
}
