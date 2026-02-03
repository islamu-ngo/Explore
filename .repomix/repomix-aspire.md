This file is a merged representation of a subset of the codebase, containing specifically included files and files not matching ignore patterns, combined into a single document by Repomix.
The content has been processed where comments have been removed, line numbers have been added.

# File Summary

## Purpose
This file contains a packed representation of a subset of the repository's contents that is considered the most important context.
It is designed to be easily consumable by AI systems for analysis, code review,
or other automated processes.

## File Format
The content is organized as follows:
1. This summary section
2. Repository information
3. Directory structure
4. Repository files (if enabled)
5. Multiple file entries, each consisting of:
  a. A header with the file path (## File: path/to/file)
  b. The full contents of the file in a code block

## Usage Guidelines
- This file should be treated as read-only. Any changes should be made to the
  original repository files, not this packed version.
- When processing this file, use the file path to distinguish
  between different files in the repository.
- Be aware that this file may contain sensitive information. Handle it with
  the same level of security as you would the original repository.

## Notes
- Some files may have been excluded based on .gitignore rules and Repomix's configuration
- Binary files are not included in this packed representation. Please refer to the Repository Structure section for a complete list of file paths, including binary files
- Only files matching these patterns are included: Explore.AppHosting/**/*, Explore.ServiceDefaults/**/*, docker-compose.yml
- Files matching these patterns are excluded: **/*.log, .opencode/command/**/*, .claude/commands/**/*, .claude/hooks/**/*
- Files matching patterns in .gitignore are excluded
- Files matching default ignore patterns are excluded
- Code comments have been removed from supported file types
- Line numbers have been added to the beginning of each line
- Files are sorted by Git change count (files with more changes are at the bottom)

# Directory Structure
```
docker-compose.yml
Explore.ServiceDefaults/Explore.ServiceDefaults.csproj
Explore.ServiceDefaults/Extensions.cs
```

# Files

## File: docker-compose.yml
```yaml
  1: name: explore-platform
  2: 
  3: x-keycloak-env: &keycloak-env
  4:   KEYCLOAK_REALM: ${KEYCLOAK_REALM:-explore}
  5:   KEYCLOAK_PUBLIC_URL: ${KEYCLOAK_PUBLIC_URL:-http://keycloak.localhost:8080}
  6:   KEYCLOAK_INTERNAL_URL: http://keycloak:8080
  7:   Keycloak__RequireHttpsMetadata: ${KEYCLOAK_REQUIRE_HTTPS_METADATA:-false}
  8: 
  9: x-postgres-env: &postgres-env
 10:   POSTGRES_USER: ${POSTGRES_USER:-explore}
 11:   POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-explore}
 12:   POSTGRES_DB: ${POSTGRES_DB:-explore}
 13: 
 14: x-s3-env: &s3-env
 15:   S3Settings__Endpoint: ${S3_INTERNAL_ENDPOINT:-http://minio:9000}
 16:   S3Settings__PublicEndpoint: ${S3_PUBLIC_ENDPOINT:-http://minio.localhost:9000}
 17:   S3Settings__Region: ${S3_REGION:-us-east-1}
 18:   S3Settings__BucketName: ${S3_BUCKET_NAME:-explore}
 19:   S3Settings__AccessKeyId: ${S3_ACCESS_KEY_ID:-minioadmin}
 20:   S3Settings__SecretAccessKey: ${S3_SECRET_ACCESS_KEY:-minioadmin}
 21: 
 22: services:
 23:   postgres:
 24:     image: postgres:18-alpine
 25:     restart: unless-stopped
 26:     environment:
 27:       <<: *postgres-env
 28:     volumes:
 29:       - postgres_data:/var/lib/postgresql/data
 30:     healthcheck:
 31:       test: ["CMD-SHELL", "pg_isready -U $$POSTGRES_USER -d $$POSTGRES_DB"]
 32:       interval: 5s
 33:       retries: 5
 34: 
 35:   keycloak-db:
 36:     image: postgres:18-alpine
 37:     restart: unless-stopped
 38:     environment:
 39:       POSTGRES_USER: keycloak
 40:       POSTGRES_PASSWORD: keycloak
 41:       POSTGRES_DB: keycloak
 42:     volumes:
 43:       - keycloak_data:/var/lib/postgresql/data
 44:     healthcheck:
 45:       test: ["CMD-SHELL", "pg_isready -U keycloak -d keycloak"]
 46:       interval: 5s
 47:       retries: 5
 48: 
 49:   keycloak:
 50:     image: quay.io/keycloak/keycloak:26.1.2
 51:     restart: unless-stopped
 52:     command: ["start-dev", "--import-realm", "--http-port=8080"]
 53:     environment:
 54:       KC_DB: postgres
 55:       KC_DB_URL: jdbc:postgresql://keycloak-db:5432/keycloak
 56:       KC_DB_USERNAME: keycloak
 57:       KC_DB_PASSWORD: keycloak
 58:       KC_HOSTNAME: ${KEYCLOAK_PUBLIC_URL:-http://keycloak.localhost:8080}
 59:       KC_HOSTNAME_URL: ${KEYCLOAK_PUBLIC_URL:-http://keycloak.localhost:8080}
 60:       KC_HOSTNAME_STRICT: "false"
 61:       KC_HOSTNAME_STRICT_BACKCHANNEL: "false"
 62:       KC_HTTP_ENABLED: "true"
 63:       KEYCLOAK_ADMIN: ${KEYCLOAK_ADMIN:-admin}
 64:       KEYCLOAK_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD:-admin}
 65:     volumes:
 66:       - ./docker/keycloak/realm-export.json:/opt/keycloak/data/import/realm-export.json:ro
 67:     ports:
 68:       - "8080:8080"
 69:     healthcheck:
 70:       test: ["CMD-SHELL", "bash -c 'exec 3<>/dev/tcp/localhost/8080'"]
 71:       interval: 10s
 72:       timeout: 10s
 73:       retries: 15
 74:       start_period: 60s
 75:     depends_on:
 76:       keycloak-db:
 77:         condition: service_healthy
 78: 
 79:   explore-api:
 80:     build:
 81:       context: .
 82:       dockerfile: Explore.API/Dockerfile
 83:     environment:
 84:       <<: [*keycloak-env, *s3-env]
 85:       ASPNETCORE_URLS: http://+:8080
 86:       ASPNETCORE_ENVIRONMENT: Production
 87:       ConnectionStrings__DefaultConnection: Host=postgres;Database=${POSTGRES_DB:-explore};Username=${POSTGRES_USER:-explore};Password=${POSTGRES_PASSWORD:-explore}
 88:       Keycloak__MetadataAddress: ${KEYCLOAK_INTERNAL_URL:-http://keycloak:8080}/realms/${KEYCLOAK_REALM:-explore}/.well-known/openid-configuration
 89:       Keycloak__Authority: ${KEYCLOAK_PUBLIC_URL:-http://keycloak.localhost:8080}/realms/${KEYCLOAK_REALM:-explore}
 90:     ports:
 91:       - "7039:8080"
 92:     extra_hosts:
 93:       - "keycloak.localhost:host-gateway"
 94:       - "minio.localhost:host-gateway"
 95:     depends_on:
 96:       postgres:
 97:         condition: service_healthy
 98:       keycloak:
 99:         condition: service_healthy
100:       minio:
101:         condition: service_healthy
102:         required: false
103: 
104:   explore-blazor:
105:     build:
106:       context: .
107:       dockerfile: Explore.Blazor/Dockerfile
108:     environment:
109:       <<: *keycloak-env
110:       ASPNETCORE_URLS: http://+:8080
111:       ASPNETCORE_ENVIRONMENT: Production
112:       EXPLORE_API_BASE_URL: ${EXPLORE_API_BASE_URL:-http://explore-api:8080/}
113:       Keycloak__Authority: ${KEYCLOAK_PUBLIC_URL:-http://keycloak.localhost:8080}/realms/${KEYCLOAK_REALM:-explore}
114:       Keycloak__MetadataAddress: ${KEYCLOAK_INTERNAL_URL:-http://keycloak:8080}/realms/${KEYCLOAK_REALM:-explore}/.well-known/openid-configuration
115:       Keycloak__Realm: ${KEYCLOAK_REALM:-explore}
116:       Keycloak__ClientId: explore-blazor-server
117:       Keycloak__ClientSecret: ${KEYCLOAK_BLAZOR_CLIENT_SECRET:-explore-blazor-server-secret}
118:     ports:
119:       - "7002:8080"
120:     extra_hosts:
121:       - "keycloak.localhost:host-gateway"
122:       - "minio.localhost:host-gateway"
123:     depends_on:
124:       explore-api:
125:         condition: service_started
126:       keycloak:
127:         condition: service_healthy
128:       minio:
129:         condition: service_healthy
130:         required: false
131: 
132:   minio:
133:     image: ghcr.io/coollabsio/minio:latest
134:     profiles: ["storage"]
135:     restart: unless-stopped
136:     environment:
137:       MINIO_ROOT_USER: ${S3_ACCESS_KEY_ID:-minioadmin}
138:       MINIO_ROOT_PASSWORD: ${S3_SECRET_ACCESS_KEY:-minioadmin}
139:       MINIO_SERVER_URL: ${S3_PUBLIC_ENDPOINT:-http://minio.localhost:9000}
140:     volumes:
141:       - minio_data:/data
142:     ports:
143:       - "9005:9000"
144:       - "9006:9001"
145:     command: server /data --console-address ":9001"
146:     healthcheck:
147:       test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
148:       interval: 5s
149:       retries: 5
150: 
151:   minio-init:
152:     image: minio/mc:latest
153:     profiles: ["storage"]
154:     depends_on:
155:       minio:
156:         condition: service_healthy
157:     entrypoint: >
158:       /bin/sh -c "
159:       mc alias set local http://minio:9000 ${S3_ACCESS_KEY_ID:-minioadmin} ${S3_SECRET_ACCESS_KEY:-minioadmin};
160:       mc mb --ignore-existing local/${S3_BUCKET_NAME:-explore};
161:       mc anonymous set public local/${S3_BUCKET_NAME:-explore};
162:       "
163: 
164: volumes:
165:   postgres_data:
166:   keycloak_data:
167:   minio_data:
```

## File: Explore.ServiceDefaults/Explore.ServiceDefaults.csproj
```
 1: <Project Sdk="Microsoft.NET.Sdk">
 2: 
 3:   <PropertyGroup>
 4:     <TargetFramework>net9.0</TargetFramework>
 5:     <ImplicitUsings>enable</ImplicitUsings>
 6:     <Nullable>enable</Nullable>
 7:     <IsAspireSharedProject>true</IsAspireSharedProject>
 8:   </PropertyGroup>
 9: 
10:   <ItemGroup>
11:     <FrameworkReference Include="Microsoft.AspNetCore.App" />
12: 
13:     <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.4.0" />
14:     <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.3.1" />
15:     <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.12.0" />
16:     <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
17:     <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
18:     <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
19:     <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.12.0" />
20:   </ItemGroup>
21: 
22: </Project>
```

## File: Explore.ServiceDefaults/Extensions.cs
```csharp
  1: using System.Text.Json;
  2: using Microsoft.AspNetCore.Builder;
  3: using Microsoft.AspNetCore.Diagnostics.HealthChecks;
  4: using Microsoft.AspNetCore.Http;
  5: using Microsoft.Extensions.DependencyInjection;
  6: using Microsoft.Extensions.Diagnostics.HealthChecks;
  7: using Microsoft.Extensions.Logging;
  8: using Microsoft.Extensions.ServiceDiscovery;
  9: using OpenTelemetry;
 10: using OpenTelemetry.Metrics;
 11: using OpenTelemetry.Trace;
 12: 
 13: namespace Microsoft.Extensions.Hosting;
 14: 
 15: 
 16: 
 17: 
 18: public static class Extensions
 19: {
 20:     private const string HealthEndpointPath = "/health";
 21:     private const string AlivenessEndpointPath = "/alive";
 22: 
 23:     public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
 24:     {
 25:         builder.ConfigureOpenTelemetry();
 26: 
 27:         builder.AddDefaultHealthChecks();
 28: 
 29:         builder.Services.AddServiceDiscovery();
 30: 
 31:         builder.Services.ConfigureHttpClientDefaults(http =>
 32:         {
 33: 
 34:             http.AddStandardResilienceHandler();
 35: 
 36: 
 37:             http.AddServiceDiscovery();
 38:         });
 39: 
 40: 
 41: 
 42: 
 43: 
 44: 
 45: 
 46:         return builder;
 47:     }
 48: 
 49:     public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
 50:     {
 51:         builder.Logging.AddOpenTelemetry(logging =>
 52:         {
 53:             logging.IncludeFormattedMessage = true;
 54:             logging.IncludeScopes = true;
 55:         });
 56: 
 57:         builder.Services.AddOpenTelemetry()
 58:             .WithMetrics(metrics =>
 59:             {
 60:                 metrics.AddAspNetCoreInstrumentation()
 61:                     .AddHttpClientInstrumentation()
 62:                     .AddRuntimeInstrumentation();
 63:             })
 64:             .WithTracing(tracing =>
 65:             {
 66:                 tracing.AddSource(builder.Environment.ApplicationName)
 67:                     .AddAspNetCoreInstrumentation(tracing =>
 68: 
 69:                         tracing.Filter = context =>
 70:                             !context.Request.Path.StartsWithSegments(HealthEndpointPath)
 71:                             && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
 72:                     )
 73: 
 74: 
 75:                     .AddHttpClientInstrumentation();
 76:             });
 77: 
 78:         builder.AddOpenTelemetryExporters();
 79: 
 80:         return builder;
 81:     }
 82: 
 83:     private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
 84:     {
 85:         var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
 86: 
 87:         if (useOtlpExporter)
 88:         {
 89:             builder.Services.AddOpenTelemetry().UseOtlpExporter();
 90:         }
 91: 
 92: 
 93: 
 94: 
 95: 
 96: 
 97: 
 98: 
 99:         return builder;
100:     }
101: 
102:     public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
103:     {
104:         builder.Services.AddHealthChecks()
105: 
106:             .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
107: 
108:         return builder;
109:     }
110: 
111:     public static WebApplication MapDefaultEndpoints(this WebApplication app)
112:     {
113: 
114: 
115: 
116: 
117:         var healthCheckOptions = new HealthCheckOptions
118:         {
119:             ResponseWriter = WriteHealthCheckResponse,
120:             ResultStatusCodes =
121:             {
122:                 [HealthStatus.Healthy] = StatusCodes.Status200OK,
123:                 [HealthStatus.Degraded] = StatusCodes.Status200OK,
124:                 [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
125:             }
126:         };
127: 
128: 
129:         app.MapHealthChecks(HealthEndpointPath, healthCheckOptions);
130: 
131: 
132:         app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
133:         {
134:             Predicate = r => r.Tags.Contains("live"),
135:             ResponseWriter = WriteHealthCheckResponse,
136:             ResultStatusCodes =
137:             {
138:                 [HealthStatus.Healthy] = StatusCodes.Status200OK,
139:                 [HealthStatus.Degraded] = StatusCodes.Status200OK,
140:                 [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
141:             }
142:         });
143: 
144:         return app;
145:     }
146: 
147:     private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
148:     {
149: 
150:         context.Response.ContentType = "application/json; charset=utf-8";
151:         context.Response.Headers["Connection"] = "close";
152:         context.Response.Headers["Access-Control-Allow-Origin"] = "*";
153:         context.Response.Headers["X-Health-Status"] = report.Status.ToString();
154:         context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
155:         context.Response.Headers["Pragma"] = "no-cache";
156: 
157:         var response = new
158:         {
159:             status = report.Status.ToString(),
160:             message = report.Status switch
161:             {
162:                 HealthStatus.Healthy => "Ok",
163:                 HealthStatus.Degraded => "Degraded",
164:                 HealthStatus.Unhealthy => "Service Unavailable",
165:                 _ => "Unknown"
166:             },
167:             totalDuration = report.TotalDuration.TotalMilliseconds,
168:             checks = report.Entries.Select(e => new
169:             {
170:                 name = e.Key,
171:                 status = e.Value.Status.ToString(),
172:                 description = e.Value.Description,
173:                 duration = e.Value.Duration.TotalMilliseconds,
174:                 error = e.Value.Exception?.Message,
175:                 data = e.Value.Data.Count > 0 ? e.Value.Data : null
176:             })
177:         };
178: 
179:         var jsonOptions = new JsonSerializerOptions
180:         {
181:             WriteIndented = true,
182:             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
183:             DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
184:         };
185: 
186:         await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
187:     }
188: }
```
