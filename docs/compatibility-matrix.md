# macOS compatibility matrix

Terraria Encyclopedia is distributed as a launcher overlay. The archive contains
managed, platform-neutral assemblies; it starts the stock macOS Terraria
launcher and lets its universal binary select the native architecture. It does
not modify the game installation.

| Environment | Result | Evidence |
| --- | --- | --- |
| macOS arm64, Terraria 1.4.5.8 | Supported and smoke-tested | `tests/smoke/macos-bootstrap.sh` and `tests/smoke/macos-encyclopedia.sh` start a bounded process, require the host and feature markers, terminate it, and compare `Terraria.exe` before and after. |
| macOS x64, Terraria 1.4.5.8 | Archive suitable; run the same smoke checks on an Intel Mac before calling the build runtime-tested there | The release files are managed IL and the installed `Terraria.bin.osx` is a universal x86_64/arm64 executable. The archive includes no architecture-specific native code. |
| Normal Steam launch | Unchanged by installation or release assembly | The archive is unpacked outside the game directory. Both launchers only add `MONO_BUNDLED_OPTIONS` to their own child process before `exec`ing the stock launcher; they do not write, copy, patch, or replace game files. Confirm with the release procedure below. |

## 2026-08-31 release sign-off

The supported Apple Silicon Terraria 1.4.5.8 installation completed both bounded
smoke checks. The Encyclopedia marker confirmed feature initialisation, updates,
rendering, an open panel, live item/NPC data, and tooltip creation. `Terraria.exe`
was `d52c7ea7e4e312d8937fd20d1f2609f0d6bd26daddf5bfab2d106ca9077103e0`
both before and after the checks.

A separate normal launch was then started through Steam's `steam://run/105600`
route with no `MONO_BUNDLED_OPTIONS` environment value. The stock
`Terraria.bin.osx` process started successfully, remained running through a
main-menu idle period, then quit through the application's normal quit command.
The same managed-assembly digest was confirmed afterwards.

The release packager's five focused tests and installed-path verification
passed. A freshly assembled archive was inspected: it contained exactly the
11 allow-listed release files and no Terraria, FNA, TerrariaModder,
TerrariaInjector, Harmony, or Mono.Cecil files.

## 2026-09-08 current-branch verification

The current `master` branch (`70db515`) passed the packager's five tests, the
installed-path manifest and licence check, and a fresh archive inspection with
the same 11 allow-listed files. Both bounded macOS smoke checks completed on
Apple Silicon, and `Terraria.exe` remained
`d52c7ea7e4e312d8937fd20d1f2609f0d6bd26daddf5bfab2d106ca9077103e0` before and
after each check.

A normal Steam launch via `steam://run/105600` also started successfully with
`MONO_BUNDLED_OPTIONS` unset. The process was stopped after startup verification
because this automation session does not have macOS Accessibility permission to
send the normal Cmd-Q event; the prior sign-off above includes a normal quit.

## Release evidence and checks

Use an idle Terraria installation: the smoke scripts deliberately refuse to
run when a `Terraria.bin.osx` process already exists. This prevents a release
check from interacting with a player's session.

```sh
terraria_path="$HOME/Library/Application Support/Steam/steamapps/common/Terraria"
terraria_exe="$terraria_path/Terraria.app/Contents/Resources/Terraria.exe"

# Record the game-file digest before all live checks.
shasum -a 256 "$terraria_exe"

# Supply the installed FNA and Terraria references for the FNA-only projects.
dotnet test TerrariaEncyclopedia.sln \
  -p:GameFramework=FNA \
  -p:TerrariaReferencePath="$terraria_exe" \
  -p:FnaReferencePath="$terraria_path/Terraria.app/Contents/Resources/FNA.dll"
dotnet run --project tools/Packager -- --verify-only --terraria-path "$terraria_path"
tests/smoke/macos-bootstrap.sh --terraria-path "$terraria_path"
tests/smoke/macos-encyclopedia.sh --terraria-path "$terraria_path"

# The same digest is required after the bounded checks.
shasum -a 256 "$terraria_exe"
```

The expected SHA-256 for the supported 1.4.5.8 installation is
`d52c7ea7e4e312d8937fd20d1f2609f0d6bd26daddf5bfab2d106ca9077103e0`.
A different digest is a stop condition, not a value to update casually.

## Normal Steam launch check

After the bounded smoke processes have terminated, launch Terraria normally
from Steam, reach the main menu, quit it normally, and run the final hash
command again. Do not set `MONO_BUNDLED_OPTIONS` for this check. The digest must
match the pre-check digest. This verifies that the release launcher leaves a
normal Steam launch unaffected.

## Archive inspection

Assemble a temporary release archive and inspect it before publishing:

```sh
dotnet run --project tools/Packager -- \
  --terraria-path "$terraria_path" \
  --output /tmp/TerrariaEncyclopedia-macos.zip
unzip -Z1 /tmp/TerrariaEncyclopedia-macos.zip
```

The archive verifier accepts only the manifest entries. In particular, it must
contain no `Terraria.exe`, `FNA.dll`, `TerrariaModder`, `TerrariaInjector`,
Harmony, or Mono.Cecil files.
