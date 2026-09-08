# Proposal: Enable multiplayer inventory QoL shortcuts

Allow the existing inventory sorting, chest sorting, and nearby-chest
quick-stack shortcuts to run for a local client in Terraria multiplayer.

## Scope

- Remove the unnecessary single-player-only gate from the local client adapter.
- Continue using Terraria's native APIs, which already handle client/server
  synchronisation for these operations.
- Preserve normal inventory, chest, local-player, and edge-trigger guards.
- Update the UI/documentation to describe multiplayer support.

## Out of scope

- Running actions on a dedicated server without a local player.
- Reimplementing Terraria's inventory or network protocol.
- Changing shortcut defaults.
