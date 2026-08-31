## Purpose

Run multiple compile-time registered first-party quality-of-life features through a small, stable lifecycle and shared in-game presentation interface.

## ADDED Requirements

### Requirement: First-party features have a shared lifecycle
The runtime SHALL initialise, update, render, and unload each compile-time registered feature through a host-owned interface.

#### Scenario: Normal feature lifecycle
- **WHEN** Terraria reaches the required game lifecycle stages
- **THEN** each enabled feature receives lifecycle notifications in registration order exactly once per applicable stage

#### Scenario: Game shutdown
- **WHEN** Terraria shuts down or the host unloads
- **THEN** every initialised feature is given an opportunity to release its subscriptions and resources

### Requirement: Features share host facilities
The runtime SHALL provide registered features with host-owned logging, keybinding, input, and in-game rendering facilities without requiring a TerrariaModder dependency.

#### Scenario: Feature keybinding
- **WHEN** a feature registers a toggle key and the user presses it
- **THEN** the runtime invokes that feature's action once for the key transition

#### Scenario: Feature interface is visible
- **WHEN** a feature displays an interactive panel
- **THEN** the runtime supplies mouse, keyboard, clipping, text, and Terraria icon rendering needed by that panel

### Requirement: Feature failures do not corrupt Terraria
The runtime SHALL isolate feature initialisation and lifecycle callback failures, disable the failing feature, and allow Terraria and unaffected features to continue where safe.

#### Scenario: Feature throws during initialisation
- **WHEN** one registered feature fails to initialise
- **THEN** the runtime logs the failure, disables that feature, and continues initialising other features

#### Scenario: Feature throws during update or rendering
- **WHEN** an enabled feature throws from a lifecycle callback
- **THEN** the runtime disables further callbacks for that feature and Terraria continues running where the host can safely recover

### Requirement: Existing Encyclopedia behaviour is preserved
The migrated Encyclopedia SHALL retain its current item, NPC, boss, recipe, drop, research, search, sort, navigation, and toggle behaviour on the supported Terraria version.

#### Scenario: Encyclopedia parity
- **WHEN** the user opens the migrated Encyclopedia in Terraria 1.4.5.8
- **THEN** the information and interactions available in version 0.8.0 remain available without TerrariaModder

### Requirement: Encyclopedia interaction follows Terraria UI scaling
The migrated Encyclopedia SHALL use the same logical UI coordinate space as Terraria for pointer input, viewport bounds, deferred tooltips, and the foreground cursor redraw.

#### Scenario: Scaled pointer interaction
- **WHEN** Terraria's UI scale is 150% and the user moves or clicks across the Encyclopedia
- **THEN** hover and click targets remain aligned with the visible pointer throughout the viewport

#### Scenario: Scaled tooltip and cursor presentation
- **WHEN** a scaled pointer hovers an Encyclopedia control near either side or edge of the viewport
- **THEN** the tooltip stays adjacent to that pointer, remains inside the logical viewport, and the foreground cursor redraw overlaps Terraria's native cursor

#### Scenario: Tab interaction feedback
- **WHEN** the pointer hovers or selects an Items, NPCs, item-category, or NPC-category tab
- **THEN** the tab presents a visible hover or selected highlight without changing its hit target

### Requirement: Current Encyclopedia content remains available through the first-party host
The migrated Encyclopedia SHALL expose its current event, boss, NPC preference, NPC shop, and item acquisition information through repository-owned feature and data-source interfaces.

#### Scenario: Event catalogue
- **WHEN** the user selects the Events section and searches or opens an event
- **THEN** the matching event list and its summoning, completion, notes, and tips information remain available

#### Scenario: Boss catalogue coverage
- **WHEN** the user selects the Bosses NPC category
- **THEN** all 33 wiki-recognised bosses, including event and invasion bosses not flagged by Terraria, are classified as bosses

#### Scenario: NPC preferences and shops
- **WHEN** the user opens a town NPC with preference or shop data
- **THEN** its living preferences and current shop inventory are displayed and linked NPC or item rows navigate to their details

#### Scenario: Item acquisition information
- **WHEN** the user opens an item sold by an NPC or dropped by an enemy
- **THEN** its seller and drop-source information is displayed without exposing unresolved projectile localisation keys

#### Scenario: Weapon critical chance
- **WHEN** an ordinary melee, ranged, or magic weapon has no explicit critical-chance bonus
- **THEN** its tooltip includes Terraria's implicit four-percent base critical chance while summon items remain unchanged

#### Scenario: Existing interaction behaviour survives content integration
- **WHEN** the user moves between Items, NPCs, and Events at a non-default UI scale
- **THEN** only the active section owns text entry and the scaled pointer, tooltip, cursor, hover, and selected-tab behaviour remains aligned

### Requirement: Runtime scope remains first-party
The runtime SHALL load only features compiled and registered by this repository and SHALL NOT scan for or execute third-party feature assemblies.

#### Scenario: Unrecognised assembly is placed beside the host
- **WHEN** an arbitrary assembly is added to the distribution directory
- **THEN** the runtime ignores it unless it is explicitly part of the compiled feature registry
