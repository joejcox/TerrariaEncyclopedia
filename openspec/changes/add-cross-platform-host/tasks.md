## 1. Foundation

- [x] T01 Create the host solution structure, installation discovery, version validation, and unit tests
  - Depends on: none
  - Writes: Directory.Build.props, TerrariaEncyclopedia.sln, src/TerrariaEncyclopedia.Host/**, tests/TerrariaEncyclopedia.Host.Tests/**
  - Locks: dependency-manifest
  - Estimate: medium
  - Acceptance: Explicit paths and standard Steam layouts resolve to validated platform installation records; missing and unsupported installations return actionable diagnostics without launching the game.
  - Verify: dotnet test tests/TerrariaEncyclopedia.Host.Tests/TerrariaEncyclopedia.Host.Tests.csproj

## 2. Bootstrap and Feature Runtime

- [x] T02 Implement and prove the non-destructive macOS pre-entry bootstrap against the installed Apple Silicon Terraria copy
  - Depends on: T01
  - Writes: src/TerrariaEncyclopedia.Host.Mono/**, scripts/launch-macos.sh, tests/smoke/macos-bootstrap.sh
  - Locks: macos-terraria-process
  - Estimate: medium
  - Acceptance: The smoke check starts the original macOS launcher with a host marker loaded before the Terraria entry point, observes the marker, terminates the test process, and confirms the managed game assembly hash is unchanged.
  - Verify: tests/smoke/macos-bootstrap.sh --terraria-path "/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria"

- [x] T03 Implement the first-party feature registry, lifecycle, failure isolation, logging, and input interfaces with fake-feature tests
  - Depends on: T01
  - Writes: src/TerrariaEncyclopedia.Runtime/**, tests/TerrariaEncyclopedia.Runtime.Tests/**
  - Locks: none
  - Estimate: medium
  - Acceptance: Compile-time registered fake features receive ordered lifecycle callbacks and key transitions; a throwing feature is disabled without stopping healthy features.
  - Verify: dotnet test tests/TerrariaEncyclopedia.Runtime.Tests/TerrariaEncyclopedia.Runtime.Tests.csproj

- [x] T04 Add the required renderer, widgets, tooltip support, macOS FNA build flavour, and third-party attribution
  - Depends on: T03
  - Writes: src/TerrariaEncyclopedia.Runtime/UI/**, src/TerrariaEncyclopedia.Runtime/TerrariaEncyclopedia.Runtime.csproj, THIRD_PARTY_NOTICES.md, tests/TerrariaEncyclopedia.Runtime.Tests/UI/**
  - Locks: dependency-manifest
  - Estimate: large
  - Acceptance: The runtime builds against the macOS FNA reference flavour and tests cover input edges, clipping state restoration, scrolling, and layout calculations without importing unrelated TerrariaModder modules.
  - Verify: dotnet test tests/TerrariaEncyclopedia.Runtime.Tests/TerrariaEncyclopedia.Runtime.Tests.csproj && dotnet build src/TerrariaEncyclopedia.Runtime/TerrariaEncyclopedia.Runtime.csproj -c Release -p:GameFramework=FNA

## 3. Feature Migration and Platforms

- [x] T05 Migrate Terraria Encyclopedia to the host feature interface and verify behavioural parity on macOS
  - Depends on: T02, T03, T04
  - Writes: TerrariaEncyclopedia.sln, src/TerrariaEncyclopedia.Host.Mono/**, src/TerrariaEncyclopedia/**, tests/TerrariaEncyclopedia.Tests/**, tests/smoke/macos-encyclopedia.sh
  - Locks: dependency-manifest, macos-terraria-process
  - Estimate: large
  - Acceptance: The Encyclopedia builds without TerrariaModder, opens through the host on Terraria 1.4.5.8, and retains item, NPC, boss, recipe, drop, research, search, sort, navigation, tooltip, and toggle behaviour.
  - Verify: dotnet test tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj && tests/smoke/macos-encyclopedia.sh --terraria-path "/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria"

## 4. Distribution and Acceptance

- [x] T10 Align macOS pointer interaction, tooltips, cursor redraw, and tab feedback with Terraria UI scaling
  - Depends on: T05
  - Writes: src/TerrariaEncyclopedia.Host.Mono/GameHost.cs, src/TerrariaEncyclopedia/Hosting/TerrariaUiCoordinateSpace.cs, src/TerrariaEncyclopedia/UI/ItemBrowserPanel.cs, tests/TerrariaEncyclopedia.Tests/TerrariaUiCoordinateSpaceTests.cs
  - Locks: macos-terraria-process
  - Estimate: small
  - Acceptance: At 100% and 150% UI scale, hover and click targets align with the visible pointer across the viewport, tooltips stay adjacent and bounded, the foreground cursor redraw overlaps Terraria's native cursor, and section/category tabs visibly distinguish hover and selected states.
  - Verify changed: dotnet test tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj --filter FullyQualifiedName~TerrariaUiCoordinateSpaceTests
  - Verify: dotnet test tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj && dotnet build src/TerrariaEncyclopedia.Host.Mono/TerrariaEncyclopedia.Host.Mono.csproj -c Release -p:TerrariaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/Terraria.exe" -p:FnaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/FNA.dll"

- [ ] T11 Integrate current Encyclopedia content without regressing the macOS host
  - Depends on: T10
  - Writes: CHANGELOG.md, README.md, docs/RELEASING.md, docs/framework-patches.md, tools/WikiScraper/Program.cs, src/TerrariaEncyclopedia.Host.Mono/GameHost.cs, src/TerrariaEncyclopedia/Content/**, src/TerrariaEncyclopedia/TerrariaEncyclopedia.csproj, src/TerrariaEncyclopedia/UI/EncyclopediaSectionSelection.cs, src/TerrariaEncyclopedia/UI/ItemBrowserPanel.cs, src/TerrariaEncyclopedia/WikiData/**, tests/TerrariaEncyclopedia.Tests/**
  - Locks: dependency-manifest, macos-terraria-process
  - Estimate: large
  - Acceptance: The migrated macOS Encyclopedia retains the current Events section, all 33 wiki-recognised bosses, NPC living preferences and shop inventories, item acquisition sources, projectile-name and critical-chance corrections, documentation, scraper support, and wiki data while preserving scaled pointer alignment, tab feedback, exclusive text entry, and repository-owned data access.
  - Verify changed: dotnet test tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj --filter FullyQualifiedName~EncyclopediaBehaviourTests
  - Verify: dotnet test tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj && dotnet test tests/TerrariaEncyclopedia.Runtime.Tests/TerrariaEncyclopedia.Runtime.Tests.csproj && dotnet build src/TerrariaEncyclopedia.Host.Mono/TerrariaEncyclopedia.Host.Mono.csproj -c Release -p:TerrariaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/Terraria.exe" -p:FnaReferencePath="/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/FNA.dll" && tests/smoke/macos-encyclopedia.sh --terraria-path "/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria"

- [ ] T08 Add macOS release assembly, manifests, licence checks, and updated build/release documentation
  - Depends on: T11
  - Writes: tools/Packager/**, docs/BUILDING.md, docs/RELEASING.md, README.md, tests/TerrariaEncyclopedia.Packager.Tests/**
  - Locks: dependency-manifest, release-layout
  - Estimate: medium
  - Acceptance: The macOS archive contains only project and permitted third-party files, accepts an optional installation path, and contains no Terraria-owned or legacy Windows loader files.
  - Verify: dotnet test tests/TerrariaEncyclopedia.Packager.Tests/TerrariaEncyclopedia.Packager.Tests.csproj && dotnet run --project tools/Packager -- --verify-only

- [ ] T09 Run macOS compatibility checks and confirm normal Steam launches remain unaffected
  - Depends on: T08
  - Writes: docs/compatibility-matrix.md, docs/RELEASING.md
  - Locks: macos-terraria-process, release-layout
  - Estimate: large
  - Acceptance: macOS arm64 passes bootstrap and Encyclopedia parity checks, the universal archive is suitable for macOS x64, game-file hashes remain unchanged, and the macOS release contains no TerrariaModder or TerrariaInjector files.
  - Verify: dotnet test && dotnet run --project tools/Packager -- --verify-only
