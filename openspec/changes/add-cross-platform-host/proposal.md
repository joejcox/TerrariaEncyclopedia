## Why

Terraria Encyclopedia currently inherits TerrariaModder's Windows-only runtime and release process, preventing native use on macOS. This project needs only a small first-party quality-of-life feature runtime, so a repository-owned host can provide the required lifecycle, input, and rendering integration without carrying an unrelated general-purpose mod platform. This delivery focuses on macOS; Linux and a first-party Windows replacement are deferred.

## What Changes

- Add a minimal host that launches the installed Terraria managed assembly without modifying the game installation.
- Support native macOS launch layouts, including Apple Silicon, by using the runtime and native libraries shipped with the Terraria installation.
- Replace the Encyclopedia's direct TerrariaModder lifecycle, input, logging, and UI dependencies with repository-owned modules.
- Preserve the current Events, boss, NPC preference/shop, and item acquisition additions while routing them through those repository-owned modules.
- Allow multiple compile-time registered first-party quality-of-life features to share the host; the Encyclopedia remains the initial feature and Boss Checklist is the planned first follow-up.
- Preserve the current Windows/TerrariaModder path; replacing it is outside this macOS-focused delivery.
- Add macOS build, packaging, diagnostics, and compatibility checks.
- Keep the scope limited to first-party quality-of-life features; custom content, multiplayer administration, third-party mod discovery, and other TerrariaModder features are excluded.
- Linux launch support and a repository-owned Windows bootstrap remain follow-up changes.

## Capabilities

### New Capabilities

- `cross-platform-game-host`: Locate supported desktop installations through portable discovery records and launch macOS through a non-destructive repository-owned host.
- `qol-feature-runtime`: Register first-party features and initialise, update, render, toggle, and unload them through host-owned lifecycle, input, logging, and UI facilities.
- `platform-distribution`: Build and assemble a macOS release artefact with actionable compatibility diagnostics.

### Modified Capabilities

None.

## Impact

- Affects the mod project, its UI layer, build and release documentation, and distributable layout.
- Adds host and assembly-loading dependencies directly to this repository while avoiding source reuse from the GPL-licensed TerrariaInjector.
- Requires validation against Terraria 1.4.5.8 on the currently available Apple Silicon installation; the universal game runtime keeps the design compatible with Intel macOS.
- Does not add a website, external desktop application, or Boss Checklist implementation in this change; Boss Checklist will use the feature interface in a follow-up change.
