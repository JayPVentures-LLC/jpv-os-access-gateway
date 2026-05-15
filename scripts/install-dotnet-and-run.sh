#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/src/JPVOS/JPVOS.csproj"
URL="${1:-http://localhost:5111}"
DOTNET_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"

install_dotnet_local() {
    if command -v dotnet >/dev/null 2>&1; then
        return
    fi

    if [ -x "$DOTNET_DIR/dotnet" ]; then
        export PATH="$DOTNET_DIR:$PATH"
        return
    fi

    if ! command -v curl >/dev/null 2>&1; then
        echo "curl is required for user-local .NET installation." >&2
        exit 1
    fi

    echo "Installing .NET 8 SDK locally into $DOTNET_DIR..."
    mkdir -p "$DOTNET_DIR" "$ROOT_DIR/.tools"
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$ROOT_DIR/.tools/dotnet-install.sh"
    bash "$ROOT_DIR/.tools/dotnet-install.sh" --channel 8.0 --install-dir "$DOTNET_DIR"
    export PATH="$DOTNET_DIR:$PATH"
}

if [ ! -f "$PROJECT" ]; then
    echo "Project not found: $PROJECT" >&2
    exit 1
fi

install_dotnet_local

echo "Using:"
dotnet --info

echo "Building init..."
dotnet build "$PROJECT"

echo "Starting init at $URL"
exec dotnet run --project "$PROJECT" --no-launch-profile --urls "$URL"
