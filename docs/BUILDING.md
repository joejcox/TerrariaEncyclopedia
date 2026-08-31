# Building the macOS host

Terraria Encyclopedia supports the stock Steam macOS release of Terraria
1.4.5.8. It is a repository-owned Mono host loaded at launch time; it does not
copy, patch, or redistribute any Terraria file.

## Requirements

- macOS with the Steam version of Terraria 1.4.5.8 installed;
- .NET SDK 8 or later;
- the Terraria installation path. The usual path is
  `~/Library/Application Support/Steam/steamapps/common/Terraria`.

## Build

From the repository root, build against the installed game assemblies:

```sh
dotnet build src/TerrariaEncyclopedia.Host.Mono/TerrariaEncyclopedia.Host.Mono.csproj -c Release \
  -p:TerrariaReferencePath="$HOME/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/Terraria.exe" \
  -p:FnaReferencePath="$HOME/Library/Application Support/Steam/steamapps/common/Terraria/Terraria.app/Contents/Resources/FNA.dll"
```

Run the release-layout check before packaging:

```sh
dotnet run --project tools/Packager -- --verify-only \
  --terraria-path "$HOME/Library/Application Support/Steam/steamapps/common/Terraria"
```

The checker validates the optional installation path but never places any game
file in the release archive.
