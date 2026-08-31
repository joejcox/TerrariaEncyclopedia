# Cutting a macOS release

The macOS archive is a self-contained, project-owned launcher overlay. It is
not extracted into Terraria and contains no game binaries, FNA, legacy Windows
loaders, or TerrariaModder files.

## Build and verify

Build the Mono host as described in [BUILDING.md](BUILDING.md), then run:

```sh
dotnet test tests/TerrariaEncyclopedia.Packager.Tests/TerrariaEncyclopedia.Packager.Tests.csproj
dotnet run --project tools/Packager -- --verify-only \
  --terraria-path "$HOME/Library/Application Support/Steam/steamapps/common/Terraria"
```

## Assemble

```sh
dotnet run --project tools/Packager -- \
  --terraria-path "$HOME/Library/Application Support/Steam/steamapps/common/Terraria" \
  --output artifacts/TerrariaEncyclopedia-macos.zip
```

The archive contains the project host, its project-built runtime and feature
assemblies, wiki data, a macOS launcher, the README, and third-party notices.
`tools/Packager` rejects any unapproved entry, including `Terraria.exe`,
`FNA.dll`, `TerrariaInjector.exe`, Harmony, Cecil, or TerrariaModder paths.

## Compatibility sign-off

Follow the full [macOS compatibility matrix](compatibility-matrix.md) before
publishing. It covers the arm64 smoke checks, the x64 suitability boundary,
archive inspection, unchanged game-file hashes, and a normal Steam launch
after the bounded host checks have ended.

Do not run a smoke check while Terraria is open. It refuses to share a running
game process; close Terraria first, then let the script start and terminate its
own bounded process.

Publish the zip and its versioned release notes. Users unpack it anywhere
outside their Terraria installation and run `launch-macos.sh`. They can pass
`--terraria-path` when Steam is installed in a non-standard location.
