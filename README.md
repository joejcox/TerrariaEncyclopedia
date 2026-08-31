# Terraria Encyclopedia

A read-only, in-game item / NPC / boss browser for Terraria **1.4.5.8**
(Steam). Press a key, search or browse, click through to a full detail page —
recipes, a recursive crafting tree, real drop-rate data, sell prices, buffs,
and wiki-sourced boss info (type, environment, summoning, aftermath, tips).

It never touches your saves. Everything it shows is read live from
already-loaded game state (items, recipes, NPC drop tables, your Journey
Mode research progress) — no `.plr`/`.wld` file is ever written, and nothing
about world generation, spawning, or gameplay is changed.

It runs on top of [TerrariaModder](https://terrariamodder.com/), a
Harmony-based injector that loads mods alongside `Terraria.exe` without
patching any game files on disk. Your normal Steam launch of Terraria is
completely unaffected either way.

## Quick start (just want to use it)

1. Grab the latest `TerrariaEncyclopedia-vX.Y.Z.zip` from this repo's
   Releases page.
2. Close Terraria and Steam if they're open.
3. Extract the zip's contents **directly into your Terraria install folder**
   — the same folder that contains `Terraria.exe`. On Steam that's usually
   `...\steamapps\common\Terraria`. Merge folders when asked; nothing in the
   zip overwrites `Terraria.exe` itself.
4. Launch **`TerrariaInjector.exe`** (in that same folder) instead of
   launching Terraria through Steam. Play normally — worlds, characters,
   and Steam Cloud all work exactly as before.
5. In-game, press **P** to open or close the Encyclopedia. Use the
   Items/NPCs toggle at the top, search, sort, and category tabs to browse;
   click anything to open its detail page.

Launching Terraria through Steam as usual still works and is unmodified —
the injector is a separate, optional way to launch with mods loaded.

If Terraria updates to a version this build doesn't recognize, the mod logs
a warning and disables itself rather than doing anything unpredictable;
Terraria keeps running normally. Check the Releases page for an updated
build in that case.

## What it shows

- **Items** — search/filter/sort by name, rarity, ID, or material "set";
  category tabs (weapons, armor, accessories, tools, consumables, blocks,
  misc). Detail page: the same stat tooltip Terraria shows on hover, sell
  price (with real coin icons), buff granted (icon + tooltip + duration),
  projectile created, Journey Mode research count, crafting station, direct
  recipe, a recursive crafting tree of what each ingredient is itself made
  from, and what other items use this one.
- **NPCs & Bosses** — search/filter/sort, town/enemy/boss tabs, stats, real
  drop-rate percentages (sorted highest-chance first) including conditions
  like "Downed Plantera," and — for the 20 vanilla bosses — wiki-sourced
  type, environment, knockback resistance, immunities, summoning method,
  Expert Mode changes, aftermath, and tips.
- **Hardmode marking** for items/NPCs that are Hardmode-exclusive.

## For developers

Building the mod (and the framework it depends on) from source, and cutting
a new release bundle, are documented separately so this file stays a
quick-start:

- [`docs/BUILDING.md`](docs/BUILDING.md) — set up the toolchain, build
  TerrariaModder + this mod from source, deploy locally for testing.
- [`docs/RELEASING.md`](docs/RELEASING.md) — assemble and version the
  distributable zip referenced above.
- [`docs/framework-patches.md`](docs/framework-patches.md) — the specific
  patches this project's fork of TerrariaModder's Core carries, and why.
