## Context

See `proposal.md` for motivation. The existing mod targets `net48`, directly reads Terraria's live managed state, and uses TerrariaModder for lifecycle, keybinding, logging, and UI. Terraria 1.4.5.8 ships a managed `Terraria.exe` on every desktop platform; Linux and macOS launch it through bundled Mono/FNA runtimes, while the current Windows integration uses a managed injector and XNA. The installed macOS copy includes a universal x64/arm64 runtime, FNA, and native libraries.

TerrariaModder Core is MIT-licensed, but TerrariaInjector is GPL-3.0. This change may adapt narrowly selected TerrariaModder UI implementation with preserved MIT notices, but it will not copy or derive source from TerrariaInjector.

## Goals / Non-Goals

**Goals:**

- Create a deep host module whose small feature interface hides platform bootstrap, Harmony patching, input edge detection, rendering state, logging, and failure isolation.
- Run the same first-party feature set against Windows XNA and Unix FNA builds.
- Preserve a normal, untouched Steam launch and the existing Windows path until parity is demonstrated.
- Make Boss Checklist and later first-party QoL features additions to the feature registry rather than new loader work.

**Non-Goals:**

- Third-party plugin discovery, binary compatibility, dependency resolution, or sandboxing.
- TerrariaModder custom items, multiplayer administration, server management, or generic mod configuration.
- A website or external desktop application.
- Shipping Terraria-owned files.
- Implementing Boss Checklist within this change.

## Decisions

### Use a first-party feature seam instead of porting TerrariaModder

The host exposes one lifecycle interface plus a context containing logging, keybinding, input, and rendering modules. Features are registered at compile time. The Encyclopedia is the first adapter; Boss Checklist will make the seam real with a second adapter in its own change.

This keeps the interface small and gives the host enough implementation depth to centralise fragile game integration. Porting all of TerrariaModder was rejected because most of its roughly 30,000-line Core—custom content, networking, server administration, generic discovery, and configuration—is unrelated to these QoL features.

### Split the bootstrap from game-referencing feature assemblies

The bootstrap does not reference Terraria, XNA, or FNA types. It locates the managed game assembly, verifies its version, establishes assembly resolution, installs lifecycle patches by reflection, and then loads the compiled first-party feature registry.

Feature and rendering assemblies use platform build flavours because Windows XNA and Unix FNA expose compatible namespaces through different assembly identities. A shared source set produces platform-specific binaries rather than hiding every rendering operation behind reflection.

### Reuse Terraria's Unix runtime through a supported bootstrap mechanism

Linux and macOS launch scripts will activate a bootstrap assembly before Terraria's entry point through the bundled Mono runtime's agent/startup facility, then invoke the original platform launcher with its normal working directory and native-library environment. A feasibility test must prove this without copying or renaming the installed game.

If the bundled runtime cannot load an external startup assembly reliably, the fallback is a small repository-built native Mono embedding launcher. Requiring a separately installed system Mono is rejected because it would introduce runtime-version and architecture mismatches, particularly on Apple Silicon.

### Implement an independent Windows bootstrap

Windows retains the current TerrariaModder path during migration. The replacement bootstrap will independently load the managed Terraria entry point and install the same host lifecycle without copying TerrariaInjector source. Harmony and Mono.Cecil may be referenced as normal third-party packages under their own licences.

### Adapt only the required MIT UI surface

The existing panel depends on TerrariaModder's renderer, draggable panel, scrolling, search, input, colours, and tooltip facilities. Required implementations may be adapted into repository-owned modules under the upstream MIT licence and recorded in third-party notices. General mod menus and unrelated widgets are excluded.

### Keep game-state reads inside feature adapters

The host owns lifecycle and presentation facilities but does not model Terraria content. The Encyclopedia continues to read live game state through its feature implementation. A future Boss Checklist feature will likewise read current world progression through its own adapter, avoiding a prematurely broad game-data interface.

## Risks / Trade-offs

- **Bundled Mono startup hooks may behave differently between Linux and macOS** → Prove bootstrap-to-main-menu on each platform before migrating UI code; retain the Windows path and normal launch as rollback.
- **XNA and FNA rendering state differs** → Build separate flavours, minimise direct framework types in shared interfaces, and add visual smoke checks for clipping, text, icons, and input.
- **Harmony targets can drift with Terraria updates** → Centralise method discovery, guard version 1.4.5.8, and fail closed before applying patches.
- **Feature exceptions occur inside the game process** → Catch at every host-to-feature call, disable the failing feature, and never patch save or world writes.
- **MIT-derived UI code needs attribution** → Preserve notices and add a repository third-party notice before distribution.
- **Maintaining multiple binaries increases release complexity** → Generate and verify manifests through one platform-aware packaging command.

## Migration Plan

1. Prove installation discovery and pre-entry bootstrap on the available Apple Silicon installation without loading features.
2. Introduce the host interface, feature registry, diagnostics, and lifecycle tests using fake features.
3. Add the Unix FNA rendering flavour and migrate the Encyclopedia on macOS.
4. Prove Linux discovery, bootstrap, FNA rendering, and packaging on a Linux installation.
5. Add the independent Windows bootstrap and XNA rendering flavour while retaining the TerrariaModder release path.
6. Run feature-parity and non-destructive-install checks across the supported matrix.
7. Remove TerrariaModder and TerrariaInjector from release artefacts only after every required platform gate passes.

Rollback at every stage is the existing TerrariaModder-based Windows build and the unmodified normal Steam launch.

## Open Questions

- Whether Intel macOS needs a separate artefact or can use the same managed FNA feature binary as Apple Silicon; this can be answered during packaging without changing the feature interface.
