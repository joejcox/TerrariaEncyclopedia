# Terraria Encyclopedia

A read-only, in-game item / NPC / boss / event browser for Terraria **1.4.5.8**
(Steam). Press a key, search or browse, click through to a full detail page —
recipes, a recursive crafting tree, real drop-rate data, sell prices, buffs,
and wiki-sourced boss info (type, environment, summoning, aftermath, tips).

It never touches your saves. Everything it shows is read live from
already-loaded game state (items, recipes, NPC drop tables, your Journey
Mode research progress) — no `.plr`/`.wld` file is ever written, and nothing
about world generation, spawning, or gameplay is changed.

On macOS, it runs through a repository-owned Mono host that loads before the
game entry point without patching any game file on disk. Your normal Steam
launch of Terraria remains unaffected.

## Quick start (just want to use it)

1. Grab the latest `TerrariaEncyclopedia-macos.zip` from this repo's
   Releases page.
2. Extract it anywhere outside the Terraria installation, for example
   `~/Applications/TerrariaEncyclopedia`.
3. In Terminal, run `./launch-macos.sh`. The default Steam location is used;
   pass `--terraria-path /path/to/Terraria` for another library.
4. Play normally — worlds, characters, and Steam Cloud are unchanged.
5. In-game, press **P** to open or close the Encyclopedia. Use the
   Items/NPCs/Events tabs at the top, search, sort, and category tabs to browse;
   click anything to open its detail page.

Launching Terraria through Steam as usual still works and is unmodified. The
release includes no Terraria-owned file and does not replace anything in the
game folder.

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
  from, drop and seller sources whether or not the item is craftable, and what
  other items use this one.
- **NPCs & Bosses** — search/filter/sort, town/enemy/boss tabs, stats, real
  drop-rate percentages (sorted highest-chance first) including conditions
  like "Downed Plantera," town-NPC shops and living preferences, and — for
  all 33 wiki-recognised bosses — wiki-sourced
  type, environment, knockback resistance, immunities, summoning method,
  Expert Mode changes, aftermath, and tips.
- **Events** — search 13 invasions, moons and world events, with wiki-sourced
  summoning, completion, notes and tips.
- **Hardmode marking** for items/NPCs that are Hardmode-exclusive.

Town-NPC shop conditions are cached per browsing session: reopening the
Encyclopedia clears the cache, the first item seller lookup captures all known
shops, and entering an NPC detail page refreshes that NPC's inventory. This
keeps detail pages current without rebuilding a shop on each rendered frame.

## For developers

Building the mod (and the framework it depends on) from source, and cutting
a new release bundle, are documented separately so this file stays a
quick-start:

- [`docs/BUILDING.md`](docs/BUILDING.md) — build the macOS host from source.
- [`docs/RELEASING.md`](docs/RELEASING.md) — verify and assemble the
  distributable macOS archive.
- [`CHANGELOG.md`](CHANGELOG.md) — what changed in each version.
