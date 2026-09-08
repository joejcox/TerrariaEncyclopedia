# macOS keyboard input

## ADDED Requirements

### Requirement: capture after Terraria processes input
The macOS host SHALL process one input snapshot after Terraria's normal input handling for each gameplay update, while retaining a menu path for screens where Terraria returns before its internal tick.

#### Scenario: gameplay key press
- **WHEN** Terraria handles a key press during a gameplay update
- **THEN** the feature receives that key in the same update's snapshot

#### Scenario: main-menu update
- **WHEN** Terraria is on a menu screen that returns before its internal tick
- **THEN** the feature still receives its menu snapshot through the menu-compatible callback

### Requirement: capture the authoritative Terraria key snapshot
The macOS host SHALL use Terraria's public pressed-key helper as the per-frame keyboard snapshot and SHALL only use FNA keyboard state when that helper fails or is unavailable.

#### Scenario: released keys do not remain pressed
- **WHEN** a key is released between two Terraria updates
- **THEN** the next input snapshot no longer contains that key

#### Scenario: compatibility fallback
- **WHEN** Terraria's pressed-key helper throws or returns no collection
- **THEN** the host reads FNA's current keyboard state and continues without disabling the feature

### Requirement: deliver shortcuts and text entry
The host SHALL deliver ordinary key presses to the existing feature runtime without changing their configured bindings.

#### Scenario: feature shortcuts
- **WHEN** the user presses P, I, O, or K
- **THEN** the configured panel or inventory QoL action receives the key edge

#### Scenario: search typing
- **WHEN** the item browser search field has focus and the user types
- **THEN** the typed characters update the search query

#### Scenario: close with Escape
- **WHEN** the feature is open and the user presses Escape
- **THEN** the feature closes as before
