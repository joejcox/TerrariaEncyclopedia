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

### Requirement: Runtime scope remains first-party
The runtime SHALL load only features compiled and registered by this repository and SHALL NOT scan for or execute third-party feature assemblies.

#### Scenario: Unrecognised assembly is placed beside the host
- **WHEN** an arbitrary assembly is added to the distribution directory
- **THEN** the runtime ignores it unless it is explicitly part of the compiled feature registry
