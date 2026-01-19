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

## Secrets Management (Infisical)

Sensitive configuration is stored in Infisical:
- Database credentials
- Keycloak secrets
- API keys
- Encryption keys
- and more!

## Cerbos (Planned)

Some older docs/templates mention a `Cerbos` configuration section.

Cerbos is **not currently wired** into `{Project}.API`, so there is no active Cerbos configuration required for the running system.

### Implementation Example: ISLAMU Event
Cerbos is not currently wired into `Explore.API`, so there is no active Cerbos configuration required.
