# Patches applied to the vendored TerrariaModder framework

`TerrariaModder` (Core + Injector) is built from source into a staging area
(`D:\TerrariaMods\_staging\`, not part of this repo) rather than installed via
the Vault, per `project-plan.md`. Three real bugs were found via live testing
against this exact Terraria 1.4.5.8 build and fixed with narrow, local patches
to that staged source. If the framework is ever rebuilt from a fresh clone of
`Inidar1/terraria-modder` (tag `v0.4.0`) or `ConfuzzedCat/TerrariaInjector`
(tag `v1.3.0`), these need to be reapplied.

Two of the fixes below (#4's `lineHeight` fix, and #5) are cosmetic/feature
additions rather than compile/runtime bugs, but live in the same staged
source and need the same reapply treatment.

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

Also fixed in the same file: both `RenderLines()` and `DrawAt()` had a
hardcoded `int lineHeight = 16;` — smaller than this font's real measured
height, so embedded tooltip text rendered visibly cramped. Both now read
`UIRenderer.TextLineHeight` (see #6) instead.

## 5. `src/Core/UI/UIRenderer.cs` — no `DrawBuff` (buff icons couldn't be drawn at all)

Buff icons live in a separate texture array from item/NPC icons (`Main.buffTexture`,
not `TextureAssets.Item`/`TextureAssets.Npc`), and Core had no equivalent
drawing method — needed to show the buff an item grants inline on its detail
page.

**Fix**: added `DrawBuff(buffType, x, y, width, height, alpha)`, mirroring
`DrawItem`/`DrawNPC` exactly (same point-filter sampler fix, same
`_assetValueProp`/`_assetIsLoadedProp` reflection helpers). One difference
from those two: no lazy-load call before drawing — reflection found no
`Main.Load*Buff*` method (the much smaller, always-used buff icon set has
nothing to lazily stream), so `DrawBuff` just reads the already-loaded asset.

## 6. `src/Core/UI/UIRenderer.cs` — `TextLineHeight` (added earlier, referenced by #4 and #5's context)

`MeasureText` only ever exposed the string's pixel *width* (`Vector2.X`),
discarding the font's measured *height* (`Vector2.Y`). Every line-height
calculation in the mod was a guessed 18-20px constant, which was smaller than
the real font and caused wrapped text to overlap. Added a `TextLineHeight`
property that reads the same font measurement's `Vector2.Y`, so callers get
the real value instead of guessing.

## Deploy checklist after rebuilding the framework from source

1. `dotnet build src/Core/TerrariaModder.Core.csproj -c Release` (with patches
   #1, #3, #4, #5, #6 applied) → `TerrariaModder.Core.dll`
2. `dotnet build TerrariaInjector.csproj -c Release` (SDK-style conversion —
   see `project-plan.md` Phase 0 — Harmony 2.4.2, Mono.Cecil 0.11.6)
3. Copy `TerrariaModder.Core.dll` → `Terraria/TerrariaModder/core/`
4. Copy `TerrariaInjector.exe` + its own `0Harmony.dll`/`Mono.Cecil*.dll` →
   `Terraria/` root
5. **Do not** copy `0Harmony.dll` into `Terraria/TerrariaModder/core/deps/`
   (patch #2) — leave that folder without a Harmony copy of its own
6. Re-verify `Terraria.exe`/`TerrariaServer.exe`/`steam_api.dll` hashes against
   `docs/baseline-manifest.json` — must be unchanged
