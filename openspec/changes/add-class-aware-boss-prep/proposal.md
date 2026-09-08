# Proposal: Class-aware boss preparation

Add a read-only preparation section to every vanilla boss detail page. The section combines the boss's existing summon/progression data with practical goals tailored to the current player's inferred class.

The player's class is inferred from the active loadout (held item, armour, and inventory damage-class flags). Terraria does not expose one authoritative class value, so mixed or empty loadouts are reported as Hybrid or Unknown. No character or world data is written.

## Scope

- Infer Melee, Ranged, Magic, Summoner, Hybrid, or Unknown from the local player loadout.
- Show class-specific preparation goals for every boss checklist entry.
- Keep the existing summoning, drops, and progression sections intact.
- Fail closed when no local player or compatible item metadata is available.

## Out of scope

- Automatically changing equipment, crafting items, or modifying saves.
- Multiplayer synchronisation or recommendations from external services.
