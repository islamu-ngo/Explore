# Infisical Optionality Plan (Docker Compose + Image Deployments)

## Executive Summary
You want two deployment modes that share the same images and configuration model:
1) Docker Compose for self-hosters (no Infisical required, but optional).
2) Image-only deployments for your hosted instance or advanced users (Infisical required or optional).

The key is to make Infisical an **optional configuration provider** that can be enabled by env vars at runtime without changing code paths for non-Infisical deployments.

## Current Constraints (from codebase)
- API and Blazor use configuration extension classes to map external env vars into .NET config.
- Docker Compose relies on direct environment variables.
- EntryPoint scripts use Infisical CLI for injected secrets in non-compose deployments.

## Goals
- Keep Docker Compose usable without Infisical.
- Allow Infisical as an opt-in feature for Compose users.
- Allow image-only deployments to pull secrets from Infisical, but still work with direct env vars.
- Preserve security best practices (least privilege, scoped secrets).

## Recommended Design (No Code Change Yet)

### 1) Infisical as an optional “overlay”
Behavior priority for both API and Blazor:
1. Environment variables (explicit overrides)
2. Infisical CLI injected values (if enabled)
3. Appsettings defaults (fallback)

This keeps Docker Compose working without Infisical, while allowing an Infisical overlay.

### 2) Runtime toggle via env vars
Standardize a minimal set of env flags:
- `INFISICAL_ENABLED=true|false`
- `INFISICAL_HOST` (for self-hosted Infisical)
- `INFISICAL_PROJECT_ID`
- `INFISICAL_CLIENT_ID`
- `INFISICAL_CLIENT_SECRET`
- `INFISICAL_ENV=dev|staging|prod`

If `INFISICAL_ENABLED=false` or unset, entrypoint should skip Infisical entirely.

### 3) Docker Compose Profiles
Expose two profiles:
- `default` (no Infisical, direct env vars)
- `infisical` (adds Infisical sidecar or enables Infisical in entrypoint)

This lets users opt-in without editing the file.

### 4) S3 + Keycloak split for internal vs public
Continue separating internal vs public endpoints:
- Internal endpoints for service-to-service
- Public endpoints for browser-facing redirects and presigned URLs

This supports both local HTTP and VPS HTTPS.

## Suggested Compose UX (No Code Change Yet)

### Local (no Infisical)
Set direct env vars in compose and use default profile.

### Local + Infisical (optional)
Add `--profile infisical` and provide Infisical env vars.
EntryPoints use those to inject secrets at startup.

### VPS / Hosted Instance
Deploy each service separately; enable Infisical via env vars.
Use reverse proxy for public URLs; internal URLs stay on private network.

## Security Notes
- Infisical improves governance by centralizing secrets, not by itself “more secure.”
- It becomes more secure **when access is scoped** (per service or per environment).
- For teams: Infisical reduces secret sprawl in CI/CD and Docker Compose files.

## Recommendation Summary
- Offer Infisical as **optional** everywhere.
- Keep env var overrides as the highest priority.
- Add a compose profile to toggle Infisical cleanly.
- Document separate internal/public endpoint variables for Keycloak and S3.

## Next Steps (If You Want Implementation)
1) Add `INFISICAL_ENABLED` logic to entrypoint scripts.
2) Provide a Compose profile that injects Infisical envs.
3) Update docs for three deployment paths: local compose, compose+infisical, image-only.
4) Add examples for Keycloak/S3 internal vs public endpoint variables.
