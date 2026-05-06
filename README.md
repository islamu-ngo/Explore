<a name="readme-top"></a>

<div align="center">

# ISLAMU Event

Self-hostable event discovery and management for communities, organizations, and SaaS-style platforms.

[**Public Instance**][islamu-platform] · [**Getting Started**](docs/GETTING_STARTED.md) · [**Docs**](docs/index.md) · [**Roadmap**][roadmap-link]

</div>

---

## What This Is

ISLAMU Event is an open-source, white-label event platform for publishing, discovering, and operating events across one organization or many isolated tenants.

The hosted ISLAMU instance is curated for Islamic community events. The software itself is general-purpose and can be rebranded for conferences, nonprofits, education, local groups, or other event ecosystems.

## Current Maturity

The project is still pre-1.0. Expect breaking changes between releases, especially around configuration, deployment shape, and administrative workflows. Data-loss changes are avoided where possible, but operators should read the [release checklist](docs/RELEASE_CHECKLIST.md) and keep backups before upgrading.

## Quick Start

### Try The Public Instance

Visit [event.openislamu.org][islamu-platform] to browse events, create an account, publish events, and register for events.

### Run The Self-Hosted Compose Stack

```bash
git clone https://github.com/islamu-ngo/Event.git
cd Event
API_ENDPOINT=http://explore-api:8080/ docker compose up -d postgres redis keycloak-db keycloak explore-api explore-blazor
```

Default local endpoints:

| Service | URL |
|---|---|
| Blazor BFF/UI | `http://localhost:7002` |
| API | `http://localhost:7039` |

Swagger, Scalar, and OpenAPI JSON endpoints are Development/Testing conveniences; the Compose stack runs the API with `ASPNETCORE_ENVIRONMENT=Production`.

Optional profiles:

```bash
docker compose --profile storage up -d  # MinIO/S3-compatible storage
docker compose --profile authz up -d    # Cerbos PDP
```

For production details, setup-secret behavior, reverse proxy guidance, and backup requirements, use [SELF_HOSTING.md](docs/SELF_HOSTING.md) and [BACKUP_RESTORE_UPGRADE.md](docs/BACKUP_RESTORE_UPGRADE.md).

### Run The Local Aspire Development Loop

```bash
dotnet workload install aspire
dotnet build --configuration Release --verbosity quiet
dotnet run --project Explore.AppHost
```

The AppHost starts the migration service first, then API and Blazor with service discovery. See [GETTING_STARTED.md](docs/GETTING_STARTED.md) for the short developer path.

## Implemented Platform Areas

- Event and session lifecycle management with lookup-driven filtering.
- Organization and membership management.
- Single-tenant and multi-tenant runtime modes with tenant-aware data isolation.
- Blazor BFF architecture with OIDC authentication through Keycloak.
- Runtime-selectable authorization provider: local authorization or Cerbos.
- HAL/HATEOAS API responses, OpenAPI output, Swagger UI, and Scalar API reference.
- Modular event aspects for Islamic and Tech-specific event metadata.
- S3-compatible object storage, direct SMTP delivery infrastructure, in-app notifications, public sitemap/robots behavior, template sync, and contact-share consent workflows.
- BenchmarkDotNet runtime microbenchmarks and architecture/docs quality checks.

Federation-related data models and outbox foundations exist, but full ActivityPub gateway and protocol interoperability endpoints remain roadmap work. See [FEDERATION.md](docs/FEDERATION.md) before claiming protocol support.

## Documentation Map

Start with [docs/index.md](docs/index.md), or jump directly by role:

| Role | Start Here |
|---|---|
| Evaluators | [PROJECT.md](docs/PROJECT.md), [ARCHITECTURE.md](docs/ARCHITECTURE.md), [SECURITY.md](docs/SECURITY.md) |
| Operators | [SELF_HOSTING.md](docs/SELF_HOSTING.md), [CONFIGURATION.md](docs/CONFIGURATION.md), [OPERATIONS.md](docs/OPERATIONS.md), [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) |
| Admins | [ADMIN_GUIDE.md](docs/ADMIN_GUIDE.md), [ADMIN_HIERARCHY.md](docs/ADMIN_HIERARCHY.md), [AUTHORIZATION_PATTERNS.md](docs/AUTHORIZATION_PATTERNS.md) |
| Integrators | [API_COOKBOOK.md](docs/API_COOKBOOK.md), [API.md](docs/API.md), [API_CHANGELOG.md](docs/API_CHANGELOG.md) |
| Contributors | [FIRST_CONTRIBUTION.md](docs/FIRST_CONTRIBUTION.md), [CONTRIBUTING.md](docs/CONTRIBUTING.md), [TESTING.md](docs/TESTING.md) |
| AI agents | [AGENTS.md](AGENTS.md), [CLAUDE.md](CLAUDE.md), [dev/_journal/README.md](dev/_journal/README.md) |

## Technology Snapshot

- Runtime: .NET 10 (`net10.0`, SDK pinned in `global.json`).
- Architecture: Clean Architecture + CQRS/MediatR.
- Frontend: Blazor BFF/client with MudBlazor.
- Database/cache: PostgreSQL and Redis.
- Identity: Keycloak OIDC/OAuth 2.0.
- Authorization: local provider or Cerbos PDP.
- Configuration/secrets: environment variables, user secrets, and Infisical-compatible loading.
- Operations: Docker Compose for self-hosting, Aspire for local orchestration, OpenTelemetry and structured logs.
- Tests: TUnit, bUnit, integration tests, architecture/docs quality tests.

## Contributing

New contributors should start with [FIRST_CONTRIBUTION.md](docs/FIRST_CONTRIBUTION.md). Before opening a pull request, complete the PR template, record documentation impact, run the relevant per-project tests, and avoid solution-level `dotnet test`.

If you use AI-assisted workflows, `AGENTS.md` and `CLAUDE.md` define the repository-specific operating rules.

## Security

Please report security vulnerabilities responsibly instead of opening a public issue. See [SECURITY-POLICY.md](SECURITY-POLICY.md), or contact [contact@openislamu.org][contact-email].

## License

This project is licensed under [GNU AGPL v3](LICENSE).

## Community

Open a [GitHub issue][github-issues-link], join [GitHub Discussions][github-discussions-link], or connect on [Discord][discord-link]. Please follow the [Code of Conduct](CODE_OF_CONDUCT.md).

[islamu-platform]: https://event.openislamu.org
[roadmap-link]: https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988
[github-issues-link]: https://github.com/islamu-ngo/Event/issues/new/choose
[github-discussions-link]: https://github.com/islamu-ngo/Event/discussions
[discord-link]: https://discord.gg/wrkY824Yv5
[contact-email]: mailto:contact@openislamu.org
