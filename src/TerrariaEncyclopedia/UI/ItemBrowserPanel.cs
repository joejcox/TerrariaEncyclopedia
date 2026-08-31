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
        private enum Section { Items, Npcs }

        private const int SectionToggleHeight = 30;

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
        private bool _hardmodeOnly;
        private int _sortModeIndex;
        private bool _sortDropdownOpen;
        private int _sortButtonX, _sortButtonY, _sortButtonWidth;

        private ViewMode _view = ViewMode.Grid;
        private ItemEntry _detailItem;
        private readonly Stack<int> _history = new Stack<int>();

        private Section _section = Section.Items;

        // ── NPC section state ──
        private enum NpcTab { All, Town, Enemies, Bosses }
        private static readonly (NpcTab Tab, string Label)[] NpcTabs =
        {
            (NpcTab.All, "All"), (NpcTab.Town, "Town NPCs"), (NpcTab.Enemies, "Enemies"), (NpcTab.Bosses, "Bosses"),
        };
        private enum NpcSortMode { NameAsc, NameDesc, LifeAsc, LifeDesc }
        private static readonly (NpcSortMode Mode, string Label)[] NpcSortModes =
        {
            (NpcSortMode.NameAsc, "A-Z"), (NpcSortMode.NameDesc, "Z-A"),
            (NpcSortMode.LifeAsc, "Life: Low-High"), (NpcSortMode.LifeDesc, "Life: High-Low"),
        };

        private List<NpcEntry> _allNpcs = new List<NpcEntry>();
        private List<NpcEntry> _filteredNpcs = new List<NpcEntry>();
        private readonly TextInput _npcSearch = new TextInput("Search NPCs...", 200);
        private readonly ScrollView _npcScroll = new ScrollView();
        private readonly ScrollView _npcDetailScroll = new ScrollView();
        private int _npcActiveTab;
        private bool _npcHardmodeOnly;
        private int _npcSortModeIndex;
        private bool _npcSortDropdownOpen;
        private int _npcSortButtonX, _npcSortButtonY, _npcSortButtonWidth;
        private ViewMode _npcView = ViewMode.Grid;
        private NpcEntry _npcDetailItem;

        public ItemBrowserPanel()
        {
            _panel = new DraggablePanel("terraria-encyclopedia", "Terraria Encyclopedia", 1200, 750);
            // Same UIScale/scissor-rect mismatch found in Phase 1 — see Mod.cs history.
            _panel.ClipContent = false;
            _panel.RegisterDrawCallback(OnDraw);
            _panel.OnClose = () => { _search.Unfocus(); _npcSearch.Unfocus(); };
            _search.KeyBlockId = "terraria-encyclopedia";
            _npcSearch.KeyBlockId = "terraria-encyclopedia";
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

            if (_allNpcs.Count == 0)
            {
                _allNpcs = NpcCatalogue.Build();
                DropCatalogue.EnsureBuilt(_allNpcs);
                FilterNpcs();
            }

            GoHome();
            _panel.Open();
        }

        public void Close() => _panel.Close();

        public void Update()
        {
            if (!_panel.IsOpen) return;
            _search.Update();
            _npcSearch.Update();
        }

        public void Unload()
        {
            _panel.UnregisterDrawCallback();
            _search.Unfocus();
            _npcSearch.Unfocus();
        }

        private void Filter()
        {
            IEnumerable<ItemEntry> source = _all;

            if (_activeTab > 0)
            {
                var category = (ItemCategory)(_activeTab - 1);
                source = source.Where(e => e.Category == category);
            }

            if (_hardmodeOnly)
                source = source.Where(e => WikiEnrichment.IsHardmodeItem(e.Name));

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
            _npcDetailItem = null;
            _npcView = ViewMode.Grid;
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
                DrawSectionToggle(_panel.ContentX, _panel.ContentY, _panel.ContentWidth);

                if (_section == Section.Items)
                {
                    if (_view == ViewMode.Grid) DrawGridView();
                    else DrawDetailView();
                }
                else
                {
                    if (_npcView == ViewMode.Grid) DrawNpcGridView();
                    else DrawNpcDetailView();
                }
            }
            finally
            {
                _panel.EndDraw();
            }
        }

        private void DrawSectionToggle(int x, int y, int width)
        {
            int halfWidth = width / 2;
            DrawSectionButton(x, y, halfWidth - 2, "Items", Section.Items);
            DrawSectionButton(x + halfWidth + 2, y, width - halfWidth - 2, "NPCs", Section.Npcs);
        }

        private void DrawSectionButton(int x, int y, int width, string label, Section section)
        {
            bool isActive = _section == section;
            bool isHover = WidgetInput.IsMouseOver(x, y, width, SectionToggleHeight - 4);

            UIRenderer.DrawRect(x, y, width, SectionToggleHeight - 4,
                isActive ? UIColors.ItemActiveBg : (isHover ? UIColors.SectionBg : UIColors.InputBg));

            int textWidth = TextUtil.MeasureWidth(label);
            UIRenderer.DrawText(label, x + (width - textWidth) / 2, y + 5,
                isActive ? UIColors.AccentText : UIColors.TextDim);

            if (isHover && WidgetInput.MouseLeftClick)
            {
                WidgetInput.ConsumeClick();
                if (_section != section)
                {
                    _section = section;
                }
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
            int y = _panel.ContentY + SectionToggleHeight;
            int width = _panel.ContentWidth;

            DrawNavBar(x, y, width, GoBack, GoHome, _history.Count > 0);
            string badge = entry.IsExpertExclusive ? "EXPERT" : null;
            if (WikiEnrichment.IsHardmodeItem(entry.Name)) badge = badge == null ? "HARDMODE" : badge + "  HARDMODE";
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
                UIRenderer.DrawText("Sell: " + FormatCoins(entry.SellValue), x, y, UIColors.Text);
                y += UIRenderer.TextLineHeight;
            }

            if (entry.ResearchCount > 0)
            {
                UIRenderer.DrawText($"Research: {entry.ResearchCount} required", x, y, UIColors.Text);
                y += UIRenderer.TextLineHeight;
            }

            if (entry.BuffType > 0)
            {
                // ponytail: no buff icon here — buff textures are a separate array from item
                // icons (Main.buffTexture, not TextureAssets.Item) and Core has no DrawBuff
                // equivalent yet. Text-only for now; add a Core DrawBuff (mirroring DrawItem/
                // DrawNPC) if the icon turns out to matter.
                UIRenderer.DrawText("Grants Buff: ", x, y, LabelBlue);
                int labelW = TextUtil.MeasureWidth("Grants Buff: ");
                UIRenderer.DrawText(entry.BuffName ?? "", x + labelW, y, UIColors.Text);
                y += UIRenderer.TextLineHeight;

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

        private static string FormatCoins(int copper)
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

        // ── NPC section ──────────────────────────────────────────────────────

        private void FilterNpcs()
        {
            IEnumerable<NpcEntry> source = _allNpcs;

            switch (NpcTabs[_npcActiveTab].Tab)
            {
                case NpcTab.Town: source = source.Where(e => e.IsTownNpc); break;
                case NpcTab.Enemies: source = source.Where(e => !e.IsTownNpc && !e.IsBoss); break;
                case NpcTab.Bosses: source = source.Where(e => e.IsBoss); break;
            }

            if (_npcHardmodeOnly)
                source = source.Where(e => WikiEnrichment.IsHardmodeNpc(e.Name));

            string search = _npcSearch.Text;
            if (!string.IsNullOrEmpty(search))
            {
                string lower = search.ToLowerInvariant();
                bool isIdSearch = int.TryParse(search, out int searchId);
                source = source.Where(e => (isIdSearch && e.Id == searchId) || e.Name.ToLowerInvariant().Contains(lower));
            }

            switch (NpcSortModes[_npcSortModeIndex].Mode)
            {
                case NpcSortMode.NameDesc: source = source.OrderByDescending(e => e.Name); break;
                case NpcSortMode.LifeAsc: source = source.OrderBy(e => e.Life).ThenBy(e => e.Name); break;
                case NpcSortMode.LifeDesc: source = source.OrderByDescending(e => e.Life).ThenBy(e => e.Name); break;
                default: source = source.OrderBy(e => e.Name); break; // NameAsc
            }

            _filteredNpcs = source.ToList();
            _npcScroll.ResetScroll();
            _panel.Title = $"Terraria Encyclopedia ({_filteredNpcs.Count}/{_allNpcs.Count})";
        }

        private static readonly string[] DefaultCollapsedSections =
            { "Drops", "Summoning & Spawning", "Expert Mode", "Aftermath", "Tips" };

        private void OpenNpcDetail(NpcEntry entry)
        {
            _npcDetailItem = entry;
            _npcView = ViewMode.Detail;
            _npcDetailScroll.ResetScroll();

            _collapsedSections.Clear();
            foreach (var key in DefaultCollapsedSections) _collapsedSections.Add(key);
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
                if (isActive)
                    UIRenderer.DrawRect(tabX, y + 28, tabW, 2, UIColors.Accent);

                string label = NpcTabs[i].Label;
                int textWidth = TextUtil.MeasureWidth(label);
                UIRenderer.DrawText(label, tabX + (tabW - textWidth) / 2, y + 8,
                    isActive ? UIColors.AccentText : UIColors.TextDim);

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

            int x = _panel.ContentX;
            int y = _panel.ContentY + SectionToggleHeight;
            int width = _panel.ContentWidth;

            DrawNavBar(x, y, width, GoHome, GoHome, false); // no cross-NPC nav chain — Back == Home here
            if (WikiEnrichment.IsHardmodeNpc(entry.Name)) DrawHardmodeMarker(x, y, width);
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
                var wiki = WikiEnrichment.GetBossInfo(entry.Name);
                if (wiki != null) y += DrawBossInfoLine(x, y, width, wiki) + 6;
            }

            int bodyHeight = _panel.ContentHeight - (y - _panel.ContentY);
            DrawNpcBody(x, y, width, bodyHeight, entry);
        }

        /// <summary>
        /// Boss Type/Environment/KB Resist/Immune-to as one compact, color-coded line below the
        /// stats row — not a collapsible section, always visible. AI Type is omitted: it's just
        /// "<Boss Name> AI" for almost every boss and conveys nothing.
        /// </summary>
        private int DrawBossInfoLine(int x, int y, int maxWidth, BossWikiInfo wiki)
        {
            var segments = new List<(string Text, Color4 Color)>();
            void AddFact(string label, Color4 labelColor, string[] values)
            {
                if (values == null || values.Length == 0) return;
                if (segments.Count > 0) segments.Add((" - ", UIColors.TextDim));
                segments.Add((label + ": ", labelColor));
                segments.Add((string.Join(" | ", values), UIColors.Text));
            }

            AddFact("Type", LabelPurple, wiki.Types);
            AddFact("Environment", LabelGreen, wiki.Environments);
            if (!string.IsNullOrEmpty(wiki.KbResist)) AddFact("KB Resist", LabelTeal, new[] { wiki.KbResist });
            AddFact("Immune to", LabelRed, wiki.ImmuneTo);

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
        private int DrawWrappedText(string text, int x, int y, int maxWidth, TerrariaModder.Core.UI.Color4 color)
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
        }

        private readonly HashSet<string> _collapsedSections = new HashSet<string>();

        private void AddWrappedRows(List<BodyRow> rows, string text, int width)
        {
            foreach (var line in WrapTextLines(text, width))
                rows.Add(new BodyRow { Suffix = line.Text, IndentX = line.IndentX, Height = TextRowHeight });
        }

        /// <summary>Adds a clickable, collapsible section header, then the body rows (via addBody) unless collapsed.</summary>
        private void AddSection(List<BodyRow> rows, string key, Action addBody)
        {
            rows.Add(new BodyRow { Header = key, SectionKey = key, Height = RowHeight });
            if (!_collapsedSections.Contains(key)) addBody();
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

            var drops = DropCatalogue.DropsFor(entry.Id).OrderByDescending(d => d.Chance).ToList();
            AddSection(rows, "Drops", () =>
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

            if (entry.IsBoss)
            {
                var wiki = WikiEnrichment.GetBossInfo(entry.Name);
                if (wiki != null)
                {
                    rows.Add(new BodyRow { Suffix = "", Height = 8 });

                    if (!string.IsNullOrEmpty(wiki.Summoning))
                    {
                        AddSection(rows, "Summoning & Spawning", () => AddWrappedRows(rows, wiki.Summoning, width));
                        rows.Add(new BodyRow { Suffix = "", Height = 6 });
                    }

                    if (!string.IsNullOrEmpty(wiki.ExpertMode))
                    {
                        AddSection(rows, "Expert Mode", () => AddWrappedRows(rows, wiki.ExpertMode, width));
                        rows.Add(new BodyRow { Suffix = "", Height = 6 });
                    }

                    if (!string.IsNullOrEmpty(wiki.Aftermath))
                    {
                        AddSection(rows, "Aftermath", () => AddWrappedRows(rows, wiki.Aftermath, width));
                        rows.Add(new BodyRow { Suffix = "", Height = 6 });
                    }

                    if (!string.IsNullOrEmpty(wiki.Tips))
                    {
                        AddSection(rows, "Tips", () => AddWrappedRows(rows, wiki.Tips, width));
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

        private void DrawNpcBodyRow(int x, int y, int width, BodyRow row)
        {
            if (row.SectionKey != null)
            {
                bool collapsed = _collapsedSections.Contains(row.SectionKey);
                bool isHover = WidgetInput.IsMouseOver(x, y, width, RowHeight);
                if (isHover) UIRenderer.DrawRect(x, y, width, RowHeight, UIColors.SectionBg);

                string arrow = collapsed ? "> " : "v ";
                UIRenderer.DrawText(arrow + row.Header, x, y + 8, UIColors.TextTitle);

                if (isHover && WidgetInput.MouseLeftClick)
                {
                    WidgetInput.ConsumeClick();
                    if (collapsed) _collapsedSections.Remove(row.SectionKey);
                    else _collapsedSections.Add(row.SectionKey);
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
                    _section = Section.Items;
                    _history.Clear();
                    OpenDetail(row.Item);
                    WidgetInput.ConsumeClick();
                }
            }
        }
    }
}
