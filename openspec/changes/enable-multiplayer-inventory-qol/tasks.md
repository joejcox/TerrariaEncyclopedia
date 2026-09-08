## 1. Multiplayer adapter

- [x] T01 Allow valid multiplayer client contexts in the Terraria QoL adapter
  - Depends on: none
  - Writes: src/TerrariaEncyclopedia.Host.Mono/GameHost.cs
  - Locks: none
  - Estimate: small
  - Acceptance: I/O/K remain guarded by their UI/local-player conditions but no
    longer require `Main.netMode == 0`
  - Verify: host Release build against Terraria 1.4.5.8

## 2. User-facing affordance

- [x] T02 Update QoL help text and focused tests/documentation for multiplayer
  - Depends on: T01
  - Writes: src/TerrariaEncyclopedia/EncyclopediaFeature.cs,
    README.md, tests/TerrariaEncyclopedia.Qol.Tests
  - Locks: none
  - Estimate: small
  - Acceptance: multiplayer support is documented and existing guard/edge tests
    remain green
  - Verify: focused QoL tests and `git diff --check`
