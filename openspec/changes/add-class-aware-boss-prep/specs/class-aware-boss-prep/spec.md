# Class-aware boss preparation

## ADDED Requirements

### Requirement: infer the active class without mutating the player
The host SHALL infer a transient class from the local player's held, equipped, and inventory items, and SHALL never write player or world state.

#### Scenario: class-bearing loadout
- **WHEN** the player has class-bearing items in the active loadout
- **THEN** the feature reports the highest-scoring class, or Hybrid when scores are close

#### Scenario: carried arsenal
- **WHEN** the inventory contains many class-bearing items but the player is holding a weapon
- **THEN** the held or equipped class remains authoritative unless the active scores are genuinely close

#### Scenario: empty or unavailable loadout
- **WHEN** no local player or class-bearing item metadata is available
- **THEN** the feature uses Unknown and continues rendering normally

### Requirement: show preparation goals for every boss
The boss detail view SHALL show a `Preparation goals — <class>` section for every vanilla boss checklist entry, before summon details, with baseline and class-specific advice.

#### Scenario: boss detail opened
- **WHEN** a user opens a boss from the Boss Checklist
- **THEN** preparation goals, summon instructions, and existing drops remain available

#### Scenario: combined checklist name
- **WHEN** a checklist entry combines alternate bosses
- **THEN** preparation content resolves from the combined display name without omitting the section

### Requirement: fail safely
Guide lookup or class inference failures SHALL degrade to Unknown/baseline text and SHALL not disable the Encyclopaedia.

#### Scenario: missing guide data
- **WHEN** guide data is missing or class inference throws
- **THEN** the detail view shows the general preparation guidance and the Encyclopaedia remains enabled
