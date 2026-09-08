# Proposal: Restore macOS keyboard input

Fix ordinary keyboard input for the macOS host so feature shortcuts and search typing receive the same key snapshot as Terraria's own input pipeline.

## Scope

- Read the pressed-key snapshot through Terraria's public input helper.
- Retain the FNA keyboard state only as a narrow compatibility fallback.
- Preserve existing edge-triggered shortcut handling, Escape closing, and text-entry behaviour.
- Avoid changing Terraria files, player state, or world state.

## Out of scope

- Changing shortcut bindings or adding new shortcuts.
- Replacing Terraria's input system or modifying the installed game binary.
- Linux input support.
