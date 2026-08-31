# Building from source

Only needed if you're developing the mod itself. If you just want to play
with it, see the root [`README.md`](../README.md) quick start instead.

## Why this isn't a normal "install TerrariaModder, drop in a DLL" mod

This mod uses a few `UIRenderer`/`ItemTooltip` APIs (`DrawNPC`, `DrawBuff`,
`TextLineHeight`, `ItemTooltip.DrawAt`) that don't exist in the stock
TerrariaModder Core release, and needed two real bugfixes
(`DedServProxy`'s broken `Item.NewItem` overload, and a duplicate
`0Harmony.dll` that silently broke every keybind) just to build and run
against Terraria 1.4.5.8 at all. So this project builds its own patched copy
of Core from source rather than depending on a stock release. See
[`framework-patches.md`](framework-patches.md) for exactly what's patched
and why.

## Prerequisites

- Terraria 1.4.5.8 (Steam), installed normally.
- .NET SDK (8/9/10 all fine — used for the mod, the mod-side tools, and to
  drive `dotnet build` against the framework's `net48` projects).
- .NET Framework 4.8 Developer Pack (Core and this mod both target `net48`,
  since that's what Terraria/TerrariaModder run on).
- Git.

## 1. Build the framework (one-time, or after a framework update)

```
git clone https://github.com/Inidar1/terraria-modder --branch v0.4.0
git clone https://github.com/ConfuzzedCat/TerrariaInjector --branch v1.3.0
```

Apply every patch in [`framework-patches.md`](framework-patches.md) to the
`terraria-modder` checkout, then:

```
dotnet build src/Core/TerrariaModder.Core.csproj -c Release
dotnet build TerrariaInjector.csproj -c Release   # in the TerrariaInjector checkout
```

Deploy into your own Terraria install per the checklist at the bottom of
`framework-patches.md` (this is also exactly what a release zip contains —
see [`RELEASING.md`](RELEASING.md)).

## 2. Point the mod project at your Terraria install

The mod's `.csproj` references Terraria's own assemblies and
`TerrariaModder.Core.dll` via a relative path:

```
src/TerrariaEncyclopedia/TerrariaEncyclopedia.csproj
  → ..\..\Terraria\Terraria.exe
  → ..\..\Terraria\TerrariaModder\core\TerrariaModder.Core.dll
```

So create a `Terraria` symlink (or junction) at the repo root pointing at
your real install — it's gitignored, never committed:

```
# from the repo root, PowerShell (run as admin, or use /D for a junction instead)
New-Item -ItemType SymbolicLink -Path Terraria -Target "F:\SteamLibrary\steamapps\common\Terraria"
```

## 3. Build and deploy the mod for local testing

```
dotnet build src/TerrariaEncyclopedia/TerrariaEncyclopedia.csproj -c Release
```

Output lands directly at `build/plugins/TerrariaEncyclopedia.dll` (the
`.csproj`'s `OutputPath`) — copy that, `manifest.json`, and `WikiData/*.json`
into `<Terraria install>/TerrariaModder/mods/terraria-encyclopedia/`, then
launch `TerrariaInjector.exe` and press **P**.

## The wiki scraper (optional, only needed to refresh boss/hardmode data)

`tools/WikiScraper` is a separate .NET 8 console app (not shipped with the
mod) that pulls boss infobox data and Hardmode-only item/NPC category lists
from terraria.wiki.gg and writes them to
`src/TerrariaEncyclopedia/WikiData/*.json`. Run it with:

```
dotnet run --project tools/WikiScraper
```

Re-run it only when you suspect the wiki content has changed (a Terraria
content update, or a correction on the wiki itself) — the mod reads the
committed JSON at runtime, it never hits the network itself.
