# Secret Management Implementation Plan (Enterprise-Grade)

> **Purpose**: Implement a unified, secret-manager-agnostic configuration system with database-backed dynamic settings, secure bootstrap, key rotation, and enterprise observability.
>
> **Date**: January 2026
> **Status**: Planning
> **Version**: 2.0 (Refactored based on security review)

---

## Executive Summary

This plan creates an **enterprise-grade layered configuration architecture** that:

1. **Solves bootstrap trust** - Uses cloud-native identity (Managed Identity, IRSA, AppRole) to avoid credential chicken-and-egg
2. **Supports key rotation and versioning** - Key version metadata, staged re-encryption, zero-downtime rotation
3. **Enforces fail-fast in production** - `ValidateOnStart` for required secrets, health check integration
4. **Uses robust refresh patterns** - `IHostedService` with `PeriodicTimer`, exponential backoff, jittered intervals
5. **Provides observability and audit** - Prometheus metrics, health endpoints, structured audit logging
6. **Handles connection pool invalidation** - Atomic client swap patterns for secret rotation
7. **Constrains database secrets** - Only operational settings in DB, high-value secrets in external managers

---

## Architecture Overview

```
Configuration Priority (Highest to Lowest):
+------------------------------------------------------------------+
|  1. DATABASE (Operational Settings ONLY)                          |
|     - Admin-managed: SMTP, feature flags, UI customization        |
|     - Encrypted with versioned keys (supports rotation)           |
|     - NEVER: DB connection, Master Key, Keycloak secrets          |
|     - Live reload via IOptionsMonitor                             |
+------------------------------------------------------------------+
          |
          v (fallback if not in DB)
+------------------------------------------------------------------+
|  2. SECRET MANAGER (High-Value Secrets)                           |
|     - Infisical / Vault / Azure KV / AWS SM                       |
|     - Bootstrap secrets: DB connection, Master Key, API keys      |
|     - Cloud-native auth: Managed Identity, IRSA, AppRole          |
|     - Token/lease renewal with jittered backoff                   |
+------------------------------------------------------------------+
          |
          v (fallback if no secret manager configured)
+------------------------------------------------------------------+
|  3. ENVIRONMENT VARIABLES (Self-Hoster Fallback)                  |
|     - Plain env vars set by user                                  |
|     - Docker Compose, Kubernetes, systemd, etc.                   |
|     - Zero external dependencies                                  |
+------------------------------------------------------------------+
          |
          v (fallback for defaults)
+------------------------------------------------------------------+
|  4. APPSETTINGS.JSON (Non-Sensitive Defaults Only)                |
|     - Logging configuration                                       |
|     - Feature toggles with SAFE defaults (disabled)               |
|     - NEVER: Any secrets or credentials                           |
+------------------------------------------------------------------+
```

---

## Critical Security Gaps Addressed

### 1. Bootstrap Trust Problem (SOLVED)

**Problem**: How to fetch master key and secret manager credentials on first boot without hardcoding?

**Solution**: Cloud-native identity + envelope encryption

```
┌─────────────────────────────────────────────────────────────────┐
│                    BOOTSTRAP FLOW                                │
├─────────────────────────────────────────────────────────────────┤
│  AZURE (Managed Identity):                                       │
│  1. App starts with NO credentials in code/env                   │
│  2. DefaultAzureCredential auto-discovers Managed Identity       │
│  3. Fetches bootstrap secrets from Key Vault                     │
│  4. Uses secrets to initialize DB, other services                │
├─────────────────────────────────────────────────────────────────┤
│  AWS (IRSA - IAM Roles for Service Accounts):                    │
│  1. Pod service account annotated with IAM role                  │
│  2. AWS SDK auto-discovers IRSA credentials                      │
│  3. Fetches bootstrap secrets from Secrets Manager               │
│  4. Uses secrets to initialize DB, other services                │
├─────────────────────────────────────────────────────────────────┤
│  VAULT (AppRole with Response Wrapping):                         │
│  1. Orchestrator provisions wrapped SecretID (single-use)        │
│  2. App unwraps token to get SecretID                            │
│  3. App authenticates with RoleID + SecretID                     │
│  4. Fetches bootstrap secrets from Vault KV                      │
├─────────────────────────────────────────────────────────────────┤
│  INFISICAL (Machine Identity):                                   │
│  1. Machine Identity ClientID/Secret from env (minimal trust)    │
│  2. SDK authenticates and fetches all secrets                    │
│  3. Secrets injected into configuration                          │
├─────────────────────────────────────────────────────────────────┤
│  SELF-HOSTED (Environment Variables):                            │
│  1. User manually sets all required env vars                     │
│  2. No secret manager dependency                                 │
│  3. Suitable for small deployments                               │
└─────────────────────────────────────────────────────────────────┘
```

### 2. Key Rotation and Versioning (SOLVED)

**Problem**: No key version metadata, no re-encryption workflow.

**Solution**: Key version envelope with staged rotation

```csharp
// AppSetting entity with key versioning
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string EncryptedValue { get; set; } = string.Empty;
    
    // KEY VERSIONING (NEW)
    public int KeyVersion { get; set; } = 1;  // Which master key version encrypted this
    public DateTime EncryptedAt { get; set; }
    
    // AUDIT (NEW)
    public Guid? EncryptedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // CONCURRENCY (NEW)
    public byte[] RowVersion { get; set; } = null!;  // EF Core concurrency token
    
    // METADATA
    public bool IsSensitive { get; set; } = true;
    public string? Description { get; set; }
    public string? Category { get; set; }
}

// Encryption service with key versioning
public interface IEncryptionService
{
    (string Ciphertext, int KeyVersion) Encrypt(string plaintext);
    string Decrypt(string ciphertext, int keyVersion);
    int CurrentKeyVersion { get; }
    Task ReEncryptAllAsync(CancellationToken ct);  // For key rotation
}
```

**Rotation Workflow**:
```
1. Admin generates new master key (version N+1)
2. New key stored in secret manager alongside old key (version N)
3. Background job re-encrypts all AppSettings with version N+1
4. After all re-encrypted, old key (version N) marked for deletion
5. Grace period, then old key deleted
```

### 3. Fail-Fast and Validation (SOLVED)

**Problem**: Fallback chain can mask misconfiguration in production.

**Solution**: `ValidateOnStart` + environment-aware fail-fast + health checks

```csharp
// Required secrets validation
public class RequiredSecretsValidator : IValidateOptions<SecretProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, SecretProviderOptions options)
    {
        var errors = new List<string>();
        
        // Always required
        if (string.IsNullOrEmpty(options.DatabaseConnectionString))
            errors.Add("Database:ConnectionString is required");
        
        // Required if using secret manager
        if (options.Provider != SecretProviderType.None)
        {
            switch (options.Provider)
            {
                case SecretProviderType.Infisical:
                    if (string.IsNullOrEmpty(options.InfisicalClientId))
                        errors.Add("Infisical:ClientId is required when using Infisical");
                    if (string.IsNullOrEmpty(options.InfisicalClientSecret))
                        errors.Add("Infisical:ClientSecret is required when using Infisical");
                    break;
                // ... other providers
            }
        }
        
        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}

// In Program.cs
builder.Services.AddOptions<SecretProviderOptions>()
    .BindConfiguration("SecretProvider")
    .ValidateOnStart();  // CRITICAL: Fail at startup, not at first use
```

### 4. Robust Refresh with IHostedService (SOLVED)

**Problem**: Timer with async fire-and-forget causes overlapping loads and swallowed exceptions.

**Solution**: `IHostedService` with `PeriodicTimer`, serialized runs, exponential backoff

```csharp
// Location: Explore.Secrets/Services/SecretRefreshService.cs

namespace Explore.Secrets.Services;

/// <summary>
/// ABOUTME: Background service that periodically refreshes secrets from external providers.
/// Uses PeriodicTimer for accurate intervals, serializes runs, and implements exponential backoff.
/// </summary>
public sealed class SecretRefreshService : BackgroundService
{
    private readonly ISecretProvider _secretProvider;
    private readonly IConfigurationRoot _configurationRoot;
    private readonly ILogger<SecretRefreshService> _logger;
    private readonly SecretRefreshOptions _options;
    private readonly SecretRefreshMetrics _metrics;
    
    private int _consecutiveFailures;
    private static readonly Random _jitter = new();

    public SecretRefreshService(
        ISecretProvider secretProvider,
        IConfiguration configuration,
        ILogger<SecretRefreshService> logger,
        IOptions<SecretRefreshOptions> options,
        SecretRefreshMetrics metrics)
    {
        _secretProvider = secretProvider;
        _configurationRoot = (IConfigurationRoot)configuration;
        _logger = logger;
        _options = options.Value;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Secret refresh service starting. Interval: {Interval}", _options.RefreshInterval);
        
        // Add jitter to initial delay to prevent thundering herd in multi-instance deployments
        var initialDelay = _options.RefreshInterval + TimeSpan.FromMilliseconds(_jitter.Next(0, 5000));
        
        using var timer = new PeriodicTimer(initialDelay);
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var activity = _metrics.StartRefreshActivity();
            
            try
            {
                _logger.LogDebug("Starting secret refresh cycle");
                var stopwatch = Stopwatch.StartNew();
                
                await _secretProvider.RefreshAsync(stoppingToken);
                
                // Trigger configuration reload
                foreach (var provider in _configurationRoot.Providers)
                {
                    if (provider is IDisposable disposable)
                    {
                        // Reload supported providers
                        provider.Load();
                    }
                }
                
                stopwatch.Stop();
                _consecutiveFailures = 0;
                
                _metrics.RecordRefreshSuccess(stopwatch.Elapsed);
                _logger.LogInformation("Secret refresh completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
                
                // Reset timer to normal interval after success
                timer.Period = AddJitter(_options.RefreshInterval);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Secret refresh service stopping");
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                _metrics.RecordRefreshFailure(ex);
                
                var backoffDelay = CalculateBackoffDelay();
                _logger.LogError(ex, 
                    "Secret refresh failed (attempt {Attempt}). Next retry in {Delay}",
                    _consecutiveFailures, backoffDelay);
                
                // Apply backoff for next iteration
                timer.Period = backoffDelay;
            }
        }
    }

    private TimeSpan CalculateBackoffDelay()
    {
        // Exponential backoff: base * 2^failures, capped at max
        var exponentialDelay = _options.BaseBackoffDelay * Math.Pow(2, Math.Min(_consecutiveFailures, 6));
        var cappedDelay = Math.Min(exponentialDelay.TotalMilliseconds, _options.MaxBackoffDelay.TotalMilliseconds);
        
        return AddJitter(TimeSpan.FromMilliseconds(cappedDelay));
    }

    private static TimeSpan AddJitter(TimeSpan baseInterval)
    {
        // Add 0-10% jitter to prevent thundering herd
        var jitterMs = _jitter.Next(0, (int)(baseInterval.TotalMilliseconds * 0.1));
        return baseInterval + TimeSpan.FromMilliseconds(jitterMs);
    }
}

public class SecretRefreshOptions
{
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan BaseBackoffDelay { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan MaxBackoffDelay { get; set; } = TimeSpan.FromMinutes(5);
}
```

### 5. Observability and Audit (SOLVED)

**Problem**: No metrics, health checks, or audit trail.

**Solution**: Prometheus metrics, health endpoints, structured audit logging

```csharp
// Location: Explore.Secrets/Observability/SecretRefreshMetrics.cs

namespace Explore.Secrets.Observability;

/// <summary>
/// ABOUTME: Prometheus metrics for secret refresh operations.
/// Tracks refresh success/failure rates, latency, and provider health.
/// </summary>
public class SecretRefreshMetrics
{
    private readonly Counter<long> _refreshTotal;
    private readonly Counter<long> _refreshFailures;
    private readonly Histogram<double> _refreshDuration;
    private readonly ObservableGauge<long> _lastRefreshTimestamp;
    private readonly ObservableGauge<int> _consecutiveFailures;
    
    private long _lastSuccessfulRefresh;
    private int _currentConsecutiveFailures;

    public SecretRefreshMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Explore.Secrets");
        
        _refreshTotal = meter.CreateCounter<long>(
            "secrets_refresh_total",
            description: "Total number of secret refresh attempts");
        
        _refreshFailures = meter.CreateCounter<long>(
            "secrets_refresh_failures_total",
            description: "Total number of failed secret refresh attempts");
        
        _refreshDuration = meter.CreateHistogram<double>(
            "secrets_refresh_duration_seconds",
            unit: "seconds",
            description: "Duration of secret refresh operations");
        
        _lastRefreshTimestamp = meter.CreateObservableGauge(
            "secrets_last_refresh_timestamp",
            () => _lastSuccessfulRefresh,
            description: "Unix timestamp of last successful refresh");
        
        _consecutiveFailures = meter.CreateObservableGauge(
            "secrets_consecutive_failures",
            () => _currentConsecutiveFailures,
            description: "Current number of consecutive refresh failures");
    }

    public Activity? StartRefreshActivity()
    {
        _refreshTotal.Add(1);
        return Activity.Current?.Source.StartActivity("SecretRefresh");
    }

    public void RecordRefreshSuccess(TimeSpan duration)
    {
        _refreshDuration.Record(duration.TotalSeconds);
        _lastSuccessfulRefresh = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _currentConsecutiveFailures = 0;
    }

    public void RecordRefreshFailure(Exception ex)
    {
        _refreshFailures.Add(1, new KeyValuePair<string, object?>("exception_type", ex.GetType().Name));
        _currentConsecutiveFailures++;
    }
}

// Health check for secret provider
public class SecretProviderHealthCheck : IHealthCheck
{
    private readonly ISecretProvider _provider;
    private readonly SecretRefreshMetrics _metrics;
    private readonly ILogger<SecretProviderHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to fetch a known secret (or use a health-check specific key)
            var result = await _provider.GetSecretAsync("_health_check", cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["provider"] = _provider.GetType().Name,
                ["supports_refresh"] = _provider.SupportsRefresh,
                ["last_refresh"] = DateTimeOffset.FromUnixTimeSeconds(_metrics.LastRefreshTimestamp)
            };
            
            return HealthCheckResult.Healthy("Secret provider is reachable", data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Secret provider health check failed");
            return HealthCheckResult.Unhealthy("Secret provider is unreachable", ex);
        }
    }
}

// Audit logging for secret access
public class AuditingSecretProvider : ISecretProvider
{
    private readonly ISecretProvider _inner;
    private readonly ILogger<AuditingSecretProvider> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ?? "system";
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "SecretAccess: Key={SecretKey}, User={UserId}, CorrelationId={CorrelationId}, Timestamp={Timestamp}",
            RedactKey(key), userId, correlationId, DateTimeOffset.UtcNow);
        
        return await _inner.GetSecretAsync(key, ct);
    }

    private static string RedactKey(string key)
    {
        // Redact sensitive parts of key names for logging
        if (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("key", StringComparison.OrdinalIgnoreCase))
        {
            var parts = key.Split(':');
            return string.Join(":", parts.Select((p, i) => i == parts.Length - 1 ? "***" : p));
        }
        return key;
    }
}
```

### 6. Provider-Specific Token/Lease Renewal (SOLVED)

**Problem**: Plan doesn't cover Vault token renewal, Azure MSI retries, AWS STS refresh.

**Solution**: Per-provider auth refresh with jittered backoff

```csharp
// Location: Explore.Secrets/Providers/VaultSecretProvider.cs

namespace Explore.Secrets.Providers;

/// <summary>
/// ABOUTME: HashiCorp Vault secret provider with AppRole auth and token renewal.
/// Handles lease management, token TTL, and automatic re-authentication.
/// </summary>
public class VaultSecretProvider : ISecretProvider, IAsyncDisposable
{
    private readonly VaultProviderOptions _options;
    private readonly ILogger<VaultSecretProvider> _logger;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    
    private IVaultClient? _client;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private Timer? _renewalTimer;

    public async Task InitializeAsync(CancellationToken ct)
    {
        await AuthenticateAsync(ct);
        StartTokenRenewalTimer();
    }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        await _authLock.WaitAsync(ct);
        try
        {
            _logger.LogInformation("Authenticating with Vault using AppRole");
            
            // AppRole authentication with response wrapping (enterprise pattern)
            IAuthMethodInfo authMethod;
            
            if (!string.IsNullOrEmpty(_options.WrappedSecretIdPath))
            {
                // Production: Use wrapped SecretID from orchestrator
                var wrappingToken = await File.ReadAllTextAsync(_options.WrappedSecretIdPath, ct);
                var unwrapClient = new VaultClient(new VaultClientSettings(
                    _options.VaultUrl, 
                    new TokenAuthMethodInfo(wrappingToken)));
                
                var secretIdData = await unwrapClient.V1.System
                    .UnwrapWrappedResponseDataAsync<Dictionary<string, object>>(null);
                var secretId = secretIdData.Data["secret_id"].ToString();
                
                authMethod = new AppRoleAuthMethodInfo(_options.RoleId, secretId);
            }
            else
            {
                // Development: Use direct SecretID (less secure)
                authMethod = new AppRoleAuthMethodInfo(_options.RoleId, _options.SecretId);
            }
            
            var settings = new VaultClientSettings(_options.VaultUrl, authMethod);
            _client = new VaultClient(settings);
            
            // Get token info to track expiry
            var tokenInfo = await _client.V1.Auth.Token.LookupSelfAsync();
            var ttl = tokenInfo.Data.TimeToLive;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(ttl);
            
            _logger.LogInformation("Vault authentication successful. Token expires at {Expiry}", _tokenExpiry);
        }
        finally
        {
            _authLock.Release();
        }
    }

    private void StartTokenRenewalTimer()
    {
        // Renew token at 75% of TTL to avoid expiry during operations
        var renewalInterval = (_tokenExpiry - DateTime.UtcNow) * 0.75;
        if (renewalInterval < TimeSpan.FromMinutes(1))
            renewalInterval = TimeSpan.FromMinutes(1);
        
        _renewalTimer?.Dispose();
        _renewalTimer = new Timer(async _ =>
        {
            try
            {
                await RenewTokenAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token renewal failed, will re-authenticate");
                await AuthenticateAsync(CancellationToken.None);
            }
        }, null, renewalInterval, Timeout.InfiniteTimeSpan);
    }

    private async Task RenewTokenAsync(CancellationToken ct)
    {
        await _authLock.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Renewing Vault token");
            var result = await _client!.V1.Auth.Token.RenewSelfAsync();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(result.AuthInfo.LeaseDurationSeconds);
            
            StartTokenRenewalTimer();  // Schedule next renewal
            _logger.LogInformation("Vault token renewed. New expiry: {Expiry}", _tokenExpiry);
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        
        var path = MapKeyToVaultPath(key);
        var secret = await _client!.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: path);
        
        var fieldName = ExtractFieldName(key);
        return secret.Data.Data.TryGetValue(fieldName, out var value) ? value?.ToString() : null;
    }

    private void EnsureAuthenticated()
    {
        if (_client == null || DateTime.UtcNow >= _tokenExpiry)
            throw new InvalidOperationException("Vault client not authenticated or token expired");
    }

    public async ValueTask DisposeAsync()
    {
        _renewalTimer?.Dispose();
        _authLock.Dispose();
    }
}
```

### 7. Client Connection Pool Invalidation (SOLVED)

**Problem**: HttpClient and DB pools need re-connect on secret rotation.

**Solution**: Atomic swap pattern with graceful drain

```csharp
// Location: Explore.Secrets/Services/RotationAwareHttpClientFactory.cs

namespace Explore.Secrets.Services;

/// <summary>
/// ABOUTME: HttpClient factory that supports credential rotation.
/// Implements atomic swap pattern to replace clients when secrets change.
/// </summary>
public class RotationAwareHttpClientFactory : IHttpClientFactory, IDisposable
{
    private readonly IOptionsMonitor<HttpClientCredentials> _credentials;
    private readonly ILogger<RotationAwareHttpClientFactory> _logger;
    private readonly ConcurrentDictionary<string, Lazy<HttpClient>> _clients = new();
    private readonly SemaphoreSlim _rotateLock = new(1, 1);
    
    private IDisposable? _credentialChangeListener;

    public RotationAwareHttpClientFactory(
        IOptionsMonitor<HttpClientCredentials> credentials,
        ILogger<RotationAwareHttpClientFactory> logger)
    {
        _credentials = credentials;
        _logger = logger;
        
        // Listen for credential changes
        _credentialChangeListener = _credentials.OnChange(OnCredentialsChanged);
    }

    private void OnCredentialsChanged(HttpClientCredentials newCredentials, string? name)
    {
        _logger.LogInformation("Credentials changed for {ClientName}, rotating clients", name ?? "default");
        
        Task.Run(async () =>
        {
            await _rotateLock.WaitAsync();
            try
            {
                // Create new client with new credentials
                var newClient = CreateClientInternal(name ?? string.Empty, newCredentials);
                
                // Atomic swap
                var oldClientLazy = _clients.AddOrUpdate(
                    name ?? string.Empty,
                    _ => new Lazy<HttpClient>(() => newClient),
                    (_, old) =>
                    {
                        // Schedule old client disposal after grace period
                        Task.Delay(TimeSpan.FromSeconds(30))
                            .ContinueWith(_ =>
                            {
                                if (old.IsValueCreated)
                                {
                                    _logger.LogDebug("Disposing old HttpClient for {ClientName}", name);
                                    old.Value.Dispose();
                                }
                            });
                        return new Lazy<HttpClient>(() => newClient);
                    });
                
                _logger.LogInformation("HttpClient rotated for {ClientName}", name ?? "default");
            }
            finally
            {
                _rotateLock.Release();
            }
        });
    }

    public HttpClient CreateClient(string name)
    {
        return _clients.GetOrAdd(
            name,
            n => new Lazy<HttpClient>(() => CreateClientInternal(n, _credentials.Get(n)))
        ).Value;
    }

    private HttpClient CreateClientInternal(string name, HttpClientCredentials credentials)
    {
        var handler = new HttpClientHandler();
        var client = new HttpClient(handler);
        
        if (!string.IsNullOrEmpty(credentials.BearerToken))
        {
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", credentials.BearerToken);
        }
        
        return client;
    }

    public void Dispose()
    {
        _credentialChangeListener?.Dispose();
        foreach (var client in _clients.Values.Where(l => l.IsValueCreated))
        {
            client.Value.Dispose();
        }
        _rotateLock.Dispose();
    }
}

// For database connections - use connection string rotation
public class RotationAwareDbContextFactory<TContext> : IDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly IOptionsMonitor<DatabaseOptions> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    
    private string? _currentConnectionString;
    private IDisposable? _changeListener;

    public RotationAwareDbContextFactory(
        IOptionsMonitor<DatabaseOptions> options,
        IServiceProvider serviceProvider,
        ILogger<RotationAwareDbContextFactory<TContext>> logger)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _currentConnectionString = options.CurrentValue.ConnectionString;
        
        _changeListener = _options.OnChange(OnConnectionStringChanged);
    }

    private void OnConnectionStringChanged(DatabaseOptions newOptions, string? name)
    {
        if (_currentConnectionString != newOptions.ConnectionString)
        {
            _logger.LogInformation("Database connection string changed, new contexts will use updated credentials");
            _currentConnectionString = newOptions.ConnectionString;
            
            // Note: Existing pooled connections will drain naturally
            // For immediate rotation, you could call NpgsqlConnection.ClearAllPools()
            // but this is aggressive and may cause brief connection errors
        }
    }

    public TContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseNpgsql(_currentConnectionString);
        
        return ActivatorUtilities.CreateInstance<TContext>(_serviceProvider, optionsBuilder.Options);
    }
}
```

---

## Detailed Design

### 1. Project Structure (Updated)

```
Explore.Secrets/
├── Explore.Secrets.csproj
├── Abstractions/
│   ├── ISecretProvider.cs
│   ├── IEncryptionService.cs
│   ├── SecretProviderType.cs
│   └── ISecretAuditLogger.cs           # NEW: Audit interface
├── Providers/
│   ├── EnvironmentSecretProvider.cs
│   ├── InfisicalSecretProvider.cs
│   ├── VaultSecretProvider.cs           # With token renewal
│   ├── AzureKeyVaultSecretProvider.cs   # With Managed Identity
│   ├── AwsSecretsManagerProvider.cs     # With IRSA support
│   └── AuditingSecretProviderDecorator.cs  # NEW: Audit wrapper
├── Configuration/
│   ├── SecretProviderOptions.cs
│   ├── SecretRefreshOptions.cs          # NEW: Backoff settings
│   ├── DbConfigurationSource.cs
│   ├── DbConfigurationProvider.cs       # With key versioning
│   └── SecretConfigurationSource.cs
├── Services/
│   ├── AesEncryptionService.cs          # With key versioning
│   ├── SecretProviderFactory.cs
│   ├── SecretRefreshService.cs          # NEW: IHostedService
│   ├── KeyRotationService.cs            # NEW: Re-encryption job
│   └── RotationAwareHttpClientFactory.cs  # NEW: Client rotation
├── Observability/
│   ├── SecretRefreshMetrics.cs          # NEW: Prometheus
│   ├── SecretProviderHealthCheck.cs     # NEW: Health check
│   └── SecretAuditLogger.cs             # NEW: Audit logging
├── Validation/
│   └── RequiredSecretsValidator.cs      # NEW: ValidateOnStart
├── Extensions/
│   └── ServiceCollectionExtensions.cs
└── README.md
```

### 2. Core Abstractions (Updated)

```csharp
// ABOUTME: Unified interface for retrieving secrets from any secret manager.
// Implementations handle provider-specific authentication, renewal, and path mapping.

namespace Explore.Secrets.Abstractions;

public interface ISecretProvider
{
    /// <summary>
    /// Initializes the provider (authentication, token acquisition).
    /// Called once at startup.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a secret value by its canonical key.
    /// </summary>
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a secret with version information.
    /// </summary>
    Task<SecretValue?> GetSecretWithMetadataAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets multiple secrets by their canonical keys.
    /// </summary>
    Task<Dictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> keys, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all secrets from a specific path/folder.
    /// </summary>
    Task<Dictionary<string, string>> GetSecretsByPathAsync(
        string path, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Whether this provider supports automatic refresh.
    /// </summary>
    bool SupportsRefresh { get; }
    
    /// <summary>
    /// Refreshes cached secrets from the provider.
    /// Thread-safe, handles concurrent calls.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets provider health information.
    /// </summary>
    Task<ProviderHealthInfo> GetHealthAsync(CancellationToken cancellationToken = default);
}

public record SecretValue(
    string Value,
    string? Version,
    DateTime? CreatedAt,
    DateTime? ExpiresAt,
    IReadOnlyDictionary<string, string>? Metadata);

public record ProviderHealthInfo(
    bool IsHealthy,
    string ProviderName,
    DateTime? LastSuccessfulFetch,
    DateTime? TokenExpiry,
    int? ConsecutiveFailures,
    string? ErrorMessage);
```

### 3. AppSetting Entity (Updated with Key Versioning)

```csharp
// Location: Explore.Domain/AppSetting.cs

namespace Explore.Domain;

/// <summary>
/// ABOUTME: Stores encrypted application settings that can be modified at runtime.
/// Supports key versioning for rotation, concurrency control, and full audit trail.
/// CONSTRAINT: Only operational settings (SMTP, feature flags). NEVER DB connection or master key.
/// </summary>
public class AppSetting
{
    /// <summary>
    /// Configuration key in colon-separated format (e.g., "Smtp:Host")
    /// Primary key, unique constraint enforced.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Encrypted value using AES-256-GCM with versioned key.
    /// Format: base64(nonce + tag + ciphertext)
    /// </summary>
    public string EncryptedValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Which master key version was used to encrypt this value.
    /// Required for key rotation - allows staged re-encryption.
    /// </summary>
    public int KeyVersion { get; set; } = 1;
    
    /// <summary>
    /// When the value was last encrypted (for rotation tracking).
    /// </summary>
    public DateTime EncryptedAt { get; set; }
    
    /// <summary>
    /// Who encrypted this value (for audit).
    /// </summary>
    public Guid? EncryptedBy { get; set; }
    
    /// <summary>
    /// Whether this setting contains sensitive data.
    /// Sensitive = always encrypted; non-sensitive = stored plaintext.
    /// </summary>
    public bool IsSensitive { get; set; } = true;
    
    /// <summary>
    /// Optional description for admin UI.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Category for grouping in admin UI (e.g., "Email", "Storage", "Features").
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Last modified timestamp (for any field change).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Who last modified this setting (for audit).
    /// </summary>
    public Guid? UpdatedBy { get; set; }
    
    /// <summary>
    /// When this setting was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Who created this setting.
    /// </summary>
    public Guid? CreatedBy { get; set; }
    
    /// <summary>
    /// Concurrency token for optimistic locking.
    /// Prevents lost updates when multiple admins edit simultaneously.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
```

### 4. EF Core Configuration (with constraints)

```csharp
// Location: Explore.Persistence/Configurations/Entities/AppSettingConfiguration.cs

namespace Explore.Persistence.Configurations.Entities;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");
        
        // Primary key on Key
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key)
            .HasMaxLength(256)
            .IsRequired();
        
        // Unique index (redundant with PK but explicit)
        builder.HasIndex(e => e.Key)
            .IsUnique()
            .HasDatabaseName("IX_AppSettings_Key");
        
        // Encrypted value
        builder.Property(e => e.EncryptedValue)
            .IsRequired();
        
        // Key versioning
        builder.Property(e => e.KeyVersion)
            .IsRequired()
            .HasDefaultValue(1);
        
        builder.Property(e => e.EncryptedAt)
            .IsRequired();
        
        // Audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired();
        
        builder.Property(e => e.UpdatedAt)
            .IsRequired();
        
        // Concurrency token
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();
        
        // Category index for admin UI queries
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_AppSettings_Category");
        
        // Key version index for rotation queries
        builder.HasIndex(e => e.KeyVersion)
            .HasDatabaseName("IX_AppSettings_KeyVersion");
        
        // Check constraint: prevent storing high-value secrets
        // (This is a safety net; primary enforcement is in application code)
        builder.HasCheckConstraint(
            "CK_AppSettings_NoHighValueSecrets",
            "\"Key\" NOT LIKE 'Database:%' AND \"Key\" NOT LIKE 'Security:MasterKey%'");
    }
}
```

### 5. Encryption Service with Key Versioning

```csharp
// Location: Explore.Secrets/Services/AesEncryptionService.cs

namespace Explore.Secrets.Services;

/// <summary>
/// ABOUTME: AES-256-GCM authenticated encryption with key versioning support.
/// Maintains multiple key versions for rotation, supports re-encryption workflow.
/// Uses CryptographicOperations.ZeroMemory for key material cleanup.
/// </summary>
public sealed class AesEncryptionService : IEncryptionService, IDisposable
{
    private readonly Dictionary<int, byte[]> _keyVersions = new();
    private readonly ILogger<AesEncryptionService> _logger;
    private readonly object _keyLock = new();
    private int _currentVersion;
    private bool _disposed;
    
    private const int NonceSize = 12;  // 96 bits for GCM
    private const int TagSize = 16;    // 128 bits for GCM

    public int CurrentKeyVersion => _currentVersion;

    public AesEncryptionService(
        IOptions<EncryptionOptions> options,
        ILogger<AesEncryptionService> logger)
    {
        _logger = logger;
        
        // Load all key versions
        foreach (var (version, keyBase64) in options.Value.KeyVersions)
        {
            var keyBytes = Convert.FromBase64String(keyBase64);
            if (keyBytes.Length != 32)
                throw new ArgumentException($"Key version {version} must be 256 bits (32 bytes)");
            
            _keyVersions[version] = keyBytes;
        }
        
        _currentVersion = options.Value.CurrentKeyVersion;
        
        if (!_keyVersions.ContainsKey(_currentVersion))
            throw new ArgumentException($"Current key version {_currentVersion} not found in key versions");
        
        _logger.LogInformation("Encryption service initialized with {Count} key versions, current: {Current}",
            _keyVersions.Count, _currentVersion);
    }

    public (string Ciphertext, int KeyVersion) Encrypt(string plaintext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        lock (_keyLock)
        {
            using var aes = new AesGcm(_keyVersions[_currentVersion], TagSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        // Format: nonce + tag + ciphertext
        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

        // Zero sensitive memory
        CryptographicOperations.ZeroMemory(plaintextBytes);

        return (Convert.ToBase64String(result), _currentVersion);
    }

    public string Decrypt(string encryptedBase64, int keyVersion)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (!_keyVersions.TryGetValue(keyVersion, out var key))
            throw new ArgumentException($"Key version {keyVersion} not available");
        
        var data = Convert.FromBase64String(encryptedBase64);
        
        if (data.Length < NonceSize + TagSize)
            throw new ArgumentException("Invalid encrypted data");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[data.Length - NonceSize - TagSize];

        Buffer.BlockCopy(data, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(data, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(data, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];

        lock (_keyLock)
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        var result = Encoding.UTF8.GetString(plaintext);
        
        // Zero sensitive memory
        CryptographicOperations.ZeroMemory(plaintext);

        return result;
    }

    public async Task ReEncryptAllAsync(
        IAppSettingRepository repository,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting re-encryption of all settings to key version {Version}", _currentVersion);
        
        var settings = await repository.GetSettingsNeedingReEncryptionAsync(_currentVersion, cancellationToken);
        var count = 0;
        
        foreach (var setting in settings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                // Decrypt with old key
                var plaintext = Decrypt(setting.EncryptedValue, setting.KeyVersion);
                
                // Re-encrypt with current key
                var (newCiphertext, newVersion) = Encrypt(plaintext);
                
                // Update setting
                setting.EncryptedValue = newCiphertext;
                setting.KeyVersion = newVersion;
                setting.EncryptedAt = DateTime.UtcNow;
                
                await repository.UpdateAsync(setting, cancellationToken);
                count++;
                
                // Zero plaintext
                CryptographicOperations.ZeroMemory(Encoding.UTF8.GetBytes(plaintext));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-encrypt setting {Key}", setting.Key);
                throw;
            }
        }
        
        _logger.LogInformation("Re-encryption complete. {Count} settings updated to key version {Version}",
            count, _currentVersion);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Zero all key material
        lock (_keyLock)
        {
            foreach (var key in _keyVersions.Values)
            {
                CryptographicOperations.ZeroMemory(key);
            }
            _keyVersions.Clear();
        }
    }
}

public class EncryptionOptions
{
    /// <summary>
    /// Map of key version -> base64-encoded 256-bit key.
    /// Multiple versions support rotation.
    /// </summary>
    public Dictionary<int, string> KeyVersions { get; set; } = new();
    
    /// <summary>
    /// Current key version for new encryptions.
    /// </summary>
    public int CurrentKeyVersion { get; set; } = 1;
}
```

### 6. Database Configuration Provider (Robust)

```csharp
// Location: Explore.Secrets/Configuration/DbConfigurationProvider.cs

namespace Explore.Secrets.Configuration;

/// <summary>
/// ABOUTME: Custom configuration provider that loads encrypted settings from database.
/// Uses IHostedService pattern for refresh, serialized loads, and proper error handling.
/// </summary>
public class DbConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DbConfigurationProvider> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    
    private DateTime _lastSuccessfulLoad = DateTime.MinValue;
    private int _consecutiveFailures;

    public DbConfigurationProvider(
        string connectionString,
        IEncryptionService encryptionService,
        ILogger<DbConfigurationProvider> logger)
    {
        _connectionString = connectionString;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public DateTime LastSuccessfulLoad => _lastSuccessfulLoad;
    public int ConsecutiveFailures => _consecutiveFailures;

    public override void Load()
    {
        // Synchronous load for initial startup
        LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        // Serialize concurrent load attempts
        if (!await _loadLock.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            _logger.LogWarning("Load lock acquisition timed out, skipping this refresh cycle");
            return;
        }
        
        try
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            
            var settings = await conn.QueryAsync<AppSettingDto>(
                """
                SELECT "Key", "EncryptedValue", "KeyVersion", "IsSensitive" 
                FROM "AppSettings"
                """);

            foreach (var setting in settings)
            {
                try
                {
                    var value = setting.IsSensitive
                        ? _encryptionService.Decrypt(setting.EncryptedValue, setting.KeyVersion)
                        : setting.EncryptedValue;
                    
                    data[setting.Key] = value;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt setting '{Key}' (KeyVersion: {Version})",
                        setting.Key, setting.KeyVersion);
                    // Continue loading other settings - don't let one failure break everything
                }
            }
            
            Data = data;
            _lastSuccessfulLoad = DateTime.UtcNow;
            _consecutiveFailures = 0;
            
            OnReload();
            
            _logger.LogDebug("Loaded {Count} settings from database", data.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _consecutiveFailures++;
            _logger.LogError(ex, 
                "Failed to load settings from database (attempt {Attempt}). Keeping existing data.",
                _consecutiveFailures);
            
            // On first load failure with no existing data, this is fatal
            if (Data.Count == 0)
            {
                throw new InvalidOperationException(
                    "Database configuration load failed on startup with no cached data", ex);
            }
            // Otherwise, keep existing data and continue
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private record AppSettingDto(string Key, string EncryptedValue, int KeyVersion, bool IsSensitive);
}
```

---

## Secret Categorization (What Goes Where)

### HIGH-VALUE SECRETS (External Secret Manager ONLY)

These secrets **MUST NEVER** be stored in the database:

| Secret | Reason |
|--------|--------|
| `Database:ConnectionString` | Bootstrap dependency - needed to read DB |
| `Security:MasterKey*` | Encrypts DB settings - chicken-and-egg |
| `Keycloak:ClientSecret` | Auth infrastructure |
| `S3:SecretAccessKey` | Cloud credentials |
| Secret manager credentials | Bootstrap dependency |

### OPERATIONAL SETTINGS (Database OK)

These settings are safe for database storage:

| Setting | Reason |
|---------|--------|
| `Smtp:Host`, `Smtp:Port` | Non-critical, admin-changeable |
| `Email:FromAddress`, `Email:FromName` | Non-critical |
| `Features:*` | Feature flags |
| `UI:*` | Customization settings |
| `RateLimiting:*` | Operational tuning |

---

## Implementation Phases (Updated)

### Phase 1: Core Infrastructure with Validation (6-8 hours)

**Tasks:**
1. Create `Explore.Secrets` project with proper structure
2. Implement `ISecretProvider` interface with health info
3. Implement `EnvironmentSecretProvider` (fallback)
4. Implement `RequiredSecretsValidator` with `ValidateOnStart`
5. Implement `SecretProviderFactory`
6. Create `ServiceCollectionExtensions` with proper DI registration
7. Add comprehensive unit tests

**Acceptance Criteria:**
- `ValidateOnStart` fails if required secrets missing
- `SECRET_PROVIDER=none` works correctly
- Health check endpoint returns provider status
- Unit tests achieve >80% coverage

### Phase 2: Observability Infrastructure (3-4 hours)

**Tasks:**
1. Implement `SecretRefreshMetrics` (Prometheus)
2. Implement `SecretProviderHealthCheck`
3. Implement `AuditingSecretProviderDecorator`
4. Wire up health checks to `/health` endpoint
5. Add metrics endpoint for Prometheus scraping

**Acceptance Criteria:**
- Prometheus metrics exposed at `/metrics`
- Health check reflects secret provider status
- Audit logs include correlation ID, user, timestamp

### Phase 3: Infisical Provider with Refresh (3-4 hours)

**Tasks:**
1. Add `Infisical.Sdk` NuGet package
2. Implement `InfisicalSecretProvider` with Universal Auth
3. Implement `SecretRefreshService` (IHostedService)
4. Add exponential backoff and jitter
5. Add integration tests

**Acceptance Criteria:**
- Connects to Infisical and retrieves secrets
- Refresh runs on schedule with backoff on failure
- Metrics track refresh success/failure
- Graceful handling of Infisical unavailability

### Phase 4: Database Configuration with Key Versioning (4-5 hours)

**Tasks:**
1. Add `AppSetting` entity with versioning and audit fields
2. Add EF Core configuration with constraints
3. Create migration
4. Implement `AesEncryptionService` with key versioning
5. Implement `DbConfigurationProvider` with serialized loads
6. Implement `KeyRotationService` for re-encryption
7. Add unit tests for encryption round-trip and versioning

**Acceptance Criteria:**
- Settings encrypted with key version metadata
- Re-encryption workflow handles all settings
- Concurrency conflicts handled gracefully
- Check constraint prevents high-value secrets in DB

### Phase 5: Additional Providers (2-3 hours each)

**HashiCorp Vault:**
1. Add `VaultSharp` NuGet package
2. Implement `VaultSecretProvider` with AppRole auth
3. Implement token renewal timer
4. Add response-wrapping support for SecretID

**Azure Key Vault:**
1. Add `Azure.Security.KeyVault.Secrets` + `Azure.Identity`
2. Implement `AzureKeyVaultSecretProvider`
3. Use `DefaultAzureCredential` for Managed Identity
4. Handle credential chain fallback

**AWS Secrets Manager:**
1. Add `AWSSDK.SecretsManager`
2. Implement `AwsSecretsManagerProvider`
3. Support IRSA credential chain
4. Implement caching with TTL

### Phase 6: Connection Pool Rotation (2-3 hours)

**Tasks:**
1. Implement `RotationAwareHttpClientFactory`
2. Implement `RotationAwareDbContextFactory`
3. Wire up `IOptionsMonitor` change listeners
4. Add graceful drain with timeout
5. Add integration tests for rotation scenarios

**Acceptance Criteria:**
- HttpClient rotates on credential change
- DB connections use updated credentials
- No connection leaks during rotation
- Graceful handling of in-flight requests

### Phase 7: Integration and Migration (3-4 hours)

**Tasks:**
1. Add `Explore.Secrets` reference to API and Blazor
2. Refactor `ConfigurationExtensions.cs`
3. Update `Program.cs` in each project
4. Remove Infisical from AppHost
5. Delete `entrypoint.sh`
6. Simplify Dockerfile
7. Update documentation

**Acceptance Criteria:**
- API starts independently with any provider
- Blazor starts independently
- Docker image works without Infisical CLI
- Self-hoster documentation complete

---

## Security Checklist

- [ ] Master key never stored in database
- [ ] Master key never logged
- [ ] Key material zeroed after use (`CryptographicOperations.ZeroMemory`)
- [ ] Secrets redacted in audit logs
- [ ] `ValidateOnStart` enabled for required secrets
- [ ] `FailFast=true` in production
- [ ] Health check doesn't leak secret values
- [ ] Metrics don't include secret content
- [ ] DB check constraint prevents high-value secrets
- [ ] Concurrency token prevents lost updates
- [ ] Token renewal before expiry (75% TTL)
- [ ] Exponential backoff on failures
- [ ] Jitter prevents thundering herd

---

## Disaster Recovery

### Master Key Loss

1. **Prevention**: Store master key in multiple secret managers (Infisical + backup in Vault)
2. **Recovery**: Re-generate key, re-encrypt all DB settings, update secret manager
3. **Drill**: Test key recovery quarterly

### Secret Manager Unavailable

1. **Short-term**: Cached secrets continue working (last refresh)
2. **Medium-term**: Fallback to environment variables if `FailFast=false`
3. **Long-term**: Restore secret manager access

### Database Settings Corruption

1. **Prevention**: Regular backups, soft-delete pattern
2. **Recovery**: Restore from backup, re-encrypt with current key
3. **Audit**: Full audit trail identifies who/when/what changed

---

## References

- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
- [Azure Key Vault Best Practices](https://learn.microsoft.com/en-us/azure/security/fundamentals/secrets-best-practices)
- [AWS Secrets Manager Best Practices](https://docs.aws.amazon.com/secretsmanager/latest/userguide/best-practices.html)
- [HashiCorp Vault AppRole Best Practices](https://developer.hashicorp.com/vault/docs/auth/approle/approle-pattern)
- [.NET Configuration Providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers)
- [Infisical .NET SDK](https://infisical.com/docs/sdks/languages/csharp)
- [VaultSharp GitHub](https://github.com/rajanadar/VaultSharp)
