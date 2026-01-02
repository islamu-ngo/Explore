# Configuration

## Application Settings

```json
{
  "Keycloak": {
    "Authority": "https://keycloak.openislamu.org/realms/{realm}",
    "Realm": "islamu-dev",
    "ClientId": "explore-api",
    "ClientSecret": ""
  },
  "Cerbos": {
    "Address": "http://localhost:3593",
    "TlsEnabled": false
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Federation": {
    "InstanceDomain": "events.islamu.org",
    "InstanceName": "ISLAMU Event",
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
