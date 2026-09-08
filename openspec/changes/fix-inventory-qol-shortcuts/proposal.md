# Proposal: Fix inventory QoL shortcuts

Make the macOS host call the Terraria 1.4.5.8 APIs that actually implement
inventory sorting, chest sorting, and nearby-chest quick-stack actions.

## Scope

- Route `I` to Terraria's inventory sorter.
- Route `O` to Terraria's open-chest sorter.
- Route `K` to the local player's nearby-chest quick-stack action.
- Preserve the existing single-player and inventory-state guards.
- Add focused regression coverage for the host action adapters.

## Out of scope

- Changing key defaults or the `P` encyclopaedia toggle.
- Changing Terraria's installed files or save/world data.
