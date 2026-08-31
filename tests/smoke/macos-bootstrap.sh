#!/usr/bin/env bash
set -euo pipefail

expected_hash="d52c7ea7e4e312d8937fd20d1f2609f0d6bd26daddf5bfab2d106ca9077103e0"
timeout_seconds=20
terraria_path=""

usage() {
    cat <<'USAGE'
Usage: tests/smoke/macos-bootstrap.sh --terraria-path PATH
USAGE
}

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
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

if [[ -z "$terraria_path" ]]; then
    echo "--terraria-path is required for the destructive-safety smoke boundary." >&2
    exit 2
fi

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repository_root=$(cd "$script_dir/../.." && pwd)
project="$repository_root/src/TerrariaEncyclopedia.Host.Mono/TerrariaEncyclopedia.Host.Mono.csproj"
launch_script="$repository_root/scripts/launch-macos.sh"
runtime="$terraria_path/Terraria.app/Contents/MacOS/Terraria.bin.osx"
managed_assembly="$terraria_path/Terraria.app/Contents/Resources/Terraria.exe"

for required_path in "$project" "$launch_script" "$runtime" "$managed_assembly"; do
    if [[ ! -e "$required_path" ]]; then
        echo "Required smoke-test path not found: $required_path" >&2
        exit 1
    fi
done

if [[ "$(uname -s)" != "Darwin" || "$(uname -m)" != "arm64" ]]; then
    echo "This smoke check requires Apple Silicon macOS; detected $(uname -s) $(uname -m)." >&2
    exit 1
fi

find_terraria_processes() {
    pgrep -f '(^|/)Terraria\.bin\.osx([[:space:]]|$)' || true
}

if [[ -n "$(find_terraria_processes)" ]]; then
    echo "A Terraria process is already running; refusing to share the smoke-test process lock." >&2
    exit 1
fi

hash_before=$(shasum -a 256 "$managed_assembly" | awk '{print $1}')
if [[ "$hash_before" != "$expected_hash" ]]; then
    echo "Unexpected Terraria.exe SHA-256 before launch: $hash_before" >&2
    exit 1
fi

advertised_agent=$(strings "$runtime" | grep -F -- '--agent=ASSEMBLY[:ARG] Loads the specific agent assembly and executes its Main method with the given argument before loading the main assembly.' | head -n 1 || true)
if [[ -z "$advertised_agent" ]]; then
    echo "The bundled Terraria runtime does not advertise the required Mono --agent startup hook." >&2
    exit 1
fi
echo "Bundled runtime advertises: $advertised_agent"

temporary_directory=$(mktemp -d /tmp/terraria-encyclopedia-macos-smoke.XXXXXX)
marker_path="$temporary_directory/pre-entry.marker"
launch_log="$temporary_directory/launcher.log"
session_ready="$temporary_directory/session.ready"
process_group_id=""
launch_started=false

process_group_exists() {
    [[ -n "$1" ]] && kill -0 "-$1" 2>/dev/null
}

terminate_process_group() {
    local group_id=$1
    local _

    if ! process_group_exists "$group_id" && kill -0 "$group_id" 2>/dev/null; then
        /bin/kill -TERM "$group_id" 2>/dev/null || true
        for _ in {1..20}; do
            kill -0 "$group_id" 2>/dev/null || break
            sleep 0.1
        done
        if kill -0 "$group_id" 2>/dev/null; then
            /bin/kill -KILL "$group_id" 2>/dev/null || true
        fi
    fi

    if process_group_exists "$group_id"; then
        /bin/kill -TERM "-$group_id" 2>/dev/null || true
        for _ in {1..20}; do
            process_group_exists "$group_id" || break
            sleep 0.1
        done
    fi

    if process_group_exists "$group_id"; then
        /bin/kill -KILL "-$group_id" 2>/dev/null || true
    fi

    wait "$group_id" 2>/dev/null || true
    for _ in {1..20}; do
        process_group_exists "$group_id" || return 0
        sleep 0.1
    done

    ! process_group_exists "$group_id"
}

cleanup() {
    local status=$?
    local hash_at_cleanup
    trap - EXIT INT TERM
    if [[ -n "$process_group_id" ]] && ! terminate_process_group "$process_group_id"; then
        echo "The bounded Terraria process group $process_group_id did not terminate." >&2
        if ((status == 0)); then
            status=1
        fi
    fi

    if [[ "$launch_started" == true ]]; then
        hash_at_cleanup=$(shasum -a 256 "$managed_assembly" 2>/dev/null | awk '{print $1}') || hash_at_cleanup=""
        if [[ "$hash_at_cleanup" != "$hash_before" || "$hash_at_cleanup" != "$expected_hash" ]]; then
            echo "Terraria.exe safety check failed during cleanup: before=$hash_before after=${hash_at_cleanup:-unavailable}" >&2
            if ((status == 0)); then
                status=1
            fi
        fi
    fi

    rm -rf "$temporary_directory"
    exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

dotnet_command="${DOTNET:-/tmp/codex-dotnet-t01/dotnet}"
if [[ ! -x "$dotnet_command" ]]; then
    dotnet_command=$(command -v dotnet || true)
fi
if [[ -z "$dotnet_command" || ! -x "$dotnet_command" ]]; then
    echo "A .NET SDK is required to build the repository-owned Mono marker." >&2
    exit 1
fi

"$dotnet_command" build "$project" --configuration Release --artifacts-path "$temporary_directory/artifacts" --nologo >/dev/null
host_assembly=$(find "$temporary_directory/artifacts/bin" -type f -name 'TerrariaEncyclopedia.Host.Mono.exe' -print -quit)
if [[ -z "$host_assembly" ]]; then
    echo "The Mono marker assembly was not produced." >&2
    exit 1
fi

/usr/bin/python3 - "$launch_script" "$terraria_path" "$host_assembly" "$marker_path" "$launch_log" "$session_ready" <<'PY' &
import os
import sys

launch_script, terraria_path, host_assembly, marker_path, launch_log, session_ready = sys.argv[1:]
os.setsid()
with open(launch_log, "wb") as output:
    os.dup2(output.fileno(), 1)
    os.dup2(output.fileno(), 2)
    with open(session_ready, "w", encoding="utf-8") as ready:
        ready.write(str(os.getpid()))
    os.execv(
        launch_script,
        [
            launch_script,
            "--terraria-path",
            terraria_path,
            "--host-assembly",
            host_assembly,
            "--host-argument",
            marker_path,
        ],
    )
PY
process_group_id=$!
launch_started=true

session_deadline=$((SECONDS + 5))
while [[ ! -f "$session_ready" && $SECONDS -lt $session_deadline ]]; do
    if ! kill -0 "$process_group_id" 2>/dev/null; then
        echo "Could not establish the bounded Terraria process session." >&2
        exit 1
    fi
    sleep 0.05
done
if [[ ! -f "$session_ready" ]]; then
    echo "Timed out while establishing the bounded Terraria process session." >&2
    exit 1
fi

deadline=$((SECONDS + timeout_seconds))
while [[ ! -f "$marker_path" && $SECONDS -lt $deadline ]]; do
    if ! kill -0 "-$process_group_id" 2>/dev/null; then
        echo "The Terraria launcher exited before writing the host marker." >&2
        sed -n '1,160p' "$launch_log" >&2
        exit 1
    fi
    sleep 0.1
done

if [[ ! -f "$marker_path" ]]; then
    echo "Timed out after ${timeout_seconds}s waiting for the pre-entry host marker." >&2
    sed -n '1,160p' "$launch_log" >&2
    exit 1
fi

grep -Fx 'marker=terraria-encyclopedia-mono-host' "$marker_path" >/dev/null
grep -Fx 'phase=mono-agent-main' "$marker_path" >/dev/null
grep -Fx 'terraria-loaded=false' "$marker_path" >/dev/null
echo "Observed pre-entry marker:"
sed -n '1,20p' "$marker_path"

if ! terminate_process_group "$process_group_id"; then
    echo "The bounded Terraria process group $process_group_id did not terminate." >&2
    exit 1
fi
process_group_id=""

if [[ -n "$(find_terraria_processes)" ]]; then
    echo "A Terraria process remains after the bounded smoke check." >&2
    exit 1
fi

hash_after=$(shasum -a 256 "$managed_assembly" | awk '{print $1}')
if [[ "$hash_after" != "$hash_before" || "$hash_after" != "$expected_hash" ]]; then
    echo "Terraria.exe changed during smoke check: before=$hash_before after=$hash_after" >&2
    exit 1
fi

echo "Terraria.exe SHA-256 before: $hash_before"
echo "Terraria.exe SHA-256 after:  $hash_after"
echo "Bounded launcher process terminated; no Terraria process remains."
