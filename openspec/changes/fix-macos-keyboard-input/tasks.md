## 1. Keyboard capture

- [x] T01 Use Terraria's authoritative key snapshot with a narrow FNA fallback
  - Depends on: none
  - Writes: src/TerrariaEncyclopedia.Host.Mono/GameHost.cs
  - Locks: none
  - Estimate: small
  - Acceptance: the host processes one post-input snapshot per gameplay update, released keys are not retained by a secondary snapshot, shortcut and text-entry key names remain unchanged, and fallback failures return an empty snapshot without disabling the host
  - Verify: PATH="/tmp/terraria-dotnet:$PATH" dotnet build src/TerrariaEncyclopedia.Host.Mono/TerrariaEncyclopedia.Host.Mono.csproj -c Release -m:1 --no-restore -p:EnableEncyclopediaFeature=true -p:TerrariaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/Terraria.exe" -p:FnaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/FNA.dll"

## 2. Runtime verification

- [x] T02 Verify the macOS host and smoke path
  - Depends on: T01
  - Writes: none
  - Locks: none
  - Estimate: small
  - Acceptance: the feature initialises and renders through the existing smoke harness without changing Terraria.exe
  - Verify: DOTNET=/tmp/terraria-dotnet/dotnet bash tests/smoke/macos-encyclopedia.sh --terraria-path "/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria"
