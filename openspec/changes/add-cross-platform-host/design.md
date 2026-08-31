## Context

See `proposal.md` for motivation. The existing mod targets `net48`, directly reads Terraria's live managed state, and uses TerrariaModder for lifecycle, keybinding, logging, and UI. Terraria 1.4.5.8 ships a managed `Terraria.exe` on every desktop platform; Linux and macOS launch it through bundled Mono/FNA runtimes, while the current Windows integration uses a managed injector and XNA. The installed macOS copy includes a universal x64/arm64 runtime, FNA, and native libraries.

TerrariaModder Core is MIT-licensed, but TerrariaInjector is GPL-3.0. This change may adapt narrowly selected TerrariaModder UI implementation with preserved MIT notices, but it will not copy or derive source from TerrariaInjector.

## Goals / Non-Goals

**Goals:**

- Create a deep host module whose small feature interface hides platform bootstrap, Harmony patching, input edge detection, rendering state, logging, and failure isolation.
- Run the first-party feature set against the macOS FNA build.
- Preserve a normal, untouched Steam launch and the existing Windows path.
- Make Boss Checklist and later first-party QoL features additions to the feature registry rather than new loader work.

**Non-Goals:**

- Third-party plugin discovery, binary compatibility, dependency resolution, or sandboxing.
- TerrariaModder custom items, multiplayer administration, server management, or generic mod configuration.
- A website or external desktop application.
- Shipping Terraria-owned files.
- Implementing Boss Checklist within this change.
- Linux launch, packaging, and live compatibility testing.
- Replacing the Windows bootstrap or producing a Windows XNA feature build.

## Decisions

### Use a first-party feature seam instead of porting TerrariaModder

The host exposes one lifecycle interface plus a context containing logging, keybinding, input, and rendering modules. Features are registered at compile time. The Encyclopedia is the first adapter; Boss Checklist will make the seam real with a second adapter in its own change.

This keeps the interface small and gives the host enough implementation depth to centralise fragile game integration. Porting all of TerrariaModder was rejected because most of its roughly 30,000-line Core—custom content, networking, server administration, generic discovery, and configuration—is unrelated to these QoL features.

### Split the bootstrap from game-referencing feature assemblies

The bootstrap does not reference Terraria, XNA, or FNA types. It locates the managed game assembly, verifies its version, establishes assembly resolution, installs lifecycle patches by reflection, and then loads the compiled first-party feature registry.

Feature and rendering assemblies use the macOS FNA flavour in this delivery. Their interfaces remain framework-light so later Linux FNA and Windows XNA changes do not require another feature seam.

### Reuse Terraria's macOS runtime through a supported bootstrap mechanism

The macOS launch script activates a bootstrap assembly before Terraria's entry point through the bundled Mono runtime's agent/startup facility, then invokes the original platform launcher with its normal working directory and native-library environment. A feasibility test must prove this without copying or renaming the installed game.

If the bundled runtime cannot load an external startup assembly reliably, the fallback is a small repository-built native Mono embedding launcher. Requiring a separately installed system Mono is rejected because it would introduce runtime-version and architecture mismatches, particularly on Apple Silicon.

### Defer Linux and the independent Windows bootstrap

Windows retains the current TerrariaModder path. Linux launch support and the independent Windows replacement will be specified and delivered separately after the macOS feature reaches parity. The portable discovery and feature interfaces already added here remain their starting point.

### Adapt only the required MIT UI surface

The existing panel depends on TerrariaModder's renderer, draggable panel, scrolling, search, input, colours, and tooltip facilities. Required implementations may be adapted into repository-owned modules under the upstream MIT licence and recorded in third-party notices. General mod menus and unrelated widgets are excluded.

### Keep game-state reads inside feature adapters

The host owns lifecycle and presentation facilities but does not model Terraria content. The Encyclopedia continues to read live game state through its feature implementation. A future Boss Checklist feature will likewise read current world progression through its own adapter, avoiding a prematurely broad game-data interface.

### Use Terraria's stable UI coordinate sources at every callback

Terraria changes `Main.mouseX`, `Main.mouseY`, `Main.screenWidth`, and `Main.screenHeight` between unscaled, world-scaled, and UI-scaled modes during update and draw. The host therefore derives feature input and viewport bounds from `PlayerInput`'s cached, Terraria-normalised coordinates and dimensions, then converts them once into logical UI units. It does not use raw FNA mouse coordinates or divide callback-dependent `Main` dimensions.

The foreground cursor redraw follows Terraria's own cursor pass: begin with `UIScaleMatrix`, switch Terraria input globals to UI zoom for the draw, then restore the unscaled state. This keeps the required redraw directly over the native cursor at non-default UI scales while allowing the modal to remain above Terraria's original cursor pass.

## Risks / Trade-offs

- **Bundled Mono startup hooks may drift between Terraria versions** → Prove bootstrap-to-main-menu on the supported macOS version before migrating UI code; retain the Windows path and normal launch as rollback.
- **A later XNA port may expose rendering differences** → Keep framework types behind a small UI seam and cover clipping, text, icons, and input on the macOS FNA flavour now.
- **Harmony targets can drift with Terraria updates** → Centralise method discovery, guard version 1.4.5.8, and fail closed before applying patches.
- **Feature exceptions occur inside the game process** → Catch at every host-to-feature call, disable the failing feature, and never patch save or world writes.
- **MIT-derived UI code needs attribution** → Preserve notices and add a repository third-party notice before distribution.
- **A universal macOS runtime still spans two architectures** → Keep managed artefacts architecture-neutral and verify the available arm64 path plus the archive layout.

## Migration Plan

1. Prove installation discovery and pre-entry bootstrap on the available Apple Silicon installation without loading features.
2. Introduce the host interface, feature registry, diagnostics, and lifecycle tests using fake features.
3. Add the macOS FNA rendering flavour and migrate the Encyclopedia.
4. Assemble and verify a macOS-only release artefact.
5. Run feature-parity and non-destructive-install checks on the supported macOS installation.

Linux launch support and the independent Windows replacement will be separate follow-up changes.

Rollback at every stage is the existing TerrariaModder-based Windows build and the unmodified normal Steam launch.

## Open Questions

- Whether Intel macOS needs a separate artefact or can use the same managed FNA feature binary as Apple Silicon; this can be answered during packaging without changing the feature interface.
