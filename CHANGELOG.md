# Changelog

Notable changes to Terraria Encyclopedia, versioned per [`manifest.json`](src/TerrariaEncyclopedia/manifest.json).
Format loosely follows [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

### Added
- Wiki-sourced boss data for the four Lunar Event pillars (Vortex/Nebula/Solar/Stardust), found missing via a live cross-check against every real in-game boss — total boss coverage is now 24/24. Their Type/Environment/Summoning/Aftermath/Tips fields are mostly empty (known gap: they share one combined wiki page structured unlike a standalone boss page); their live game data (stats, drops, hardmode marking) is unaffected.

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
