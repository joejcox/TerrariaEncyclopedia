# Patches applied to the vendored TerrariaModder framework

`TerrariaModder` (Core + Injector) is built from source into a staging area
(`D:\TerrariaMods\_staging\`, not part of this repo) rather than installed via
the Vault, per `project-plan.md`. Three real bugs were found via live testing
against this exact Terraria 1.4.5.8 build and fixed with narrow, local patches
to that staged source. If the framework is ever rebuilt from a fresh clone of
`Inidar1/terraria-modder` (tag `v0.4.0`) or `ConfuzzedCat/TerrariaInjector`
(tag `v1.3.0`), these need to be reapplied.

## 1. `src/Core/Server/DedServProxy.cs` — broken `Item.NewItem` overload

Core's `TerrariaModder.Core.csproj` fails to compile against installed
Terraria 1.4.5.8: a direct call `Terraria.Item.NewItem(null, pos, width,
height, itemId, stack, false, prefix)` no longer matches any overload
(`CS1503` on args 2/7/8) — Re-Logic changed the signature since this framework
version was last verified. Confirmed via git history: no upstream fix exists
past the `v0.4.0` tag.

**Fix**: removed the broken direct call in `ItemNewItem(...)`, falling through
to the reflection-based path the same method already uses for dedicated-server
mode (resolves parameters dynamically, unaffected by the signature drift).
Irrelevant to the Encyclopedia (dedicated-server item spawning), so the
narrowest possible fix.

## 2. Duplicate `0Harmony.dll` in `TerrariaModder/core/deps/`

Symptom: ~15 of Core's own Harmony/MonoMod transpiler patches failed at
runtime (`MonoMod.Utils.Cil...violates the constraint of type 'TTarget'`),
including `KeybindUpdatePatch` — the *only* mechanism that calls
`KeybindManager.Update()` every frame. With that patch failing, **no keybind
could fire at all**, not even Core's own F6 mod menu.

Root cause: `TerrariaInjector`'s `GM.Inject()` (`Program.cs`) explicitly
`Assembly.UnsafeLoadFrom()`s a *second* copy of `0Harmony.dll` from
`TerrariaModder/core/deps/`, separate from the copy already loaded next to
`TerrariaInjector.exe` for its own compiled `using HarmonyLib;` reference. In
.NET Framework, the `LoadFrom` context and the default `Load` context don't
unify even for byte-identical files, so Harmony's internal MonoMod types end
up with two separate type identities in the same process.

**Fix**: don't ship `0Harmony.dll` inside `TerrariaModder/core/deps/` at
deploy time — the injector's own `DependencyResolveEventHandler` already
checks already-loaded assemblies first, so Core's Harmony calls resolve back
to the single instance the injector already loaded. This is a **deployment
step, not a source change** — see the deploy checklist below.

## 3. `src/Core/UI/UIRenderer.cs` — `DrawItem` renders blurry (bilinear, not point-filtered)

Symptom: item icons in a mod's UI render smeared/blurry (color bleed at
edges), while `DrawText` right next to them renders crisp, in the same draw
pass. Confirmed visually (close-up screenshot showed real bilinear smearing,
not just "bigger but crisp" pixel scaling) and by direct comparison to
vanilla's own crisp inventory-slot rendering.

Root cause: `DrawItem` draws directly into whatever ambient `SpriteBatch`
sampler state happens to be active, with no attempt to force point/nearest
filtering. `DrawText` already works around this exact class of bug elsewhere
in the same file (comment: *"parameterless Begin() defaults to LinearClamp ->
blurry text"*) — `DrawItem` never got the same treatment.

**Fix**: inside `DrawItem`, when not already nested in an active `BeginClip`
region (which explicitly refuses to nest and manages its own sampler), end
the current batch, begin a new one with `SamplerState.PointClamp`, draw the
item, then end and restore the batch with whatever sampler was active before
(mirrors the existing `BeginClip`/`EndClip` save-restore technique in the same
file).

## 4. `src/Core/UI/Widgets/ItemTooltip.cs` — no way to embed the full tooltip inline

`ItemTooltip` only exposed `Set()` + `DrawDeferred()`, which renders the built
tooltip lines as a floating popup positioned near the mouse cursor — no public
way to get the built lines, or to render them at a fixed position, for
embedding full item stats (damage, use time, knockback, defense, flavor text)
directly into a mod's own UI layout instead of only on hover.

**Fix**: extracted the existing word-wrap loop out of the private
`RenderLines()` into a shared `BuildWrappedLines(maxContentWidth)` helper (used
by both), and added a new public `DrawAt(itemType, x, y, maxWidth, prefix,
stack)` that builds the same tooltip lines and draws them at an explicit
position, returning the height consumed so callers can lay out content below
it. Zero changes to the existing hover-tooltip behavior (`Set()`/
`DrawDeferred()`/`RenderLines()` untouched apart from the extraction).

## Deploy checklist after rebuilding the framework from source

1. `dotnet build src/Core/TerrariaModder.Core.csproj -c Release` (with patches
   #1 and #3 applied) → `TerrariaModder.Core.dll`
2. `dotnet build TerrariaInjector.csproj -c Release` (SDK-style conversion —
   see `project-plan.md` Phase 0 — Harmony 2.4.2, Mono.Cecil 0.11.6)
3. Copy `TerrariaModder.Core.dll` → `Terraria/TerrariaModder/core/`
4. Copy `TerrariaInjector.exe` + its own `0Harmony.dll`/`Mono.Cecil*.dll` →
   `Terraria/` root
5. **Do not** copy `0Harmony.dll` into `Terraria/TerrariaModder/core/deps/`
   (patch #2) — leave that folder without a Harmony copy of its own
6. Re-verify `Terraria.exe`/`TerrariaServer.exe`/`steam_api.dll` hashes against
   `docs/baseline-manifest.json` — must be unchanged
