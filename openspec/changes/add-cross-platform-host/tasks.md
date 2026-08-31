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

- [ ] T04 Add the required renderer, widgets, tooltip support, FNA/XNA build flavours, and third-party attribution
  - Depends on: T03
  - Writes: src/TerrariaEncyclopedia.Runtime/UI/**, src/TerrariaEncyclopedia.Runtime/TerrariaEncyclopedia.Runtime.csproj, THIRD_PARTY_NOTICES.md, tests/TerrariaEncyclopedia.Runtime.Tests/UI/**
  - Locks: dependency-manifest
  - Estimate: large
  - Acceptance: The runtime builds against the Unix FNA and Windows XNA reference flavours and tests cover input edges, clipping state restoration, scrolling, and layout calculations without importing unrelated TerrariaModder modules.
  - Verify: dotnet test tests/TerrariaEncyclopedia.Runtime.Tests/TerrariaEncyclopedia.Runtime.Tests.csproj && dotnet build src/TerrariaEncyclopedia.Runtime/TerrariaEncyclopedia.Runtime.csproj -c Release -p:GameFramework=FNA

## 3. Feature Migration and Platforms

- [ ] T05 Migrate Terraria Encyclopedia to the host feature interface and verify behavioural parity on macOS
  - Depends on: T02, T03, T04
  - Writes: src/TerrariaEncyclopedia/**, tests/TerrariaEncyclopedia.Tests/**
  - Locks: macos-terraria-process
  - Estimate: large
  - Acceptance: The Encyclopedia builds without TerrariaModder, opens through the host on Terraria 1.4.5.8, and retains item, NPC, boss, recipe, drop, research, search, sort, navigation, tooltip, and toggle behaviour.
  - Verify: dotnet test tests/TerrariaEncyclopedia.Tests/TerrariaEncyclopedia.Tests.csproj && tests/smoke/macos-encyclopedia.sh --terraria-path "/Users/josephanson/Library/Application Support/Steam/steamapps/common/Terraria"

- [ ] T06 Add Linux discovery, bundled-Mono bootstrap, FNA packaging, and smoke-test support
  - Depends on: T02, T03
  - Writes: scripts/launch-linux.sh, tests/smoke/linux-bootstrap.sh, src/TerrariaEncyclopedia.Host/Linux/**
  - Locks: linux-terraria-process
  - Estimate: medium
  - Acceptance: A native Linux x64 Terraria installation can be detected and launched with the bootstrap through its bundled runtime without Wine, Proton, or modified game files.
  - Verify: tests/smoke/linux-bootstrap.sh --terraria-path "$TERRARIA_LINUX_PATH"

- [ ] T07 Add the independent Windows bootstrap and XNA feature build while retaining the legacy fallback
  - Depends on: T03, T04
  - Writes: src/TerrariaEncyclopedia.Host.Windows/**, scripts/launch-windows.ps1, tests/smoke/windows-bootstrap.ps1
  - Locks: dependency-manifest, windows-terraria-process
  - Estimate: large
  - Acceptance: Windows Terraria 1.4.5.8 launches with the repository-owned feature host and XNA build without using TerrariaInjector source; the existing TerrariaModder bundle remains available as rollback until matrix acceptance.
  - Verify: pwsh -File tests/smoke/windows-bootstrap.ps1 -TerrariaPath "$env:TERRARIA_WINDOWS_PATH"

## 4. Distribution and Acceptance

- [ ] T08 Add platform release assembly, manifests, licence checks, and updated build/release documentation
  - Depends on: T05, T06, T07
  - Writes: tools/Packager/**, docs/BUILDING.md, docs/RELEASING.md, README.md, tests/TerrariaEncyclopedia.Packager.Tests/**
  - Locks: dependency-manifest, release-layout
  - Estimate: medium
  - Acceptance: Windows, Linux, and macOS archives contain only project and permitted third-party files, accept optional installation paths, and contain no Terraria-owned files.
  - Verify: dotnet test tests/TerrariaEncyclopedia.Packager.Tests/TerrariaEncyclopedia.Packager.Tests.csproj && dotnet run --project tools/Packager -- --verify-only

- [ ] T09 Run the supported-platform compatibility matrix, confirm normal Steam launches remain unaffected, and retire legacy release dependencies
  - Depends on: T08
  - Writes: docs/compatibility-matrix.md, docs/RELEASING.md
  - Locks: macos-terraria-process, linux-terraria-process, windows-terraria-process, release-layout
  - Estimate: large
  - Acceptance: Windows x64, Linux x64, macOS x64, and macOS arm64 pass bootstrap and Encyclopedia parity checks; game-file hashes remain unchanged; release documentation removes TerrariaModder and TerrariaInjector only after all gates pass.
  - Verify: dotnet test && dotnet run --project tools/Packager -- --verify-only
