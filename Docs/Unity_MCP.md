# Unity Editor and Codex MCP

## Editor

This project was imported and compiled on Apple Silicon with the installed
Unity **2022.3.34f1c1** Editor. Open `Assets/Scenes/Teleoperation.unity` after opening
the project. The project version and package lock reflect this Editor and its
Unity China package registry.

## MCP versions

- Unity package: CoplayDev MCP for Unity **10.3.0**, pinned in `Packages/manifest.json`
  to commit `81542a7e86292b5c4c21b1782c5af23b567f45ce`.
- Python server: `mcpforunityserver==10.3.0`.
- Python runtime: **3.12**, managed by `uv`.
- Local endpoint: `http://127.0.0.1:8080/mcp`.
- Codex project configuration: `.codex/config.toml`.

## Start and connect

1. Open the project in Unity.
2. Open **Window > MCP for Unity > Toggle MCP Window**.
3. Use **HTTP Local** and `http://127.0.0.1:8080`.
4. Click **Start Server** if needed, then **Connect**. The panel should show
   **Session Active (ARISE_AR_APP)**.
5. **Advanced > Auto-Start Server on Editor Load** can automate subsequent launches.
   This is a local Unity Editor preference. It has been enabled on the setup machine.
6. In Codex, trust this repository so its project configuration is loaded. Reload
   the MCP connection or start a new Codex session after initially adding the server.

The setup machine has `uv`, `uvx`, and Python 3.12 in `~/.local/bin`. On another
machine, install [uv](https://docs.astral.sh/uv/getting-started/installation/), then run
`uv python install 3.12 --default` before running the Unity dependency checks.

For a manually managed server, run from the repository root:

```sh
sh Tools/start-unity-mcp.sh
```

This helper pins the Python server version, selects `ARISE_AR_APP` by default,
disables optional server telemetry, and binds to loopback. Keep it running while
using MCP; connect Unity using the same HTTP URL. Use either this helper or Unity's
server launcher, since both use port 8080.

## Verify

```sh
codex mcp get unityMCP
curl --noproxy '*' http://127.0.0.1:8080/health
NO_PROXY=127.0.0.1,localhost,::1 uvx --python 3.12 \
  --from mcpforunityserver==10.3.0 unity-mcp instance list
```

The setup was verified by initializing an MCP session, reading the connected Unity
instance and project information, requesting script compilation with `refresh_unity`,
waiting for domain reload and reconnection, and reading the console. The result was
an idle Editor with no pending compilation or reload, the `Teleoperation` scene
loaded, and **zero console errors**. This verifies Editor script compilation;
device deployment and Quest runtime behavior require separate checks.

If a local MCP request receives a proxy error, ensure `127.0.0.1` and `localhost`
bypass your proxy. The helper and verification commands above set this explicitly.

References:
[Codex MCP configuration](https://learn.chatgpt.com/docs/extend/mcp?surface=cli),
[MCP for Unity installation](https://coplaydev.github.io/unity-mcp/getting-started/install).
