## ADDED Requirements

### Requirement: Explicit inventory sorting

The host MUST expose an explicit inventory-sort action that delegates to Terraria-compatible item ordering and leaves favourited items in place.

#### Scenario: Sort inventory

- **WHEN** the player invokes the action with an active local inventory
- **THEN** eligible inventory items are sorted once and favourited items are not moved

### Requirement: Nearby chest quick-stack

The host MUST expose an explicit nearby-chest quick-stack action that obeys Terraria's normal range, stack compatibility, favourited-item, and multiplayer permission rules.

#### Scenario: Quick-stack nearby

- **WHEN** the player invokes the action while an accessible nearby chest is open
- **THEN** eligible compatible stacks move to that chest and protected items remain untouched

#### Scenario: No valid destination

- **WHEN** no accessible chest or local player is available
- **THEN** the action performs no item movement and reports a no-op status
