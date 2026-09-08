## 1. Terraria API routing

- [x] T01 Replace invalid Player reflection targets with Terraria's supported
  sorting and quick-stack APIs
  - Depends on: none
  - Writes: src/TerrariaEncyclopedia.Host.Mono/GameHost.cs
  - Locks: none
  - Estimate: small
  - Acceptance: `I`, `O`, and `K` call the correct Terraria 1.4.5.8 APIs and
    preserve existing guards
  - Verify: host Release build against installed Terraria.exe and FNA.dll

## 2. Focused verification

- [x] T02 Add or update focused QoL regression coverage
  - Depends on: T01
  - Writes: tests/TerrariaEncyclopedia.Qol.Tests and/or host tests
  - Locks: none
  - Estimate: small
  - Acceptance: edge-trigger and guard behaviour remains covered
  - Verify: dotnet test tests/TerrariaEncyclopedia.Qol.Tests/TerrariaEncyclopedia.Qol.Tests.csproj
