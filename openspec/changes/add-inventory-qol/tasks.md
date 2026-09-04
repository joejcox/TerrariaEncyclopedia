## 1. Native action seam

- [ ] T01 Add a testable QoL action interface and native Terraria adapters for inventory sorting and nearby-chest quick-stack
  - Depends on: none
  - Writes: src/TerrariaEncyclopedia.Qol/**, tests/TerrariaEncyclopedia.Qol.Tests/**
  - Locks: dependency-manifest
  - Estimate: medium
  - Acceptance: explicit actions invoke native-compatible sorting/stacking rules, are no-ops in menus or without a valid player, and never move favourited items
  - Verify: dotnet test tests/TerrariaEncyclopedia.Qol.Tests/TerrariaEncyclopedia.Qol.Tests.csproj

## 2. Host integration

- [ ] T02 Integrate QoL actions with host keybinds, UI affordances, and failure isolation
  - Depends on: T01
  - Writes: src/TerrariaEncyclopedia.Host.Mono/**, src/TerrariaEncyclopedia/**, tests/TerrariaEncyclopedia.Tests/**
  - Locks: macos-terraria-process
  - Estimate: medium
  - Acceptance: users can invoke both actions through visible buttons or configurable keybinds; actions are edge-triggered, status is visible, and feature failures do not stop the Encyclopaedia
  - Verify: dotnet test tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj && dotnet build src/TerrariaEncyclopedia.Host.Mono/TerrariaEncyclopedia.Host.Mono.csproj -c Release -p:TerrariaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/Terraria.exe" -p:FnaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/FNA.dll"

