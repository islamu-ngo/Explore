# Independent Project Setup Guide

> **Purpose**: Configure User Secrets to run Explore.API and Explore.Blazor independently without Aspire orchestration.
>
> **Date**: January 2026

---

## Executive Summary

This guide decouples local development from Aspire + Infisical orchestration by using .NET User Secrets for local configuration. After setup, you can:

- Run `Explore.API` standalone with `dotnet run`
- Run `Explore.Blazor` standalone with `dotnet run`
- Generate API clients without starting the full Aspire stack
- Keep Infisical for orchestration (AppHost) and production deployments only

---

## Architecture Change

```
BEFORE (Tightly Coupled):
+----------------------------------------------------------+
|                    Aspire AppHost                         |
|  +----------+   +----------+   +----------------------+   |
|  | Infisical|--->|  Secrets |--->| API / Blazor / etc |   |
|  +----------+   +----------+   +----------------------+   |
+----------------------------------------------------------+
     Must run AppHost to run ANY service

AFTER (Decoupled):
+----------------------------------------------------------+
| Local Development (User Secrets)                          |
|  +----------+        +----------+                         |
|  |   API    |<------>|PostgreSQL|  (direct connection)    |
|  +----------+        +----------+                         |
|  +----------+        +----------+                         |
|  |  Blazor  |<------>|   API    |  (direct connection)    |
|  +----------+        +----------+                         |
+----------------------------------------------------------+
     Each service runs independently

+----------------------------------------------------------+
| Integration / Production (Keep Infisical)                 |
|  +----------+   +----------+   +----------------------+   |
|  | Infisical|--->| AppHost  |--->| Full Stack Testing |   |
|  +----------+   +----------+   +----------------------+   |
+----------------------------------------------------------+
```

---

## Prerequisites

1. **PostgreSQL Database** running locally or accessible remotely
2. **Keycloak Instance** (use existing `keycloak.openislamu.org` for dev)
3. **.NET 10 SDK** installed
4. **Existing Infisical secrets** (to copy values from)

---

## Step 1: Get Current Secret Values from Infisical

Before setting up User Secrets, retrieve current values from Infisical. You can either:

**Option A: From Infisical Web UI**
1. Go to `https://infisical.openislamu.org`
2. Navigate to your project -> `dev` environment
3. Copy values from paths: `/keycloak`, `/api`, `/blazor`, `/postgresql`

**Option B: From Infisical CLI**
```powershell
# Install Infisical CLI if not installed
# https://infisical.com/docs/cli/overview

# Login
infisical login

# Export all secrets (replace with your project ID)
infisical export --projectId=YOUR_PROJECT_ID --env=dev --format=dotenv > secrets.env

# View specific paths
infisical secrets --projectId=YOUR_PROJECT_ID --env=dev --path=/postgresql
infisical secrets --projectId=YOUR_PROJECT_ID --env=dev --path=/keycloak
infisical secrets --projectId=YOUR_PROJECT_ID --env=dev --path=/api
infisical secrets --projectId=YOUR_PROJECT_ID --env=dev --path=/blazor
```

---

## Step 2: Configure Explore.API User Secrets

### Initialize User Secrets (if not already)

```powershell
cd Explore.API
dotnet user-secrets init  # Skip if already initialized (UserSecretsId exists in .csproj)
```

### Set Required Secrets

```powershell
cd Explore.API

# ============================================
# DATABASE (REQUIRED)
# ============================================
# Replace with your actual PostgreSQL connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=5432;Database=explore_dev;User Id=postgres;Password=YOUR_PASSWORD;Include Error Detail=true"

# ============================================
# KEYCLOAK AUTHENTICATION (REQUIRED)
# ============================================
dotnet user-secrets set "Keycloak:Realm" "islamu-dev"
dotnet user-secrets set "Keycloak:Authority" "https://keycloak.openislamu.org/realms/islamu-dev"
dotnet user-secrets set "Keycloak:MetadataAddress" "https://keycloak.openislamu.org/realms/islamu-dev/.well-known/openid-configuration"
dotnet user-secrets set "Keycloak:AuthorizationUrl" "https://keycloak.openislamu.org/realms/islamu-dev/protocol/openid-connect/auth"
dotnet user-secrets set "Keycloak:RequireHttpsMetadata" "true"

# ============================================
# S3 OBJECT STORAGE (OPTIONAL - for file uploads)
# ============================================
# Skip these if you don't need file upload functionality during development
dotnet user-secrets set "S3Settings:Region" "YOUR_REGION"
dotnet user-secrets set "S3Settings:BucketName" "YOUR_BUCKET_NAME"
dotnet user-secrets set "S3Settings:AccessKeyId" "YOUR_ACCESS_KEY"
dotnet user-secrets set "S3Settings:SecretAccessKey" "YOUR_SECRET_KEY"
dotnet user-secrets set "S3Settings:Endpoint" "YOUR_S3_ENDPOINT"
dotnet user-secrets set "S3Settings:PublicEndpoint" "YOUR_S3_PUBLIC_ENDPOINT"

# ============================================
# EMAIL (OPTIONAL - for email notifications)
# ============================================
# Skip if not testing email functionality
dotnet user-secrets set "EmailSettings:ApiKey" "YOUR_EMAIL_API_KEY"
dotnet user-secrets set "EmailSettings:FromAddress" "contact@openislamu.org"
dotnet user-secrets set "EmailSettings:FromName" "Explore Dev"

# ============================================
# DEPLOYMENT MODE (OPTIONAL)
# ============================================
# Default is MultiTenant - set to SingleTenant for simpler local dev
dotnet user-secrets set "Deployment:Mode" "SingleTenant"
dotnet user-secrets set "Deployment:DefaultTenantId" "018e4e5c-7f00-7000-8000-000000000001"
```

### Verify Secrets Were Set

```powershell
cd Explore.API
dotnet user-secrets list
```

Expected output:
```
ConnectionStrings:DefaultConnection = Server=localhost;Port=5432;...
Keycloak:Authority = https://keycloak.openislamu.org/realms/islamu-dev
Keycloak:AuthorizationUrl = https://keycloak.openislamu.org/realms/islamu-dev/protocol/openid-connect/auth
Keycloak:MetadataAddress = https://keycloak.openislamu.org/realms/islamu-dev/.well-known/openid-configuration
Keycloak:Realm = islamu-dev
Keycloak:RequireHttpsMetadata = true
...
```

---

## Step 3: Configure Explore.Blazor User Secrets

### Initialize User Secrets (if not already)

```powershell
cd Explore.Blazor
dotnet user-secrets init  # Skip if already initialized
```

### Set Required Secrets

```powershell
cd Explore.Blazor

# ============================================
# KEYCLOAK AUTHENTICATION (REQUIRED)
# ============================================
dotnet user-secrets set "Keycloak:Authority" "https://keycloak.openislamu.org/realms/islamu-dev"
dotnet user-secrets set "Keycloak:ClientId" "explore-blazor-server"
dotnet user-secrets set "Keycloak:ClientSecret" "YOUR_BLAZOR_CLIENT_SECRET"
dotnet user-secrets set "Keycloak:RequireHttpsMetadata" "true"

# ============================================
# API BASE URL (REQUIRED)
# ============================================
# Points to locally running API
dotnet user-secrets set "ExploreApi:BaseUrl" "https://localhost:7039/"

# ============================================
# MULTI-TENANCY (OPTIONAL)
# ============================================
dotnet user-secrets set "Explore:MultiTenancy:DefaultTenantId" "018e4e5c-7f00-7000-8000-000000000001"
```

### Verify Secrets Were Set

```powershell
cd Explore.Blazor
dotnet user-secrets list
```

---

## Step 4: Configure Event.MigrationService User Secrets (Optional)

Only needed if you want to run migrations independently.

```powershell
cd Event.MigrationService

# DATABASE (REQUIRED)
dotnet user-secrets set "ConnectionStrings:EventMigrationService" "Server=localhost;Port=5432;Database=explore_dev;User Id=postgres;Password=YOUR_PASSWORD;Include Error Detail=true"
```

---

## Step 5: Test Independent Execution

### Run API Independently

```powershell
cd Explore.API
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7039
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5035
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Verify API is working:**
```powershell
# Health check
curl https://localhost:7039/health

# Swagger UI
# Open browser: https://localhost:7039/scalar/v1
```

### Run Blazor Independently

```powershell
# First, ensure API is running (in another terminal)
cd Explore.API
dotnet run

# Then run Blazor
cd Explore.Blazor
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7177
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5144
```

**Verify Blazor is working:**
```powershell
# Open browser: https://localhost:7177
```

---

## Step 6: Create API Client Generation Script

Create a PowerShell script to regenerate the API client without Aspire:

### Create Script File

```powershell
# Create scripts directory if it doesn't exist
New-Item -ItemType Directory -Force -Path scripts
```

Create `scripts/update-api-client.ps1`:

```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Regenerates the Blazor API client from the running API's OpenAPI schema.
    
.DESCRIPTION
    This script:
    1. Starts Explore.API temporarily
    2. Waits for it to be healthy
    3. Downloads the OpenAPI schema (swagger.json)
    4. Regenerates the API client using NSwag
    5. Stops the API
    
.EXAMPLE
    ./scripts/update-api-client.ps1
#>

$ErrorActionPreference = "Stop"

# Configuration
$ApiProjectPath = "Explore.API"
$ApiUrl = "https://localhost:7039"
$SwaggerUrl = "$ApiUrl/swagger/swagger.json"
$OutputSwaggerPath = "Explore.API/swagger.json"
$ClientOutputPath = "Explore.Blazor.Client/Clients/EventApiClient.g.cs"
$MaxWaitSeconds = 60
$PollIntervalSeconds = 2

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  API Client Generation Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if API is already running
Write-Host "[1/6] Checking if API is already running..." -ForegroundColor Yellow
$apiAlreadyRunning = $false
try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/health" -TimeoutSec 5 -SkipCertificateCheck -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        $apiAlreadyRunning = $true
        Write-Host "  API is already running" -ForegroundColor Green
    }
} catch {
    Write-Host "  -> API is not running, will start it" -ForegroundColor Gray
}

$apiProcess = $null

if (-not $apiAlreadyRunning) {
    # Start the API
    Write-Host "[2/6] Starting Explore.API..." -ForegroundColor Yellow
    Push-Location $ApiProjectPath
    try {
        $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build" -PassThru -WindowStyle Hidden
        Write-Host "  API process started (PID: $($apiProcess.Id))" -ForegroundColor Green
    } finally {
        Pop-Location
    }

    # Wait for API to be healthy
    Write-Host "[3/6] Waiting for API to be healthy..." -ForegroundColor Yellow
    $elapsed = 0
    $healthy = $false
    
    while ($elapsed -lt $MaxWaitSeconds) {
        try {
            $response = Invoke-WebRequest -Uri "$ApiUrl/health" -TimeoutSec 5 -SkipCertificateCheck -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $healthy = $true
                break
            }
        } catch {
            # Ignore errors, keep polling
        }
        
        Start-Sleep -Seconds $PollIntervalSeconds
        $elapsed += $PollIntervalSeconds
        Write-Host "  -> Waiting... ($elapsed/$MaxWaitSeconds seconds)" -ForegroundColor Gray
    }

    if (-not $healthy) {
        Write-Host "  API failed to become healthy within $MaxWaitSeconds seconds" -ForegroundColor Red
        if ($apiProcess -and -not $apiProcess.HasExited) {
            Stop-Process -Id $apiProcess.Id -Force
        }
        exit 1
    }
    
    Write-Host "  API is healthy" -ForegroundColor Green
} else {
    Write-Host "[2/6] Skipping API start (already running)" -ForegroundColor Gray
    Write-Host "[3/6] Skipping health check (already running)" -ForegroundColor Gray
}

# Download swagger.json
Write-Host "[4/6] Downloading OpenAPI schema..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $SwaggerUrl -OutFile $OutputSwaggerPath -SkipCertificateCheck
    Write-Host "  Downloaded to $OutputSwaggerPath" -ForegroundColor Green
} catch {
    Write-Host "  Failed to download swagger.json: $_" -ForegroundColor Red
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
    }
    exit 1
}

# Generate API client
Write-Host "[5/6] Generating API client..." -ForegroundColor Yellow

# Check if NSwag is available
$nswagPath = Get-Command "nswag" -ErrorAction SilentlyContinue
if (-not $nswagPath) {
    Write-Host "  -> NSwag CLI not found, attempting to use dotnet tool..." -ForegroundColor Gray
    
    # Try dotnet tool
    $result = dotnet tool list -g | Select-String "nswag"
    if (-not $result) {
        Write-Host "  -> Installing NSwag as global tool..." -ForegroundColor Gray
        dotnet tool install -g NSwag.ConsoleCore
    }
    
    $nswagCommand = "nswag"
} else {
    $nswagCommand = $nswagPath.Source
}

# Check for nswag.json configuration
if (Test-Path "nswag.json") {
    Write-Host "  -> Using nswag.json configuration" -ForegroundColor Gray
    & $nswagCommand run nswag.json
} else {
    Write-Host "  -> Generating with inline parameters" -ForegroundColor Gray
    & $nswagCommand openapi2csclient `
        /input:$OutputSwaggerPath `
        /output:$ClientOutputPath `
        /namespace:Explore.Blazor.Client.Clients `
        /className:EventApiClient `
        /generateClientInterfaces:true `
        /generateExceptionClasses:true `
        /exceptionClass:ApiException `
        /useBaseUrl:false `
        /injectHttpClient:true
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "  Client generated at $ClientOutputPath" -ForegroundColor Green
} else {
    Write-Host "  Client generation failed" -ForegroundColor Red
}

# Stop the API (only if we started it)
Write-Host "[6/6] Cleaning up..." -ForegroundColor Yellow
if ($apiProcess -and -not $apiProcess.HasExited) {
    Stop-Process -Id $apiProcess.Id -Force
    Write-Host "  API process stopped" -ForegroundColor Green
} else {
    Write-Host "  -> API was already running, leaving it running" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Done! API client has been regenerated" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Check for compile errors in Explore.Blazor.Client" -ForegroundColor Gray
Write-Host "  2. Update Blazor components to match new API contract" -ForegroundColor Gray
Write-Host "  3. Run full integration test with Aspire when ready" -ForegroundColor Gray
```

### Make Script Executable

```powershell
# On Windows, PowerShell scripts are executable by default
# On Linux/Mac with PowerShell Core:
chmod +x scripts/update-api-client.ps1
```

---

## Step 7: New Development Workflow

### Scenario: Making Breaking API Changes

1. **Make API changes** in `Explore.API`
   - Modify controllers, DTOs, etc.

2. **Build API to ensure it compiles**
   ```powershell
   cd Explore.API
   dotnet build
   ```

3. **Run the client generation script**
   ```powershell
   ./scripts/update-api-client.ps1
   ```

4. **Fix compile errors** in `Explore.Blazor.Client`
   - IDE shows immediate feedback
   - Update components to match new API contract

5. **Test individually**
   ```powershell
   # Terminal 1: Run API
   cd Explore.API && dotnet run
   
   # Terminal 2: Run Blazor
   cd Explore.Blazor && dotnet run
   ```

6. **Integration test** (when ready)
   ```powershell
   # Run full stack with Aspire
   cd Explore.AppHost && dotnet run
   ```

---

## Troubleshooting

### "Connection string not found"

```
System.InvalidOperationException: 'ConnectionStrings:DefaultConnection' is null
```

**Fix:** Ensure you set the connection string in user-secrets:
```powershell
cd Explore.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
```

### "Keycloak authentication failed"

```
System.Exception: Unable to retrieve document from: https://keycloak.../well-known/openid-configuration
```

**Fix:** 
1. Verify Keycloak is accessible: `curl https://keycloak.openislamu.org/realms/islamu-dev/.well-known/openid-configuration`
2. Check your secrets are correctly set:
   ```powershell
   cd Explore.API
   dotnet user-secrets list | Select-String "Keycloak"
   ```

### "Certificate error" when running locally

```
The SSL connection could not be established
```

**Fix:** Trust the development certificate:
```powershell
dotnet dev-certs https --trust
```

### "Port already in use"

```
System.IO.IOException: Failed to bind to address https://127.0.0.1:7039
```

**Fix:** Kill existing process or use different port:
```powershell
# Find process using port
netstat -ano | findstr :7039

# Kill it
taskkill /PID <PID> /F

# Or change port in launchSettings.json
```

### "User secrets not being read"

**Fix:** Ensure you're running in Development environment:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

---

## Secret Locations Reference

User secrets are stored at:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- **Linux/Mac:** `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

| Project | UserSecretsId |
|---------|---------------|
| Explore.API | `e88f0772-e883-4ace-8ffe-13303d1355eb` |
| Explore.Blazor | `5e896b2d-4efd-42cc-8b6f-f6cc3c5e4c7a` |
| Event.MigrationService | `dotnet-Event.MigrationService-3ef3345d-919b-42f0-8c02-2e81a55d3240` |
| Explore.AppHost | `cf2496b7-15ad-4167-8905-1b67f7d20442` |

---

## Summary: What Changed

| Component | Before | After |
|-----------|--------|-------|
| **Explore.API** | Requires AppHost + Infisical | Runs standalone with User Secrets |
| **Explore.Blazor** | Requires AppHost + Infisical | Runs standalone with User Secrets |
| **AppHost** | Required for any development | Only needed for integration testing |
| **Infisical** | Required for local dev | Only for AppHost/Docker/Production |
| **Client Generation** | Manual, painful process | Automated script |

---

## Quick Reference Commands

```powershell
# Run API independently
cd Explore.API && dotnet run

# Run Blazor independently (API must be running)
cd Explore.Blazor && dotnet run

# Regenerate API client
./scripts/update-api-client.ps1

# List all secrets for a project
cd Explore.API && dotnet user-secrets list

# Clear all secrets for a project
cd Explore.API && dotnet user-secrets clear

# Run with Aspire (full integration)
cd Explore.AppHost && dotnet run
```

---

**Remember:** User Secrets are for local development only. Production and staging environments should continue using Infisical through AppHost or Docker.
