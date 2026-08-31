## Why

Terraria Encyclopedia currently inherits TerrariaModder's Windows-only runtime and release process, preventing native use on Linux and macOS. This project needs only a small first-party quality-of-life feature runtime, so a repository-owned host can provide the required lifecycle, input, and rendering integration without carrying an unrelated general-purpose mod platform.

## What Changes

- Add a minimal host that launches the installed Terraria managed assembly without modifying the game installation.
- Support Windows, Linux, and macOS launch layouts, including Apple Silicon, by using the runtime and native libraries shipped with each Terraria installation.
- Replace the Encyclopedia's direct TerrariaModder lifecycle, input, logging, and UI dependencies with repository-owned modules.
- Allow multiple compile-time registered first-party quality-of-life features to share the host; the Encyclopedia remains the initial feature and Boss Checklist is the planned first follow-up.
- Preserve the current Windows/TerrariaModder path until the new host reaches feature parity, then remove it from release bundles.
- Add platform-aware build, packaging, diagnostics, and compatibility checks.
- Keep the scope limited to first-party quality-of-life features; custom content, multiplayer administration, third-party mod discovery, and other TerrariaModder features are excluded.
- **BREAKING**: completed release bundles will no longer contain or require TerrariaModder or TerrariaInjector.

## Capabilities

### New Capabilities

- `cross-platform-game-host`: Locate and launch a supported Terraria installation through a non-destructive repository-owned host on Windows, Linux, and macOS.
- `qol-feature-runtime`: Register first-party features and initialise, update, render, toggle, and unload them through host-owned lifecycle, input, logging, and UI facilities.
- `platform-distribution`: Build and assemble platform-specific release artefacts with actionable compatibility diagnostics.

### Modified Capabilities

None.

## Impact

- Affects the mod project, its UI layer, build and release documentation, and distributable layout.
- Adds Harmony and assembly-loading dependencies directly to this repository while avoiding source reuse from the GPL-licensed TerrariaInjector.
- Requires validation against Terraria 1.4.5.8 on Windows, Linux, Intel macOS, and Apple Silicon macOS; the currently available Apple Silicon installation can be used for the first proof.
- Does not add a website, external desktop application, or Boss Checklist implementation in this change; Boss Checklist will use the feature interface in a follow-up change.
