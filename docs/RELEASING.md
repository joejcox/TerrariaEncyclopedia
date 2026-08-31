# Cutting a release

What a friend downloads from the Releases page has to include this
project's own **patched** TerrariaModder Core, not just the mod DLL — see
[`BUILDING.md`](BUILDING.md) for why. So the release zip is a small overlay
of the whole modded install, meant to be extracted straight into someone's
Terraria folder.

## Versioning

Releases from here on are **alpha** — this is still early, actively-changing
software with a small, known set of users who understand that. Tag and
title releases accordingly (e.g. `v0.9.0-alpha`), and say so plainly in the
release notes, until told otherwise.

## 1. Bump the version

- `src/TerrariaEncyclopedia/EncyclopediaFeature.cs` → `FeatureVersion = "X.Y.Z"`
- `src/TerrariaEncyclopedia/manifest.json` → `"version": "X.Y.Z"`

## 2. Build

```
dotnet build src/Core/TerrariaModder.Core.csproj -c Release          # in your framework checkout
dotnet build src/TerrariaEncyclopedia/TerrariaEncyclopedia.csproj -c Release
```

## 3. Assemble the zip

Stage these files (paths relative to a Terraria install root) into an empty
folder, then zip the folder's *contents* (not the folder itself):

```
TerrariaInjector.exe
0Harmony.dll
Mono.Cecil.dll
Mono.Cecil.Mdb.dll
Mono.Cecil.Pdb.dll
Mono.Cecil.Rocks.dll
TerrariaModder/core/TerrariaModder.Core.dll
TerrariaModder/core/config.ini
TerrariaModder/core/config.json
TerrariaModder/core/Docs/*
TerrariaModder/mods/terraria-encyclopedia/TerrariaEncyclopedia.dll
TerrariaModder/mods/terraria-encyclopedia/manifest.json
TerrariaModder/mods/terraria-encyclopedia/WikiData/*.json
```

Deliberately **excluded**:

- `TerrariaModder/core/identity.json` — a per-install random ID the
  framework generates itself on first run; shipping the maintainer's own ID
  would make every recipient's install identical, which defeats its purpose.
- `TerrariaModder/core/logs/` — runtime output, not install state.
- Anything under `TerrariaModder/core/deps/` — must stay empty (see
  `framework-patches.md`, patch #2); including a stray `0Harmony.dll` there
  reintroduces the "no keybind works" bug.

Name the zip `TerrariaEncyclopedia-vX.Y.Z.zip`.

## 4. Publish

Create a GitHub Release on this repo tagged `vX.Y.Z`, attach the zip, and
paste in what changed. Point people at the root `README.md` quick start for
install steps rather than repeating them in the release notes.

## On updates reaching people automatically

There's no auto-update today — deliberately: this is a small-audience,
read-only tool, so a "check Releases occasionally" model is enough, and
avoids the complexity/risk of a mod silently pulling and running new code.
If that ever stops being enough, the natural next step is a scheduled job
that re-runs `tools/WikiScraper`, diffs its output against what's committed,
and opens a PR (not an auto-publish — scraped wiki content should get a
human glance before shipping) when the wiki has changed; a separate check
comparing the detected Terraria version against `GameVersionGuard` would
flag when the game itself has moved on, since that needs API
re-verification before a new build ships, not just new data. The mod could
then check the Releases API on startup and show an in-panel "update
available" notice — still no auto-install, consistent with the mod being
read-only end to end.
