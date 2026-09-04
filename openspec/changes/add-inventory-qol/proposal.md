## Why

Inventory management is repetitive during play. The repository-owned host can provide small, explicit actions without porting the rest of TerrariaModder.

## What changes

- Add a read-only-safe inventory sort action using Terraria's native sorting behaviour.
- Add nearby-chest quick-stack using Terraria's own range, stack, favourited-item, and multiplayer rules.
- Expose both actions through configurable host keybinds and visible tooltips.

## Non-goals

- No arbitrary item spawning, deletion, chest creation, save-file editing, or map changes.
- No automatic sorting on every update; actions occur only on explicit input.

