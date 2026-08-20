---
name: mcp-csharp-publish
description: "Load when packaging or deploying a C# MCP server as a NuGet tool or HTTP container, writing MCP-specific Docker/CI, deploying to Azure Container Apps/App Service, configuring server.json, or publishing to the MCP Registry; not for general NuGet/Docker, creation, debugging, or tests."
type: workflow
enforcement: suggest
priority: medium
license: MIT
---
<!-- ABOUTME: Publishing router for tested C# MCP servers. -->
<!-- ABOUTME: Separates stdio NuGet, HTTP container, Azure, and Registry paths. -->

# C# MCP Server Publishing

## Release paths

| Server | Primary artifact | Required proof |
|---|---|---|
| stdio | NuGet tool package | Install and invoke the packed local `.nupkg` |
| HTTP | Non-root container image | Health check plus MCP tool invocation |
| Registry listing | `.mcp/server.json` | Registry lookup returns the published version |

## Rules

- Do not publish until the exact release artifact passes the MCP tests.
- Keep the project version, package version, image tag, and Registry package version synchronized.
- Never bake API keys, `.env` files, or development credentials into a package or image.
- HTTP production endpoints use TLS, input validation, rate limits where appropriate, and runtime secret injection.
- Use immutable versions/tags for release; do not make `latest` the only deployable identity.

## Workflow

1. Resolve transport, destination, package/server identity, and version.
2. Load only the matching packaging reference below.
3. Build the Release artifact and test that artifact locally.
4. Publish to NuGet or a container registry, then deploy if requested.
5. Publish Registry metadata only after the referenced package or endpoint exists.
6. Connect an MCP client and invoke a representative tool against the released endpoint/artifact.

## Resources

- [NuGet packaging](references/nuget-packaging.md) — load for stdio tool properties, local install, trusted publishing, or NuGet push.
- [Docker and Azure](references/docker-azure.md) — load for HTTP images, ACR, Container Apps, App Service, health checks, or runtime secrets.
- [MCP Registry](references/mcp-registry.md) — load for `server.json`, namespaces, authentication, CLI publishing, or CI.
- Use current official platform documentation before executing cloud or Registry commands whose syntax may have changed.

## Verification

- stdio: `dotnet pack --configuration Release`, install from the local source, then invoke it through an MCP client.
- HTTP: run the built image as its non-root user, pass its health check, and invoke an MCP tool.
- Registry: verify the listing resolves the exact published version.
- Inspect the final artifact/image for embedded secrets before release.
