# Design: Class-aware boss preparation

## Player class inference

The host computes a transient `PlayerClass` from Terraria's local player. Held and equipped items receive more weight than ordinary inventory items, whose contribution is capped per class so a large carried arsenal cannot override the active held weapon. Vanilla item damage flags (`melee`, `ranged`, `magic`, `summon`, and `sentry`) provide the class signal. A close score between two classes yields Hybrid; no signal yields Unknown.

The host passes the result to the feature each update. The value is read-only, never persisted, and is refreshed as the player changes loadout.

## Preparation content

`BossPreparationGuide` owns concise, static baseline goals for each boss and shared class focus text. It is keyed by checklist display name, with combined bosses resolving through their displayed name. The UI renders a collapsible `Preparation goals — <class>` section before `Summoning & Spawning` on boss detail pages.

If a boss or class is unknown, the baseline goals still render. Missing guide content must not prevent the rest of the detail page from rendering.

## Failure handling

Inference errors return Unknown. Guide lookup returns a short baseline rather than throwing. Existing feature failure isolation remains unchanged.
