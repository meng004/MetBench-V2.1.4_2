# Windows VM Prompt: Docker Runtime MCP CLI + Runtime Environments UI

## Preconditions

- Checkout this PR branch on the Windows VM.
- Docker Desktop is installed and running if you want to run a real health check.
- A Docker MCP server can be started on the VM or another LAN machine.

## Commands

From the repository root:

```powershell
dotnet restore MetBench_Client/MetBench_Client.csproj
dotnet build MetBench_Client/MetBench_Client.csproj --no-restore
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~DockerMcpRuntimeProfileSettingsTests|FullyQualifiedName~DockerMcpRuntimeProfileTests|FullyQualifiedName~SystemMtPageResourceTests"
python tools/metbench-docker-runtime-mcp profile-uri --runtime-key docker-linux --endpoint http://192.168.1.42:8765 --image metbench/runtime-python:latest --python python3 --auth-token-env METBENCH_DOCKER_MCP_TOKEN
```

Expected:

- Client build: 0 errors. Existing warnings are acceptable if unrelated.
- Focused tests: all pass.
- CLI prints a `docker-mcp://docker-linux?...` URI.

## UI Flow

1. Start MetBench:

   ```powershell
   dotnet run --project MetBench_Client/MetBench_Client.csproj
   ```

2. Open the navigation item `Runtime Environments`.
3. Confirm the page renders:
   - profile grid on the left;
   - Docker MCP form on the right;
   - fields for runtime key, endpoint, image, python executable, auth token env;
   - Refresh, Health, and Save profile buttons.
4. Fill:
   - Runtime key: `docker-linux`
   - Endpoint: a reachable MCP endpoint, e.g. `http://<LAN-IP>:8765`
   - Image: an allowlisted image from the MCP server config
   - Python executable: `python3`
   - Auth token env: `METBENCH_DOCKER_MCP_TOKEN`
5. Click `Save profile`.
6. Verify `<MetBench output directory>/appsettings.local.json` contains:

   ```json
   {
     "LauncherOptions": {
       "RuntimePythons": {
         "docker-linux": "docker-mcp://docker-linux?image=..."
       }
     }
   }
   ```

7. Restart MetBench and confirm `Runtime Environments` reloads the saved profile.
8. If a real MCP server is running and the token environment variable is set,
   click `Health`; expected status text reports health OK. If no server/token is
   available, the failure text must be explicit and must not crash the UI.

## Evidence To Return

- Build/test command outputs.
- Screenshot of the `Runtime Environments` page.
- Screenshot or text excerpt showing the saved `appsettings.local.json` profile.
- Health button result, either OK or explicit failure reason.
