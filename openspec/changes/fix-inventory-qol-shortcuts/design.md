# Design: Fix inventory QoL shortcuts

The current host uses reflection against `Player` for all three QoL actions.
Terraria 1.4.5.8 does not define those three methods on `Player`: sorting is
implemented by the public static methods `Terraria.UI.ItemSorting.SortInventory`
and `Terraria.UI.ItemSorting.SortChest`, while nearby quick-stack is exposed by
the public instance method `Player.QuickStackAllChests()`.

The host will call those APIs directly. Direct references are preferable here:
the host already compiles against the exact supported Terraria and FNA
assemblies, and a missing API becomes a build-time compatibility failure rather
than a silent runtime no-op. The existing `InventoryQolController` remains
responsible for edge detection, state guards, and diagnostic reporting.

Verification uses the focused QoL test project and the macOS host Release build;
the user can then test only `I`, `O`, and `K` in a live single-player session.
