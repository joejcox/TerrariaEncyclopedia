#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<'USAGE'
Usage: scripts/launch-macos.sh [--terraria-path PATH] [--host-assembly PATH]
                               [--host-argument ARG] [--] [TERRARIA_ARGUMENT ...]

Launches the original macOS Terraria launcher with the repository-owned Mono
host loaded before Terraria's entry point. The Terraria installation is never
modified. When omitted, PATH is the standard per-user Steam installation.
USAGE
}

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repository_root=$(cd "$script_dir/.." && pwd)
terraria_path="$HOME/Library/Application Support/Steam/steamapps/common/Terraria"
host_assembly="$repository_root/src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/TerrariaEncyclopedia.Host.Mono.exe"
host_argument=""
terraria_arguments=()

while (($# > 0)); do
    case "$1" in
        --terraria-path)
            (($# >= 2)) || { echo "--terraria-path requires a value." >&2; exit 2; }
            terraria_path=$2
            shift 2
            ;;
        --host-assembly)
            (($# >= 2)) || { echo "--host-assembly requires a value." >&2; exit 2; }
            host_assembly=$2
            shift 2
            ;;
        --host-argument)
            (($# >= 2)) || { echo "--host-argument requires a value." >&2; exit 2; }
            host_argument=$2
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

for required_path in "$launcher" "$runtime" "$managed_assembly" "$host_assembly"; do
    if [[ ! -f "$required_path" ]]; then
        echo "Required launch file not found: $required_path" >&2
        exit 1
    fi
done

if [[ ! -x "$launcher" || ! -x "$runtime" ]]; then
    echo "The installed macOS Terraria launcher or runtime is not executable: $terraria_path" >&2
    exit 1
fi

if [[ "$host_assembly" == *[[:space:]:]* ]]; then
    echo "The Mono host assembly path cannot contain whitespace or a colon: $host_assembly" >&2
    exit 1
fi

if [[ "$host_argument" == *[[:space:]]* ]]; then
    echo "The Mono host argument cannot contain whitespace: $host_argument" >&2
    exit 1
fi

agent_option="--agent=$host_assembly"
if [[ -n "$host_argument" ]]; then
    agent_option+=":$host_argument"
fi

if [[ -n "${MONO_BUNDLED_OPTIONS:-}" ]]; then
    export MONO_BUNDLED_OPTIONS="$MONO_BUNDLED_OPTIONS $agent_option"
else
    export MONO_BUNDLED_OPTIONS="$agent_option"
fi

if ((${#terraria_arguments[@]} == 0)); then
    exec "$launcher"
fi

exec "$launcher" "${terraria_arguments[@]}"
