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

## Cerbos (Planned - Not Currently Integrated)

Some older docs/templates mention a `Cerbos` configuration section.

Cerbos is **not currently integrated** into `{Project}.API`. All authorization logic is currently implemented within the application code. There is no active Cerbos configuration required for the running system.

### Implementation Example: ISLAMU Event
Cerbos is not currently integrated into `Explore.API`. Authorization is handled by MediatR handlers and endpoint-level `[Authorize]` attributes.

---

## Instance-Level Settings

The system supports **instance-level configuration** that controls deployment modes, feature toggles, and system-wide settings through the `SystemSetting` table.

### SystemSetting Table

Instance administrators can configure system behavior through database-stored settings:

**Generic Pattern:**
```csharp
public class SystemSetting
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Auditing
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

### Governance Setting Keys

System settings are defined as constants in `GovernanceSettingKeys`:

```csharp
public static class GovernanceSettingKeys
{
    // Deployment Mode
    public const string DeploymentMode = "System.DeploymentMode";  // "SingleTenant" or "MultiTenant"

    // Feature Flags
    public const string FederationEnabled = "System.Federation.Enabled";
    public const string RegistrationOpen = "System.Registration.Open";
    public const string MaintenanceMode = "System.MaintenanceMode";

    // Module Governance (see Module-Specific Configuration section)
    public const string IslamicModuleEnabled = "System.Modules.Islamic.Enabled";
    public const string TechModuleEnabled = "System.Modules.Tech.Enabled";

    // Instance Metadata
    public const string InstanceName = "System.Instance.Name";
    public const string InstanceDomain = "System.Instance.Domain";
}
```

### Deployment Mode Switching

The `DeploymentMode` setting controls multi-tenant vs single-tenant behavior:

**Generic Pattern:**
```csharp
// Retrieve deployment mode
var deploymentMode = await _systemSettingService.GetAsync(
    GovernanceSettingKeys.DeploymentMode);

if (deploymentMode == "SingleTenant")
{
    // Single tenant: All users belong to default tenant
    // No tenant selection UI shown
}
else if (deploymentMode == "MultiTenant")
{
    // Multi-tenant: Users can belong to multiple tenants
    // Tenant selection UI shown
    // Tenant context required for all operations
}
```

### Implementation Example: ISLAMU Event

```csharp
// Explore.Domain/Constants/GovernanceSettingKeys.cs
public static class GovernanceSettingKeys
{
    public const string DeploymentMode = "System.DeploymentMode";
    public const string FederationEnabled = "System.Federation.Enabled";
    public const string IslamicModuleEnabled = "System.Modules.Islamic.Enabled";
    // ... more keys
}

// Usage in services
var federationEnabled = await _systemSettingService.GetBoolAsync(
    GovernanceSettingKeys.FederationEnabled);

if (federationEnabled)
{
    // Enable federation endpoints
}
```

**See Also**: [OPERATIONS.md](OPERATIONS.md) for deployment mode details.

---

## Module-Specific Configuration

The system supports **modular event types** through per-tenant capability configuration. Different tenants can enable different event modules (e.g., Islamic events, Tech events).

### TenantCapability Table

Modules are enabled per-tenant through the `TenantCapability` table:

**Generic Pattern:**
```csharp
public class TenantCapability
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ModuleName { get; set; } = string.Empty;  // "Islamic", "Tech", etc.
    public bool Enabled { get; set; }
    public Dictionary<string, string>? Configuration { get; set; }  // Module-specific settings

    // Relationships
    public Tenant? Tenant { get; set; }
}
```

### Module Governance Settings

Modules can be enabled/disabled at two levels:

1. **System Level** (via `SystemSetting` table):
   - Controls if a module is available at all
   - Instance admin configures

2. **Tenant Level** (via `TenantCapability` table):
   - Controls if a specific tenant can use a module
   - Tenant admin configures

**Hierarchy**:
```
System.Modules.Islamic.Enabled = true   (System-wide toggle)
  └── TenantCapability: TenantId=abc, ModuleName="Islamic", Enabled=true   (Tenant-specific)
```

If system-level setting is `false`, the module is unavailable to ALL tenants, regardless of `TenantCapability` settings.

### Module Resolution Example

**Generic Pattern:**
```csharp
// Check if module is available for tenant
public async Task<bool> IsModuleAvailableAsync(Guid tenantId, string moduleName)
{
    // 1. Check system-level setting
    var systemKey = $"System.Modules.{moduleName}.Enabled";
    var systemEnabled = await _systemSettingService.GetBoolAsync(systemKey);
    if (!systemEnabled) return false;

    // 2. Check tenant-level capability
    var capability = await _tenantCapabilityRepository.GetByTenantAndModule(
        tenantId, moduleName);

    return capability?.Enabled ?? false;
}

// Usage in handlers
if (await _moduleService.IsModuleAvailableAsync(tenantId, "Islamic"))
{
    // Load EventIslamicAspect
    var aspect = await _islamicAspectRepository.GetByEventId(eventId);
}
```

### Implementation Example: ISLAMU Event

```csharp
// Module availability check in Create Event handler
public async Task<BaseCommandResponse<Guid>> Handle(...)
{
    // ... create base event ...

    // If Islamic module enabled for this tenant, create Islamic aspect
    if (await _moduleService.IsModuleAvailableAsync(tenantId, "Islamic"))
    {
        if (request.EventDto.IslamicAspect != null)
        {
            var islamicAspect = new EventIslamicAspect
            {
                EventId = newEvent.Id,
                PrayerTimesAvailable = request.EventDto.IslamicAspect.PrayerTimesAvailable,
                // ... map other fields
            };
            await _islamicAspectRepository.Create(islamicAspect);
        }
    }

    return response;
}
```

**See Also**: [CODEBASE_INSIGHTS.md](CODEBASE_INSIGHTS.md) Section 7 for module governance implementation.

---

## Monitoring & Observability

The system uses **Prometheus** for metrics and **Loki** for centralized logging.

### Prometheus (Metrics)

Application metrics are exposed at `/metrics` endpoint using Prometheus format:

**Generic Configuration:**
```json
{
  "Prometheus": {
    "Enabled": true,
    "MetricsPath": "/metrics",
    "Port": 9090
  }
}
```

**Metrics Exposed**:
- HTTP request duration histograms
- Database query performance
- Command/Query execution times (MediatR pipeline)
- Repository operation counters
- Exception counts by type

**Implementation Example: ISLAMU Event**
```csharp
// Explore.API/Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
        metrics.AddMeter("Explore.API");
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
    });

app.MapPrometheusScrapingEndpoint();  // Exposes /metrics
```

### Loki (Centralized Logging)

Logs are shipped to Loki via Serilog:

**Generic Configuration:**
```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "GrafanaLoki",
        "Args": {
          "uri": "http://loki:3100",
          "labels": [
            { "key": "app", "value": "{project}-api" },
            { "key": "environment", "value": "production" }
          ]
        }
      }
    ]
  }
}
```

**Implementation Example: ISLAMU Event**
```csharp
// Explore.API/Program.cs
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Explore.API")
        .WriteTo.Console()
        .WriteTo.GrafanaLoki(
            context.Configuration["Loki:Uri"]!,
            labels: new List<LokiLabel>
            {
                new() { Key = "app", Value = "explore-api" },
                new() { Key = "env", Value = context.HostingEnvironment.EnvironmentName }
            });
});
```

### Observability Stack

**Recommended Stack**:
- **Prometheus**: Metrics collection and storage
- **Loki**: Log aggregation and querying
- **Grafana**: Unified dashboards for metrics + logs
- **Aspire Dashboard** (Development): Local observability during development

**Key Benefits**:
- Unified observability (metrics + logs in Grafana)
- Label-based log filtering (tenant, user, request ID)
- Performance monitoring (P50, P95, P99 latencies)
- Error tracking with stack traces
- Correlation between logs and metrics

**See Also**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for debugging with Prometheus/Loki.

