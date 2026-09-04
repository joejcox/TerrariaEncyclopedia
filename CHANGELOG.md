# Changelog

Notable changes to Terraria Encyclopedia, versioned per [`manifest.json`](src/TerrariaEncyclopedia/manifest.json).
Format loosely follows [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

### Added
- Wiki-sourced boss data for the four Lunar Event pillars (Vortex/Nebula/Solar/Stardust). Their Type/Environment/Summoning/Aftermath/Tips fields are mostly empty (known gap: they share one combined wiki page structured unlike a standalone boss page); their live game data (stats, drops, hardmode marking) is unaffected.
- Full boss coverage: `NpcCatalogue.IsBoss` now also recognizes 9 event/invasion bosses (Betsy, Ogre, Dark Mage, Mourning Wood, Pumpking, Everscream, Santa-NK1, Ice Queen, Flying Dutchman) that the engine's own boss/bestiary flags don't mark as bosses but the wiki does — bringing total boss coverage to 33/33, matching the wiki's "Boss NPCs" category exactly.
- Town NPC Living Preferences (Loves/Likes/Dislikes/Hates x Biome/Neighbor), using wiki data for complete tables with Terraria's live personality data as a fallback. Neighbor names are clickable, jumping straight to that NPC's page.
- Town NPC shop inventories ("Items Sold") with live item + coin-icon cost.
- Item detail pages now show "Dropped by" and "Sold by" for items with no crafting recipe, reusing the same live drop-rule and shop data — this also fixes item pages showing a generic Hardmode badge instead of the real, more precise condition (e.g. "In Master Mode").
- New top-level **Events** section (Blood Moon, invasions, Pumpkin/Frost Moon, Martian Madness, Lunar Events, Old One's Army, and more) — entirely wiki-sourced, since events have no live data source at all (only Old One's Army has a dedicated engine-level enemy list; everything else is imperative spawn logic).
- Town NPC pages now show "How to Get" (house/move-in/rescue requirements, e.g. Tavernkeep's rescue-as-Unconscious-Man-after-EoW/BoC) — this text lives only in each wiki page's unstructured intro paragraph before any heading, with no dedicated section of its own.
- Two-way chat item linking: a "Link to Chat" button on the item detail page inserts that item's chat link exactly like Alt+Click from your inventory (reuses the real `ItemTagHandler.GenerateTag`), and clicking any item link in chat (yours or another player's) now opens that item's page in the Encyclopedia. The latter needed the mod's first Harmony patch (`ChatItemLinkPatch`) — vanilla's `ItemSnippet` never wires up `OnClick()`, only `OnHover()`, even though the click-dispatch plumbing already exists and runs for every chat snippet.

### Fixed
- Weapon crit chance displayed 4 percentage points too low everywhere (the FNA host tooltip drew `item.crit` directly instead of accounting for the game's implicit +4% base, confirmed via IL disassembly of `Player.ResetEffects`).
- Shop inventories previously showed only one near-random item per NPC — `Chest.SetupShop`'s parameter turned out to be a small sequential "shop index" assigned by `NPCInteractions.Initialize()`, completely unrelated to the NPC's own type ID (e.g. Demolitionist is NPCID 38 but shop index 4). Passing the raw NPCID silently fell through to a generic near-empty branch.
- A `TypeInitializationException` could permanently break the whole panel for the rest of the game session — a static field read another static field before it was declared (fixed) that hadn't been initialized yet.
- `ProjectileName.MartianPet`-style raw, unresolved localization keys (e.g. on Cosmic Skateboard) now correctly show nothing instead of the raw key, for the handful of pet/visual-only projectiles with no registered display name.
- The Living Preferences biome column showed a raw untranslated key ("NormalUnderground") instead of a real name — the resolver needs a `"TownNPCMoodBiomes."` prefix that was missing.

## [0.8.0] - 2026-08-31

### Added
- Coin icons (platinum/gold/silver/copper) for sell price, replacing the "15g" text suffix.
- Buff icon next to granted-buff name, tooltip, and duration (new `UIRenderer.DrawBuff` in Core).
- A recursive crafting tree on the item detail page — ingredients now expand into what they're themselves crafted from, depth-capped and cycle-safe.
- Padding on item/NPC detail pages (content no longer sits flush against the panel edge).
- `README.md`, `docs/BUILDING.md`, `docs/RELEASING.md`, `CLAUDE.md` — install/build/release docs and ground rules for future contributors.

### Changed
- Boss Info (Type/Environment/KB Resist/Immune to) now renders as two grouped lines instead of one crammed line.
- Ingredient row quantity ("x50") now sits right after the item name instead of right-aligned across the panel.
- Item tooltip text (`ItemTooltip.DrawAt`/`RenderLines` in Core) uses the real measured font line height instead of a hardcoded 16px, fixing cramped line spacing.

### Fixed
- Crafting station name never rendered on the item detail page — a row-tuple bug set both a header and a suffix on the same row, and rendering returned before drawing the suffix.

## Earlier development (pre-0.8.0, unversioned in this file)

Built in phases; see `git log` for full detail. In order:
Phase 1 proof of concept → Phase 2 item browser (search/tabs/grid) → Phase 3 item
detail page (recipes, navigation) → sorting and material-set grouping → wiki-sourced
boss info and Hardmode marking → text-overlap/wrapping fixes and collapsible NPC
detail sections → live item facts (sell price, research count, buff, projectile).
