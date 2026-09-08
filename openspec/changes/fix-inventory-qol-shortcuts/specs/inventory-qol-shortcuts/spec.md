# Inventory QoL shortcuts

## ADDED Requirements

### Requirement: invoke Terraria's supported QoL APIs
The macOS host SHALL route each configured inventory QoL shortcut to the
corresponding public Terraria 1.4.5.8 API.

#### Scenario: sort inventory
- **WHEN** the configured inventory-sort key edge is received in a valid
  single-player session
- **THEN** the host invokes `Terraria.UI.ItemSorting.SortInventory()`

#### Scenario: sort open chest
- **WHEN** the configured chest-sort key edge is received while a chest is open
- **THEN** the host invokes `Terraria.UI.ItemSorting.SortChest()`

#### Scenario: quick-stack nearby chests
- **WHEN** the configured quick-stack key edge is received with the inventory
  open in a valid single-player session
- **THEN** the host invokes `Player.QuickStackAllChests()` for the local player

### Requirement: preserve guards and edge behaviour
The host SHALL retain the existing single-player, local-player, open-chest, and
inventory-open guards and SHALL invoke an action once per key press transition.

#### Scenario: unavailable context
- **WHEN** a QoL shortcut is pressed outside its valid Terraria context
- **THEN** no Terraria action is invoked and the host continues running

#### Scenario: held key
- **WHEN** a QoL key remains held across multiple updates
- **THEN** its action is attempted at most once until the key is released
