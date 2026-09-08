# Multiplayer inventory QoL

## ADDED Requirements

### Requirement: allow local multiplayer clients
The macOS host SHALL allow a local multiplayer Terraria client to invoke the
existing inventory QoL actions through Terraria's native APIs.

#### Scenario: sort inventory in multiplayer
- **WHEN** the inventory-sort key edge is received by a local multiplayer
  client
- **THEN** the host invokes Terraria's native inventory sorter

#### Scenario: sort a normal chest in multiplayer
- **WHEN** the chest-sort key edge is received while a normal world chest is
  open on a local multiplayer client
- **THEN** the host invokes Terraria's native chest sorter and leaves network
  synchronisation to Terraria

#### Scenario: quick-stack in multiplayer
- **WHEN** the quick-stack key edge is received with the inventory open and a
  nearby chest available on a local multiplayer client
- **THEN** the host invokes the local player's native quick-stack action

### Requirement: preserve context safety
The host SHALL retain its menu, local-player, open-chest, inventory-open, and
edge-trigger guards in multiplayer.

#### Scenario: invalid multiplayer context
- **WHEN** a shortcut is pressed in a context that fails its existing guard
- **THEN** the action remains unavailable and no network operation is issued
