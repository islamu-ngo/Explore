# Configuration

> **Project-Agnostic Configuration Guide**
>
> Placeholders use `{Placeholder}` syntax - see [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md).

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{project}` | Lowercase project name | `explore` |
| `{Project}.API` | API project name | `Explore.API` |
| `{Instance Name}` | Your instance display name | `ISLAMU Event` |
| `{Instance Domain}` | Your instance domain | `events.islamu.org` |

---

## Application Settings

**Generic Template:**
```json
{
  "Keycloak": {
    "Authority": "https://your-keycloak-instance.com/realms/{realm}",
    "Realm": "your-realm",
    "ClientId": "{project}-api",
    "ClientSecret": ""
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Federation": {
    "InstanceDomain": "{Instance Domain}",
    "InstanceName": "{Instance Name}",
    "EnableFederation": true,
    "SharedInboxEnabled": true
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } },
      { "Name": "Seq", "Args": { "serverUrl": "http://localhost:5341" } }
    ]
  }
}
```

### Implementation Example: ISLAMU Event
```json
{
  "Keycloak": {
    "Authority": "https://keycloak.openislamu.org/realms/islamu-dev",
    "Realm": "islamu-dev",
    "ClientId": "explore-api",
    "ClientSecret": ""
  },
  "Federation": {
    "InstanceDomain": "events.islamu.org",
    "InstanceName": "ISLAMU Event",
    "EnableFederation": true,
    "SharedInboxEnabled": true
  }
}
```

## Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development`, `Staging`, `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | `Host=...;Database=...` |
| `Keycloak__ClientSecret` | Keycloak client secret | `...` |
| `Infisical__ClientId` | Secrets manager client | `...` |
| `Infisical__ClientSecret` | Secrets manager secret | `...` |

## Secret Management

The application uses a unified secret management system (`Explore.Secrets`) that supports multiple providers:

### Secret Providers

| Provider | Use Case | Configuration |
|----------|----------|---------------|
| `none` | Self-hosters, local dev | Use environment variables directly |
| `infisical` | Production with Infisical | Connects to Infisical service |

### Configuration Options

```json
{
  "SecretProvider": {
    "Provider": "infisical",
    "FailFast": true
  },
  "Infisical": {
    "Url": "https://app.infisical.com",
    "ProjectId": "your-project-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Environment": "dev"
  },
  "SecretRefresh": {
    "RefreshInterval": "00:05:00",
    "BaseBackoffDelay": "00:00:30",
    "MaxBackoffDelay": "00:05:00"
  }
}
```

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `SECRET_PROVIDER` | Provider type: `none` or `infisical` | No (default: `none`) |
| `Infisical__Url` | Infisical server URL | Only if using Infisical |
| `Infisical__ProjectId` | Infisical project ID | Only if using Infisical |
| `Infisical__ClientId` | Universal Auth client ID | Only if using Infisical |
| `Infisical__ClientSecret` | Universal Auth client secret | Only if using Infisical |
| `Infisical__Environment` | Environment slug (dev, staging, prod) | No (default: `dev`) |

### Self-Hosted Deployment (No Secret Manager)

For self-hosters who don't want to use Infisical, set `SECRET_PROVIDER=none` and provide all secrets via environment variables:

```bash
# Required environment variables for self-hosted deployment
SECRET_PROVIDER=none
ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
Keycloak__Authority=https://your-keycloak/realms/your-realm
Keycloak__ClientId=explore-api
Keycloak__ClientSecret=your-secret
S3Settings__AccessKeyId=your-access-key
S3Settings__SecretAccessKey=your-secret-key
```

### Infisical Deployment

For production with Infisical:

```bash
SECRET_PROVIDER=infisical
Infisical__Url=https://app.infisical.com
Infisical__ProjectId=your-project-id
Infisical__ClientId=your-client-id
Infisical__ClientSecret=your-client-secret
Infisical__Environment=prod
```

Secrets are automatically loaded from Infisical paths:
- `/keycloak` - Keycloak configuration
- `/postgresql` - Database credentials
- `/api` - API-specific secrets (S3, etc.)
- `/blazor` - Blazor-specific configuration

### Features

- **Automatic Refresh**: Secrets are refreshed periodically (configurable interval)
- **Health Checks**: Secret provider health is exposed at `/health`
- **Metrics**: Prometheus-compatible metrics for monitoring
- **Audit Logging**: All secret access is logged with redaction
- **Graceful Fallback**: Falls back to environment variables if Infisical unavailable

## Cerbos (Planned)

Some older docs/templates mention a `Cerbos` configuration section.

Cerbos is **not currently wired** into `{Project}.API`, so there is no active Cerbos configuration required for the running system.

### Implementation Example: ISLAMU Event
Cerbos is not currently wired into `Explore.API`, so there is no active Cerbos configuration required.
