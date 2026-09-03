<!-- ABOUTME: Packaging, containerization, and deployment workflows for C# MCP servers. -->
<!-- ABOUTME: Covers NuGet tool packaging, chiseled Dockerfiles, Azure Container Apps, and MCP Registry publishing. -->

# C# MCP Server Publishing & Deployment

A guide for packaging, containerizing, deploying, and distributing C# Model Context Protocol (MCP) servers across local package, cloud container, and public registry destinations.

## Release Paths & Requirements

| Transport / Shape | Distribution Artifact | Verification Requirement |
|---|---|---|
| **stdio** | NuGet Tool Package (`.nupkg`) | Install local package with `dotnet tool install` and invoke via MCP client |
| **HTTP** | Non-root container image | Run container locally, pass health check, and call an MCP endpoint |
| **Cloud Host** | Azure Container App / App Service | Deploy via CLI/Bicep with environment secret injection |
| **Discoverability** | MCP Registry listing (`server.json`) | Registry lookup resolves the exact published package/endpoint |

## Release Invariants & Rules

1. **Secrets Isolation**: Never bake API keys, connection strings, `.env` files, or development secrets into NuGet packages, Docker images, or `server.json`. Secrets must be injected at runtime via environment variables or secret stores.
2. **Synchronized Versioning**: Keep the `.csproj` `<Version>`, package version, container tag, and `server.json` version aligned. Do not rely on mutable `latest` tags for production releases.
3. **Non-Root Containers**: Container images must run under an unprivileged user (e.g., `USER app` in .NET 10 chiseled images).
4. **Test Before Release**: The exact build artifact slated for publication must pass all unit and protocol integration tests.

---

## 1. NuGet Tool Packaging (stdio)

Package stdio MCP servers as global or local .NET tools:

### `.csproj` Configuration
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>my-mcp-server</ToolCommandName>
  <PackageId>MyOrg.McpServer</PackageId>
  <Version>1.0.0</Version>
  <Description>C# MCP server providing tools for developer workflows.</Description>
</PropertyGroup>
```

### Build & Verify Locally
```bash
# 1. Build the release package
dotnet pack --configuration Release -o ./nupkg

# 2. Test local installation
dotnet tool install --global --add-source ./nupkg MyOrg.McpServer

# 3. Verify execution via tool command
my-mcp-server --version

# 4. Push to NuGet
dotnet nuget push ./nupkg/*.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json
```
See [nuget-packaging.md](nuget-packaging.md) for signing, trusted publishing, and tool manifests.

---

## 2. Containerization & Azure Hosting (HTTP)

### Multi-Stage Chiseled Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MyMcpServer.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MyMcpServer.dll"]
```

### Azure Container Apps Deployment
Deploy the container with secure environment secrets:
```bash
az containerapp create \
  --name my-mcp-server \
  --resource-group rg-mcp \
  --environment mcp-env \
  --image myregistry.azurecr.io/my-mcp-server:1.0.0 \
  --target-port 8080 \
  --ingress external \
  --secrets "api-key=$EXTERNAL_API_KEY" \
  --env-vars "ExternalApiKey=secretref:api-key"
```
See [docker-azure.md](docker-azure.md) for health checks, Bicep templates, and App Service alternatives.

---

## 3. MCP Registry Publishing

Register your server on the official MCP Registry (`registry.modelcontextprotocol.io`) to enable client discovery:

Create `.mcp/server.json`:
```json
{
  "$schema": "https://registry.modelcontextprotocol.io/schema/server.json",
  "name": "org.myorg/my-mcp-server",
  "version": "1.0.0",
  "description": "Developer tools for .NET environments.",
  "packages": [
    {
      "registryType": "nuget",
      "identifier": "MyOrg.McpServer",
      "version": "1.0.0"
    }
  ]
}
```
Publish via the MCP Registry CLI:
```bash
npx @modelcontextprotocol/registry-cli publish
```
See [mcp-registry.md](mcp-registry.md) for namespace ownership, schema validation, and GitHub Actions CI.

## Related Resources

- [nuget-packaging.md](nuget-packaging.md) — .NET tool packaging configuration and publishing options.
- [docker-azure.md](docker-azure.md) — Production Dockerfiles, Azure CLI commands, and cloud architectures.
- [mcp-registry.md](mcp-registry.md) — Complete `server.json` schema and namespace verification.
