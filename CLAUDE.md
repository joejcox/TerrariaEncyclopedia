# Working on Terraria Encyclopedia

This file is for an AI assistant (Claude Code or similar) picking up work in
this repo — most likely to add macOS support. Read this before touching
anything. It carries forward the non-negotiable rules this project was
originally built under, adapted so they apply regardless of OS or drive
letters.

## What this project is

A **read-only** in-game item/NPC/boss browser for Terraria, running as a mod
on top of [TerrariaModder](https://terrariamodder.com/) (Harmony-based
runtime injection — no game files are patched on disk). See
[`README.md`](README.md) for the full feature list and
[`docs/framework-patches.md`](docs/framework-patches.md) for exactly what's
patched in this project's fork of TerrariaModder's Core and why.

## Non-negotiable rules (apply on any OS)

- **Never touch save data.** No `.plr`/`.wld`/world-gen writes, ever. This
  mod only reads already-loaded game state. If a macOS port needs to read
  save/world data from a different location (Mac's Steam Cloud paths,
  `~/Library/...` instead of `Documents\My Games\Terraria`), that's fine to
  investigate — but the read-only invariant itself does not get relaxed.
- **Never assume — verify.** Don't invent Terraria/TerrariaModder API names,
  and don't assume Windows behavior carries over to the Mono runtime macOS
  Terraria actually runs on. Every new API this project used was checked by
  reflection against the real installed game assembly before being coded
  against (see the pattern throughout `src/TerrariaEncyclopedia/Content/`).
  Do the same against the real Mac install and the real TerrariaModder
  behavior under Mono — don't guess from the Windows source reading similar.
- **Back up before modifying anything live.** Before installing, patching,
  or injecting anything into an actual Terraria install (Mac or otherwise),
  take a verified backup of that machine's save data first.
- **Investigate before building.** If TerrariaModder's own Mono/macOS support
  turns out to be incomplete, figure out exactly what's broken and why
  before writing a fix or workaround — this project was built the same way
  (see the git history / commit messages for the original Windows
  investigation phase).

## Known state of macOS support (as of this writing)

Nothing here has been tested on macOS. What's known from upstream, not
assumed:

- TerrariaModder's own docs and build scripts are Windows-only (`.bat`
  files, `TerrariaInjector.exe`).
- Upstream `Inidar1/terraria-modder` has open issues specifically about
  Mono/macOS/Linux: `CaptureManagerGuard` reportedly crashes the game under
  Mono, and release archives ship Windows-style backslash paths that break
  on Unix filesystems. Treat both as real, unresolved problems to work
  around or fix — not as solved.
- This repo's own build (`src/TerrariaEncyclopedia/TerrariaEncyclopedia.csproj`)
  targets `net48` and references Terraria's assemblies and
  `TerrariaModder.Core.dll` via Windows-relative `HintPath`s. The mod's own
  C# (item/recipe/NPC catalog logic, UI) is plain, portable IL — it's the
  *injection/launch mechanism* (`TerrariaInjector.exe`, the patched Core) that
  is Windows-specific, not the mod's own logic.

## How to avoid breaking the Windows side while doing this

- `master` is what versioned Windows releases are cut from (see
  [`docs/RELEASING.md`](docs/RELEASING.md)) — don't commit
  platform-untested changes directly to it. Work in a branch
  (e.g. `mac-support`) and open a PR so the maintainer can review before
  merging.
- Keep macOS-specific mechanics (launcher, any Mono-specific Core patches)
  isolated from `src/TerrariaEncyclopedia/**`'s shared logic rather than
  branching that code by OS inline. If the mod's own C# needs a genuine
  cross-platform fix (not a hack), that's welcome back in `master` via a
  normal PR — it's the launch/injection side that needs to stay separate.
- Keep `Mod.cs`'s `Version`, `manifest.json`'s `"version"`, and the mod's
  `id` (`terraria-encyclopedia`) in sync with whatever `master` has —
  don't fork them for a platform-specific build.
- If TerrariaModder Core itself needs different patches for Mono than the
  ones documented in `docs/framework-patches.md`, document them the same
  way (what broke, root cause, the narrowest fix) rather than replacing that
  file's Windows-specific content.
