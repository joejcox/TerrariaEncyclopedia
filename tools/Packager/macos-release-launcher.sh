#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<'USAGE'
Usage: ./launch-macos.sh [--terraria-path PATH] [--] [TERRARIA_ARGUMENT ...]

Launches the original macOS Terraria application with Terraria Encyclopedia
loaded before the game entry point. It never modifies the Terraria install.
USAGE
}

release_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
terraria_path="$HOME/Library/Application Support/Steam/steamapps/common/Terraria"
terraria_arguments=()

while (($# > 0)); do
    case "$1" in
        --terraria-path)
            (($# >= 2)) || { echo "--terraria-path requires a value." >&2; exit 2; }
            terraria_path=$2
            shift 2
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        --)
            shift
            terraria_arguments=("$@")
            break
            ;;
        *)
            terraria_arguments+=("$1")
            shift
            ;;
    esac
done

launcher="$terraria_path/Terraria.app/Contents/MacOS/Terraria"
runtime="$terraria_path/Terraria.app/Contents/MacOS/Terraria.bin.osx"
managed_assembly="$terraria_path/Terraria.app/Contents/Resources/Terraria.exe"
host_assembly="$release_dir/TerrariaEncyclopedia.Host.Mono.exe"

for required_path in "$launcher" "$runtime" "$managed_assembly" "$host_assembly"; do
    [[ -f "$required_path" ]] || { echo "Required launch file not found: $required_path" >&2; exit 1; }
done

[[ -x "$launcher" && -x "$runtime" ]] || { echo "The Terraria launcher is not executable: $terraria_path" >&2; exit 1; }

if [[ "$host_assembly" == *[[:space:]:]* ]]; then
    echo "The release folder path cannot contain whitespace or a colon." >&2
    exit 1
fi

agent_option="--agent=$host_assembly"
export MONO_BUNDLED_OPTIONS="${MONO_BUNDLED_OPTIONS:+$MONO_BUNDLED_OPTIONS }$agent_option"

exec "$launcher" "${terraria_arguments[@]}"
