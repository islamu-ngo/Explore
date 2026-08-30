// ABOUTME: Decorator that wraps ISecretProvider to provide audit logging.
// Logs secret access with correlation ID, user context, and redacted key names.
// Integrates with Serilog structured logging via ILogger for OpenTelemetry export.

using System.Diagnostics;
using Explore.Secrets.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Explore.Secrets.Providers;

/// <summary>
/// Decorator that adds audit logging to any <see cref="ISecretProvider"/>.
/// Logs all secret operations with correlation ID and user context.
/// Redacts sensitive key names to prevent credential exposure in logs.
/// </summary>
public sealed class AuditingSecretProviderDecorator : ISecretProvider
{
    private readonly ISecretProvider _inner;
    private readonly ISecretAuditLogger _auditLogger;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<AuditingSecretProviderDecorator> _logger;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Keys containing these patterns will have their values redacted in logs.
    /// </summary>
    private static readonly string[] SensitiveKeyPatterns =
    [
        "password",
        "secret",
        "key",
        "token",
        "credential",
        "connectionstring",
        "apikey",
        "accesskey",
        "privatekey"
    ];

    public AuditingSecretProviderDecorator(
        ISecretProvider inner,
        ISecretAuditLogger auditLogger,
        ILogger<AuditingSecretProviderDecorator> logger,
        IHttpContextAccessor? httpContextAccessor = null,
        TimeProvider? clock = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor;
        _clock = clock ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public SecretProviderType ProviderType => _inner.ProviderType;

    /// <inheritdoc />
    public bool SupportsRefresh => _inner.SupportsRefresh;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId();
        var userId = GetUserId();

        _logger.LogInformation(
            "Initializing secret provider {ProviderType}. CorrelationId: {CorrelationId}, UserId: {UserId}",
            _inner.ProviderType,
            correlationId,
            userId ?? "system");

        try
        {
            await _inner.InitializeAsync(cancellationToken);

            await _auditLogger.LogAsync(new SecretAuditEntry(
                Operation: SecretOperation.Initialize,
                ProviderType: _inner.ProviderType,
                KeyPattern: null,
                Timestamp: _clock.GetUtcNow(),
                UserId: userId,
                CorrelationId: correlationId,
                Success: true),
                cancellationToken);

            _logger.LogInformation(
                "Secret provider {ProviderType} initialized successfully. CorrelationId: {CorrelationId}",
                _inner.ProviderType,
                correlationId);
        }
        catch (Exception ex)
        {
            await _auditLogger.LogAsync(new SecretAuditEntry(
                Operation: SecretOperation.InitializeFailed,
                ProviderType: _inner.ProviderType,
                KeyPattern: null,
                Timestamp: _clock.GetUtcNow(),
                UserId: userId,
                CorrelationId: correlationId,
                Success: false,
                ErrorMessage: ex.Message),
                cancellationToken);

            _logger.LogError(ex,
                "Secret provider {ProviderType} initialization failed. CorrelationId: {CorrelationId}",
                _inner.ProviderType,
                correlationId);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId();
        var userId = GetUserId();
        var redactedKey = RedactKeyForLogging(key);

        _logger.LogDebug(
            "Retrieving secret {RedactedKey} from {ProviderType}. CorrelationId: {CorrelationId}, UserId: {UserId}",
            redactedKey,
            _inner.ProviderType,
            correlationId,
            userId ?? "system");

        var result = await _inner.GetSecretAsync(key, cancellationToken);

        await _auditLogger.LogAsync(new SecretAuditEntry(
            Operation: SecretOperation.Access,
            ProviderType: _inner.ProviderType,
            KeyPattern: redactedKey,
            Timestamp: _clock.GetUtcNow(),
            UserId: userId,
            CorrelationId: correlationId,
            Success: result is not null),
            cancellationToken);

        if (result is null)
        {
            _logger.LogDebug(
                "Secret {RedactedKey} not found in {ProviderType}. CorrelationId: {CorrelationId}",
                redactedKey,
                _inner.ProviderType,
                correlationId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<SecretValue?> GetSecretWithMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId();
        var userId = GetUserId();
        var redactedKey = RedactKeyForLogging(key);

        _logger.LogDebug(
            "Retrieving secret with metadata {RedactedKey} from {ProviderType}. CorrelationId: {CorrelationId}, UserId: {UserId}",
            redactedKey,
            _inner.ProviderType,
            correlationId,
            userId ?? "system");

        var result = await _inner.GetSecretWithMetadataAsync(key, cancellationToken);

        await _auditLogger.LogAsync(new SecretAuditEntry(
            Operation: SecretOperation.Access,
            ProviderType: _inner.ProviderType,
            KeyPattern: redactedKey,
            Timestamp: _clock.GetUtcNow(),
            UserId: userId,
            CorrelationId: correlationId,
            Success: result is not null),
            cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetSecretsByPathAsync(
        string pathPrefix,
        CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId();
        var userId = GetUserId();

        _logger.LogDebug(
            "Retrieving secrets by path {PathPrefix} from {ProviderType}. CorrelationId: {CorrelationId}, UserId: {UserId}",
            pathPrefix,
            _inner.ProviderType,
            correlationId,
            userId ?? "system");

        var result = await _inner.GetSecretsByPathAsync(pathPrefix, cancellationToken);

        await _auditLogger.LogAsync(new SecretAuditEntry(
            Operation: SecretOperation.Access,
            ProviderType: _inner.ProviderType,
            KeyPattern: $"{pathPrefix}:* ({result.Count} secrets)",
            Timestamp: _clock.GetUtcNow(),
            UserId: userId,
            CorrelationId: correlationId,
            Success: true),
            cancellationToken);

        _logger.LogDebug(
            "Retrieved {Count} secrets from path {PathPrefix}. CorrelationId: {CorrelationId}",
            result.Count,
            pathPrefix,
            correlationId);

        return result;
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId();
        var userId = GetUserId();

        _logger.LogInformation(
            "Refreshing secrets from {ProviderType}. CorrelationId: {CorrelationId}",
            _inner.ProviderType,
            correlationId);

        try
        {
            await _inner.RefreshAsync(cancellationToken);

            await _auditLogger.LogAsync(new SecretAuditEntry(
                Operation: SecretOperation.Refresh,
                ProviderType: _inner.ProviderType,
                KeyPattern: null,
                Timestamp: _clock.GetUtcNow(),
                UserId: userId,
                CorrelationId: correlationId,
                Success: true),
                cancellationToken);

            _logger.LogInformation(
                "Secrets refreshed successfully from {ProviderType}. CorrelationId: {CorrelationId}",
                _inner.ProviderType,
                correlationId);
        }
        catch (Exception ex)
        {
            await _auditLogger.LogAsync(new SecretAuditEntry(
                Operation: SecretOperation.RefreshFailed,
                ProviderType: _inner.ProviderType,
                KeyPattern: null,
                Timestamp: _clock.GetUtcNow(),
                UserId: userId,
                CorrelationId: correlationId,
                Success: false,
                ErrorMessage: ex.Message),
                cancellationToken);

            _logger.LogError(ex,
                "Secret refresh failed for {ProviderType}. CorrelationId: {CorrelationId}",
                _inner.ProviderType,
                correlationId);

            throw;
        }
    }

    /// <inheritdoc />
    public Task<ProviderHealthInfo> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        // Health checks are not audited to avoid noise
        return _inner.GetHealthAsync(cancellationToken);
    }

    /// <summary>
    /// Redacts sensitive parts of a key for safe logging.
    /// Example: "Database:ConnectionString" -> "Database:***"
    /// </summary>
    private static string RedactKeyForLogging(string key)
    {
        if (string.IsNullOrEmpty(key))
            return key;

        // Split by common delimiters
        var parts = key.Split([':', '_', '.']);

        if (parts.Length == 0)
            return key;

        // Check if any part is sensitive
        var lastPart = parts[^1];
        var isSensitive = SensitiveKeyPatterns.Any(pattern =>
            lastPart.Contains(pattern, StringComparison.OrdinalIgnoreCase));

        if (isSensitive)
        {
            // Redact the last part
            return string.Join(":", parts[..^1]) + ":***";
        }

        return key;
    }

    /// <summary>
    /// Gets the correlation ID from the current HTTP context or activity.
    /// </summary>
    private string GetCorrelationId()
    {
        // Try HTTP request headers first
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext is not null)
        {
            // Check common correlation ID headers
            if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader) &&
                !string.IsNullOrEmpty(correlationHeader.ToString()))
            {
                return correlationHeader.ToString();
            }

            if (httpContext.Request.Headers.TryGetValue("X-Request-Id", out var requestIdHeader) &&
                !string.IsNullOrEmpty(requestIdHeader.ToString()))
            {
                return requestIdHeader.ToString();
            }

            // Fall back to trace identifier
            if (!string.IsNullOrEmpty(httpContext.TraceIdentifier))
            {
                return httpContext.TraceIdentifier;
            }
        }

        // Try OpenTelemetry Activity
        var activity = Activity.Current;
        if (activity is not null)
        {
            return activity.TraceId.ToString();
        }

        // Generate a new one if nothing available
        return Guid.CreateVersion7().ToString("N")[..8];
    }

    /// <summary>
    /// Gets the user ID from the current HTTP context claims.
    /// </summary>
    private string? GetUserId()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Try common claim types for user ID (matching QUICK_REFERENCE.md pattern)
        var user = httpContext.User;

        // Priority: sub → nameidentifier → sid
        var userId = user.FindFirst("sub")?.Value
            ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? user.FindFirst("sid")?.Value;

        return userId;
    }
}
