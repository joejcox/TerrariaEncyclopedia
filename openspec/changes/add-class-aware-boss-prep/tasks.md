## 1. Class inference

- [x] T01 Add transient player-class inference from the live Terraria loadout
  - Depends on: none
  - Writes: src/TerrariaEncyclopedia/Content/Models.cs, src/TerrariaEncyclopedia.Host.Mono/GameHost.cs
  - Locks: none
  - Estimate: medium
  - Acceptance: class inference recognises class flags, weights active gear, returns Hybrid/Unknown safely, and writes no game state
  - Verify: PATH="/tmp/terraria-dotnet:$PATH" dotnet build src/TerrariaEncyclopedia.Host.Mono/TerrariaEncyclopedia.Host.Mono.csproj -c Release -m:1 --no-restore -p:EnableEncyclopediaFeature=true -p:TerrariaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/Terraria.exe" -p:FnaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/FNA.dll"

## 2. Boss preparation UI

- [x] T02 Add baseline and class-specific preparation goals to boss details
  - Depends on: T01
  - Writes: src/TerrariaEncyclopedia/Content/BossPreparationGuide.cs, src/TerrariaEncyclopedia/UI/ItemBrowserPanel.cs, src/TerrariaEncyclopedia/EncyclopediaFeature.cs, src/TerrariaEncyclopedia/Content/WikiEnrichment.cs, tests/TerrariaEncyclopedia.Tests/EncyclopediaBehaviourTests.cs, tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj
  - Locks: none
  - Estimate: medium
  - Acceptance: every vanilla checklist entry displays preparation goals before summon instructions; missing data fails closed
  - Verify: PATH="/tmp/terraria-dotnet:$PATH" dotnet test tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj --no-restore
