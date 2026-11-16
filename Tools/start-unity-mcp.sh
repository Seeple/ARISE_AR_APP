#!/bin/sh
set -eu

# Keep the Python server on the same version as the pinned Unity package.
arise_uvx="${UVX_BIN:-$HOME/.local/bin/uvx}"
if [ ! -x "$arise_uvx" ]; then
    arise_uvx="$(command -v uvx || true)"
fi
if [ -z "$arise_uvx" ]; then
    echo "uvx is required. Install uv: https://docs.astral.sh/uv/getting-started/installation/" >&2
    exit 1
fi

export UNITY_MCP_DISABLE_TELEMETRY=true
export NO_PROXY="${NO_PROXY:+$NO_PROXY,}127.0.0.1,localhost,::1"
exec "$arise_uvx" --python 3.12 --from mcpforunityserver==10.3.0 \
    mcp-for-unity --transport http --http-url http://127.0.0.1:8080 \
    --default-instance ARISE_AR_APP --project-scoped-tools
