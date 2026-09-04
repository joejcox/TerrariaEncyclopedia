## Design

The QoL feature stays behind the existing first-party feature interface. It delegates item movement to Terraria's native `Player` and chest operations where available, rather than reimplementing stack rules. It must fail closed when the game menu is active, no local player is available, or a multiplayer operation is not permitted.

Actions are edge-triggered and debounced. Inventory sorting never moves favourited items. Nearby quick-stack only targets chests within Terraria's normal interaction range and leaves favourited items and incompatible stacks untouched. No action writes directly to world or player files.

The UI shows the two actions only while the inventory or a chest is open, and reports a short completion/no-op status without blocking normal Terraria input.

