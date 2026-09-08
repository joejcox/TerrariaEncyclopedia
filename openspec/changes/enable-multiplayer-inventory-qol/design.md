# Design: Enable multiplayer inventory QoL shortcuts

The host runs as a local Terraria client, so `Main.netMode == 1` is a valid
execution context. Terraria's native APIs already branch on network mode:

- `ItemSorting.SortInventory()` performs the same local inventory operation as
  the vanilla inventory button.
- `ItemSorting.SortChest()` sends normal chest slot changes when the client is
  connected.
- `Player.QuickStackAllChests()` delegates to `QuickStacking`, which sends the
  appropriate quick-stack packets in multiplayer.

Remove only the `Main.netMode == 0` checks. Keep the local-player and UI-state
guards so actions cannot run on the menu, without a player, without an open
normal chest, or without the inventory open for quick-stack. Dedicated-server
execution remains impossible because this host requires the client renderer.
