// ABOUTME: Defines custom OpenTelemetry business metrics for the platform.
// ABOUTME: Tracks domain activity plus external API-key, email-dispatch, storage, notification fanout, and governance signals.

using System.Diagnostics.Metrics;
using Explore.Application.Responses;
using Explore.Domain;

namespace Explore.Application.Telemetry;

/// <summary>
/// Custom business metrics exposed via OpenTelemetry.
/// Metrics use metric-specific bounded tags; not every metric includes tenant_id.
/// Meter name: "Explore.Business"
/// </summary>
public sealed class BusinessMetrics : IDisposable
{
    public const string MeterName = "Explore.Business";

    private readonly Meter _meter;
    private readonly Counter<long> _eventsCreated;
    private readonly Counter<long> _eventsPublished;
    private readonly Counter<long> _registrationsCreated;
    private readonly Counter<long> _organizationsCreated;
    private readonly Counter<long> _authorizationDecisions;
    private readonly Counter<long> _eventRoleAssignmentChanged;
    private readonly Counter<long> _externalApiKeysCreated;
    private readonly Counter<long> _externalApiKeysRevoked;
    private readonly Counter<long> _externalApiKeyAuthenticationAttempts;
    private readonly Counter<long> _externalApiKeyThrottleEvents;
    private readonly Counter<long> _externalApiKeyPolicyUpdated;
    private readonly Counter<long> _externalApiKeyRotated;
    private readonly Counter<long> _emailDispatchAttempts;
    private readonly Counter<long> _emailDispatchRabbitMqPublishes;
    private readonly Counter<long> _emailDispatchRabbitMqConsumes;
    private readonly Counter<long> _notificationFanoutRuns;
    private readonly Counter<long> _notificationFanoutSubscribers;
    private readonly Counter<long> _customPropertyPurgeDecisions;
    private readonly Counter<long> _idempotencyCleanupRuns;
    private readonly Counter<long> _idempotencyCleanupRows;
    private readonly Counter<long> _aiRetentionCleanupRuns;
    private readonly Counter<long> _aiRetentionCleanupRows;
    private readonly Counter<long> _aiProviderHealthChecks;
    private readonly Counter<long> _aiProviderRequests;
    private readonly Histogram<double> _aiProviderRequestDuration;
    private readonly Histogram<long> _aiProviderTokenUsage;
    private readonly Counter<long> _aiProviderProposedActions;
    private readonly Counter<long> _storageUploadSessions;
    private readonly Histogram<long> _storageUploadBytes;
    private readonly Counter<long> _storageReads;
    private readonly Histogram<long> _storageReadBytes;
    private readonly Counter<long> _storageDeletes;
    private readonly Counter<long> _storageQuotaReservations;
    private readonly Histogram<long> _storageQuotaBytes;
    private readonly Counter<long> _storageProviderTests;
    private readonly Counter<long> _storageReconciliationRuns;
    private readonly Counter<long> _storageReconciliationObjects;

    public BusinessMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        var meter = _meter;

        _eventsCreated = meter.CreateCounter<long>(
            "explore.events.created",
            unit: "{event}",
            description: "Total number of events created");

        _eventsPublished = meter.CreateCounter<long>(
            "explore.events.published",
            unit: "{event}",
            description: "Total number of events published");

        _registrationsCreated = meter.CreateCounter<long>(
            "explore.registrations.created",
            unit: "{registration}",
            description: "Total number of event registrations created");

        _organizationsCreated = meter.CreateCounter<long>(
            "explore.organizations.created",
            unit: "{organization}",
            description: "Total number of organizations created");

        _authorizationDecisions = meter.CreateCounter<long>(
            "explore.authorization.decisions",
            unit: "{decision}",
            description: "Total authorization decisions (allowed/denied)");

        _eventRoleAssignmentChanged = meter.CreateCounter<long>(
            "event_role_assignment.changed",
            unit: "{change}",
            description: "Total event-role assignment lifecycle changes by operation and outcome");

        _externalApiKeysCreated = meter.CreateCounter<long>(
            "explore.external_api_keys.created",
            unit: "{key}",
            description: "Total number of external API keys created");

        _externalApiKeysRevoked = meter.CreateCounter<long>(
            "explore.external_api_keys.revoked",
            unit: "{key}",
            description: "Total number of external API keys revoked");

        _externalApiKeyAuthenticationAttempts = meter.CreateCounter<long>(
            "explore.external_api_keys.authentication_attempts",
            unit: "{attempt}",
            description: "Total external API key authentication attempts by outcome");

        _externalApiKeyThrottleEvents = meter.CreateCounter<long>(
            "explore.external_api_keys.throttled",
            unit: "{throttle}",
            description: "Total external API key throttle events by policy");

        _externalApiKeyPolicyUpdated = meter.CreateCounter<long>(
            "explore.external_api_keys.policy_updated",
            unit: "{update}",
            description: "Total external API key policy updates");

        _externalApiKeyRotated = meter.CreateCounter<long>(
            "explore.external_api_keys.rotated",
            unit: "{rotation}",
            description: "Total external API key rotations");

        _emailDispatchAttempts = meter.CreateCounter<long>(
            "explore.email_dispatch.attempts",
            unit: "{attempt}",
            description: "Total Basic Dispatch Mode email dispatch attempts by outcome");

        _emailDispatchRabbitMqPublishes = meter.CreateCounter<long>(
            "explore.email_dispatch.rabbitmq.publishes",
            unit: "{publish}",
            description: "Total RabbitMQ Dispatch Mode pointer publishes by outcome");

        _emailDispatchRabbitMqConsumes = meter.CreateCounter<long>(
            "explore.email_dispatch.rabbitmq.consumes",
            unit: "{delivery}",
            description: "Total RabbitMQ Dispatch Mode pointer deliveries by durable consumer outcome");

        _notificationFanoutRuns = meter.CreateCounter<long>(
            "explore.notifications.fanout_runs",
            unit: "{run}",
            description: "Total notification fanout runs by kind and bounded outcome");

        _notificationFanoutSubscribers = meter.CreateCounter<long>(
            "explore.notifications.fanout_subscribers",
            unit: "{subscriber}",
            description: "Total notification fanout subscriber decisions by kind and bounded outcome");

        _customPropertyPurgeDecisions = meter.CreateCounter<long>(
            "explore.custom_properties.purge_decisions",
            unit: "{decision}",
            description: "Total custom-property hard-purge decisions by scope, outcome, and bounded blocker category");

        _idempotencyCleanupRuns = meter.CreateCounter<long>(
            "explore.idempotency.cleanup_runs",
            unit: "{run}",
            description: "Total idempotency cleanup passes by mode and outcome");

        _idempotencyCleanupRows = meter.CreateCounter<long>(
            "explore.idempotency.cleanup_rows",
            unit: "{row}",
            description: "Total idempotency rows selected or deleted by cleanup mode and outcome");

        _aiRetentionCleanupRuns = meter.CreateCounter<long>(
            "explore.ai.retention.cleanup_runs",
            unit: "{run}",
            description: "Total AI retention cleanup passes by mode and bounded outcome");

        _aiRetentionCleanupRows = meter.CreateCounter<long>(
            "explore.ai.retention.cleanup_rows",
            unit: "{row}",
            description: "Total AI retention rows selected or redacted by cleanup mode and bounded category");

        _aiProviderHealthChecks = meter.CreateCounter<long>(
            "explore.ai.provider.health_checks",
            unit: "{check}",
            description: "Total AI provider health checks by provider, status, and bounded reason");

        _aiProviderRequests = meter.CreateCounter<long>(
            "explore.ai.provider.requests",
            unit: "{request}",
            description: "Total AI provider requests by provider, outcome, and bounded failure category");

        _aiProviderRequestDuration = meter.CreateHistogram<double>(
            "explore.ai.provider.request_duration",
            unit: "s",
            description: "AI provider request duration by provider, outcome, and bounded failure category");

        _aiProviderTokenUsage = meter.CreateHistogram<long>(
            "explore.ai.provider.token_usage",
            unit: "{token}",
            description: "AI provider token usage by provider and bounded token type");

        _aiProviderProposedActions = meter.CreateCounter<long>(
            "explore.ai.provider.proposed_actions",
            unit: "{action}",
            description: "Total AI proposed actions returned by provider and bounded action kind");

        _storageUploadSessions = meter.CreateCounter<long>(
            "explore.storage.upload_sessions",
            unit: "{session}",
            description: "Total storage upload session lifecycle outcomes by provider and bounded category");

        _storageUploadBytes = meter.CreateHistogram<long>(
            "explore.storage.upload_bytes",
            unit: "By",
            description: "Storage upload byte counts by provider and bounded outcome");

        _storageReads = meter.CreateCounter<long>(
            "explore.storage.reads",
            unit: "{read}",
            description: "Total storage read outcomes by provider, visibility, and bounded failure category");

        _storageReadBytes = meter.CreateHistogram<long>(
            "explore.storage.read_bytes",
            unit: "By",
            description: "Storage read byte counts by provider, visibility, and bounded outcome");

        _storageDeletes = meter.CreateCounter<long>(
            "explore.storage.deletes",
            unit: "{delete}",
            description: "Total storage delete outcomes by provider and bounded failure category");

        _storageQuotaReservations = meter.CreateCounter<long>(
            "explore.storage.quota_reservations",
            unit: "{reservation}",
            description: "Total storage quota reservation outcomes by provider and bounded operation");

        _storageQuotaBytes = meter.CreateHistogram<long>(
            "explore.storage.quota_bytes",
            unit: "By",
            description: "Storage quota reservation byte counts by provider, operation, and bounded outcome");

        _storageProviderTests = meter.CreateCounter<long>(
            "explore.storage.provider_tests",
            unit: "{test}",
            description: "Total storage provider connection test outcomes by provider and bounded failure category");

        _storageReconciliationRuns = meter.CreateCounter<long>(
            "explore.storage.reconciliation_runs",
            unit: "{run}",
            description: "Total storage reconciliation passes by mode and bounded outcome");

        _storageReconciliationObjects = meter.CreateCounter<long>(
            "explore.storage.reconciliation_objects",
            unit: "{object}",
            description: "Total storage reconciliation object decisions by provider, category, action, and bounded outcome");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public void RecordEventCreated(string? tenantId = null, string? eventType = null)
    {
        _eventsCreated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("event_type", eventType ?? "unknown"));
    }

    public void RecordEventPublished(string? tenantId = null)
    {
        _eventsPublished.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"));
    }

    public void RecordRegistrationCreated(string? tenantId = null)
    {
        _registrationsCreated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"));
    }

    public void RecordOrganizationCreated(string? tenantId = null)
    {
        _organizationsCreated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"));
    }

    public void RecordAuthorizationDecision(string resource, string action, bool allowed)
    {
        _authorizationDecisions.Add(1,
            new KeyValuePair<string, object?>("resource", resource),
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("result", allowed ? "allowed" : "denied"));
    }

    public void RecordEventRoleAssignmentChanged(string operation, string outcome, string? roleCode = null)
    {
        _eventRoleAssignmentChanged.Add(1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("role", roleCode ?? "unknown"));
    }

    public void RecordExternalApiKeyCreated(string? tenantId = null, string? ownerType = null)
    {
        _externalApiKeysCreated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("owner_type", ownerType ?? "unknown"));
    }

    public void RecordExternalApiKeyRevoked(string? tenantId = null, string? ownerType = null)
    {
        _externalApiKeysRevoked.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("owner_type", ownerType ?? "unknown"));
    }

    public void RecordExternalApiKeyAuthentication(string outcome, string? tenantId = null, string? ownerType = null)
    {
        _externalApiKeyAuthenticationAttempts.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("owner_type", ownerType ?? "unknown"),
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    public void RecordExternalApiKeyThrottle(string policy, string? tenantId = null, string? ownerType = null)
    {
        _externalApiKeyThrottleEvents.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("owner_type", ownerType ?? "unknown"),
            new KeyValuePair<string, object?>("policy", policy));
    }

    public void RecordExternalApiKeyPolicyUpdated(string? tenantId = null, string? ownerType = null)
    {
        _externalApiKeyPolicyUpdated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("owner_type", ownerType ?? "unknown"));
    }

    public void RecordExternalApiKeyRotated(string? tenantId = null, string? ownerType = null)
    {
        _externalApiKeyRotated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("owner_type", ownerType ?? "unknown"));
    }

    public void RecordEmailDispatchAttempt(string? tenantId = null, string? outcome = null, string? failureCategory = null)
    {
        _emailDispatchAttempts.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("outcome", outcome ?? "unknown"),
            new KeyValuePair<string, object?>("failure_category", failureCategory ?? "none"));
    }

    public void RecordEmailDispatchRabbitMqPublish(string? tenantId = null, string? outcome = null, string? failureCategory = null)
    {
        _emailDispatchRabbitMqPublishes.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("outcome", outcome ?? "unknown"),
            new KeyValuePair<string, object?>("failure_category", failureCategory ?? "none"));
    }

    public void RecordEmailDispatchRabbitMqConsume(string? tenantId = null, string? outcome = null, string? failureCategory = null)
    {
        _emailDispatchRabbitMqConsumes.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("outcome", outcome ?? "unknown"),
            new KeyValuePair<string, object?>("failure_category", failureCategory ?? "none"));
    }

    public void RecordNotificationFanoutRun(string? tenantId, string? fanoutKind, string? outcome)
    {
        _notificationFanoutRuns.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("fanout_kind", NormalizeTag(fanoutKind)),
            new KeyValuePair<string, object?>("outcome", NormalizeTag(outcome)));
    }

    public void RecordNotificationFanoutSubscribers(long subscriberCount, string? tenantId, string? fanoutKind, string? outcome)
    {
        if (subscriberCount <= 0)
        {
            return;
        }

        _notificationFanoutSubscribers.Add(subscriberCount,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("fanout_kind", NormalizeTag(fanoutKind)),
            new KeyValuePair<string, object?>("outcome", NormalizeTag(outcome)));
    }

    public void RecordCustomPropertyPurgeDecision(string? tenantId, string scope, string outcome, string blockerCategory)
    {
        _customPropertyPurgeDecisions.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("scope", NormalizeTag(scope)),
            new KeyValuePair<string, object?>("outcome", NormalizeTag(outcome)),
            new KeyValuePair<string, object?>("blocker_category", NormalizeTag(blockerCategory)));
    }

    public void RecordIdempotencyCleanupRun(string mode, string outcome)
    {
        _idempotencyCleanupRuns.Add(1,
            new KeyValuePair<string, object?>("mode", NormalizeTag(mode)),
            new KeyValuePair<string, object?>("outcome", NormalizeTag(outcome)));
    }

    public void RecordIdempotencyCleanupRows(long rowCount, string mode, string outcome)
    {
        _idempotencyCleanupRows.Add(rowCount,
            new KeyValuePair<string, object?>("mode", NormalizeTag(mode)),
            new KeyValuePair<string, object?>("outcome", NormalizeTag(outcome)));
    }

    public void RecordAiRetentionCleanupRun(string mode, string outcome)
    {
        _aiRetentionCleanupRuns.Add(1,
            new KeyValuePair<string, object?>("mode", NormalizeTag(mode)),
            new KeyValuePair<string, object?>("outcome", NormalizeTag(outcome)));
    }

    public void RecordAiRetentionCleanupRows(long rowCount, string mode, string category)
    {
        if (rowCount <= 0)
        {
            return;
        }

        _aiRetentionCleanupRows.Add(rowCount,
            new KeyValuePair<string, object?>("mode", NormalizeTag(mode)),
            new KeyValuePair<string, object?>("category", NormalizeAiRetentionCleanupCategory(category)));
    }

    public void RecordAiProviderHealthCheck(string? provider, string? status, string? reason)
    {
        _aiProviderHealthChecks.Add(1,
            new KeyValuePair<string, object?>("provider", NormalizeAiProvider(provider)),
            new KeyValuePair<string, object?>("status", NormalizeTag(status)),
            new KeyValuePair<string, object?>("reason", NormalizeTag(reason)));
    }

    public void RecordAiProviderRequest(string? provider, string? outcome, string? failureCategory = null)
    {
        _aiProviderRequests.Add(1,
            new KeyValuePair<string, object?>("provider", NormalizeAiProvider(provider)),
            new KeyValuePair<string, object?>("outcome", NormalizeAiProviderOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeAiProviderFailureCategory(failureCategory)));
    }

    public void RecordAiProviderRequestDuration(
        TimeSpan duration,
        string? provider,
        string? outcome,
        string? failureCategory = null)
    {
        if (duration < TimeSpan.Zero)
        {
            return;
        }

        _aiProviderRequestDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("provider", NormalizeAiProvider(provider)),
            new KeyValuePair<string, object?>("outcome", NormalizeAiProviderOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeAiProviderFailureCategory(failureCategory)));
    }

    public void RecordAiProviderTokenUsage(
        string? provider,
        int? inputTokens,
        int? outputTokens,
        int? totalTokens)
    {
        RecordAiProviderTokenUsage(provider, "input", inputTokens);
        RecordAiProviderTokenUsage(provider, "output", outputTokens);
        RecordAiProviderTokenUsage(provider, "total", totalTokens);
    }

    public void RecordAiProviderProposedActions(string? provider, int count, string? actionKind)
    {
        if (count <= 0)
        {
            return;
        }

        _aiProviderProposedActions.Add(count,
            new KeyValuePair<string, object?>("provider", NormalizeAiProvider(provider)),
            new KeyValuePair<string, object?>("action_kind", NormalizeAiProposedActionKind(actionKind)));
    }

    public void RecordStorageUploadSession(
        string? provider,
        string? operation,
        string? outcome,
        string? failureCategory = null)
    {
        _storageUploadSessions.Add(1,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("operation", NormalizeStorageOperation(operation)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeStorageFailureCategory(failureCategory)));
    }

    public void RecordStorageUploadBytes(
        long byteCount,
        string? provider,
        string? outcome,
        string? failureCategory = null)
    {
        if (byteCount < 0)
        {
            return;
        }

        _storageUploadBytes.Record(byteCount,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeStorageFailureCategory(failureCategory)));
    }

    public void RecordStorageRead(
        string? provider,
        string? outcome,
        string? failureCategory,
        string? visibility)
    {
        _storageReads.Add(1,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeStorageFailureCategory(failureCategory)),
            new KeyValuePair<string, object?>("visibility", NormalizeStorageVisibility(visibility)));
    }

    public void RecordStorageReadBytes(
        long byteCount,
        string? provider,
        string? outcome,
        string? visibility)
    {
        if (byteCount < 0)
        {
            return;
        }

        _storageReadBytes.Record(byteCount,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("visibility", NormalizeStorageVisibility(visibility)));
    }

    public void RecordStorageDelete(string? provider, string? outcome, string? failureCategory = null)
    {
        _storageDeletes.Add(1,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeStorageFailureCategory(failureCategory)));
    }

    public void RecordStorageQuotaReservation(
        string? provider,
        string? operation,
        string? outcome,
        string? failureCategory = null)
    {
        _storageQuotaReservations.Add(1,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("operation", NormalizeStorageOperation(operation)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeStorageFailureCategory(failureCategory)));
    }

    public void RecordStorageQuotaBytes(
        long byteCount,
        string? provider,
        string? operation,
        string? outcome)
    {
        if (byteCount < 0)
        {
            return;
        }

        _storageQuotaBytes.Record(byteCount,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("operation", NormalizeStorageOperation(operation)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)));
    }

    public void RecordStorageProviderTest(string? provider, string? outcome, string? failureCategory = null)
    {
        _storageProviderTests.Add(1,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeStorageFailureCategory(failureCategory)));
    }

    public void RecordStorageReconciliationRun(string? mode, string? outcome, string? failureCategory = null)
    {
        _storageReconciliationRuns.Add(1,
            new KeyValuePair<string, object?>("mode", NormalizeStorageReconciliationMode(mode)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeStorageFailureCategory(failureCategory)));
    }

    public void RecordStorageReconciliationObjects(
        long count,
        string? provider,
        string? category,
        string? action,
        string? outcome,
        string? failureCategory = null)
    {
        if (count <= 0)
        {
            return;
        }

        _storageReconciliationObjects.Add(count,
            new KeyValuePair<string, object?>("provider", NormalizeStorageProvider(provider)),
            new KeyValuePair<string, object?>("category", NormalizeStorageReconciliationCategory(category)),
            new KeyValuePair<string, object?>("action", NormalizeStorageReconciliationAction(action)),
            new KeyValuePair<string, object?>("outcome", NormalizeStorageOutcome(outcome)),
            new KeyValuePair<string, object?>("failure_category", NormalizeStorageFailureCategory(failureCategory)));
    }

    private static string NormalizeTag(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeStorageProvider(string? provider)
    {
        return NormalizeTag(provider) switch
        {
            StorageProviders.Local => StorageProviders.Local,
            StorageProviders.S3Compatible => StorageProviders.S3Compatible,
            StorageProviders.LegacyExternal => StorageProviders.LegacyExternal,
            _ => "unknown"
        };
    }

    private static string NormalizeAiRetentionCleanupCategory(string? category)
    {
        return NormalizeTag(category) switch
        {
            "eligible_conversations" => "eligible_conversations",
            "redacted_conversations" => "redacted_conversations",
            "redacted_messages" => "redacted_messages",
            "redacted_runs" => "redacted_runs",
            "redacted_references" => "redacted_references",
            "redacted_proposed_actions" => "redacted_proposed_actions",
            "redacted_tool_executions" => "redacted_tool_executions",
            _ => "unknown"
        };
    }

    private static string NormalizeAiProvider(string? provider)
    {
        return NormalizeTag(provider) switch
        {
            "none" => "none",
            "fake" => "fake",
            "openai-compatible" => "openai-compatible",
            "openai-sdk" => "openai-sdk",
            "azure-openai" => "azure-openai",
            "microsoft-extensions-ai" => "microsoft-extensions-ai",
            _ => "unknown"
        };
    }

    private static string NormalizeAiProviderOutcome(string? outcome)
    {
        return NormalizeTag(outcome) switch
        {
            "succeeded" => "succeeded",
            "failed" => "failed",
            "cancelled" => "cancelled",
            _ => "unknown"
        };
    }

    private static string NormalizeAiProviderFailureCategory(string? failureCategory)
    {
        var normalized = NormalizeTag(failureCategory ?? "none");
        if (normalized == "none")
        {
            return normalized;
        }

        if (normalized.StartsWith("http_", StringComparison.Ordinal)
            && int.TryParse(normalized.AsSpan(5), out var statusCode)
            && statusCode is >= 100 and <= 599)
        {
            return normalized;
        }

        return normalized switch
        {
            "provider_disabled" => "provider_disabled",
            "provider_not_configured" => "provider_not_configured",
            "invalid_settings" => "invalid_settings",
            "streaming_not_supported" => "streaming_not_supported",
            "empty_messages" => "empty_messages",
            "unsupported_message_role" => "unsupported_message_role",
            "invalid_action_schema" => "invalid_action_schema",
            "provider_timeout" => "provider_timeout",
            "provider_unreachable" => "provider_unreachable",
            "invalid_response" => "invalid_response",
            "provider_failure" => "provider_failure",
            "content_filtered" => "content_filtered",
            "invalid_tool_arguments" => "invalid_tool_arguments",
            _ => "unknown"
        };
    }

    private void RecordAiProviderTokenUsage(string? provider, string tokenType, int? tokenCount)
    {
        if (tokenCount is null or <= 0)
        {
            return;
        }

        _aiProviderTokenUsage.Record(tokenCount.Value,
            new KeyValuePair<string, object?>("provider", NormalizeAiProvider(provider)),
            new KeyValuePair<string, object?>("token_type", NormalizeAiProviderTokenType(tokenType)));
    }

    private static string NormalizeAiProviderTokenType(string? tokenType)
    {
        return NormalizeTag(tokenType) switch
        {
            "input" => "input",
            "output" => "output",
            "total" => "total",
            _ => "unknown"
        };
    }

    private static string NormalizeAiProposedActionKind(string? actionKind)
    {
        return NormalizeTag(actionKind) switch
        {
            "create_event_draft" => "create_event_draft",
            _ => "unknown"
        };
    }

    private static string NormalizeStorageOperation(string? operation)
    {
        return NormalizeTag(operation) switch
        {
            "create" => "create",
            "finalize" => "finalize",
            "cancel" => "cancel",
            "read" => "read",
            "delete" => "delete",
            "reserve" => "reserve",
            "release" => "release",
            "commit" => "commit",
            "test" => "test",
            "scan" => "scan",
            "exists" => "exists",
            "quarantine" => "quarantine",
            _ => "unknown"
        };
    }

    private static string NormalizeStorageOutcome(string? outcome)
    {
        return NormalizeTag(outcome) switch
        {
            "succeeded" => "succeeded",
            "failed" => "failed",
            "idempotent" => "idempotent",
            "skipped" => "skipped",
            _ => "unknown"
        };
    }

    private static string NormalizeStorageVisibility(string? visibility)
    {
        return NormalizeTag(visibility) switch
        {
            StorageObjectVisibilities.PublicImage => StorageObjectVisibilities.PublicImage,
            StorageObjectVisibilities.AuthenticatedTenant => StorageObjectVisibilities.AuthenticatedTenant,
            StorageObjectVisibilities.PrivateOwner => StorageObjectVisibilities.PrivateOwner,
            _ => "unknown"
        };
    }

    private static string NormalizeStorageFailureCategory(string? failureCategory)
    {
        if (string.IsNullOrWhiteSpace(failureCategory))
        {
            return "none";
        }

        return NormalizeTag(failureCategory) switch
        {
            "none" => "none",
            "validation_failed" => "validation_failed",
            "metadata_not_found" => "metadata_not_found",
            "object_not_found" => "object_not_found",
            "missing_object_key" => "missing_object_key",
            "access_denied" => "access_denied",
            "provider_resolution_failed" => "provider_resolution_failed",
            "provider_unavailable" => "provider_unavailable",
            "delete_failed" => "delete_failed",
            "local_storage_unavailable" => "local_storage_unavailable",
            "s3_not_configured" => "s3_not_configured",
            "s3_unavailable" => "s3_unavailable",
            "reconciliation_failed" => "reconciliation_failed",
            "inventory_unavailable" => "inventory_unavailable",
            "quarantine_failed" => "quarantine_failed",
            FailureCodes.QuotaExceeded => FailureCodes.QuotaExceeded,
            FailureCodes.StorageUploadTooLarge => FailureCodes.StorageUploadTooLarge,
            FailureCodes.StorageUploadSessionNotFound => FailureCodes.StorageUploadSessionNotFound,
            FailureCodes.StorageUploadSessionFinalized => FailureCodes.StorageUploadSessionFinalized,
            FailureCodes.StorageUploadSessionExpired => FailureCodes.StorageUploadSessionExpired,
            FailureCodes.StorageUploadSessionInvalidState => FailureCodes.StorageUploadSessionInvalidState,
            FailureCodes.StorageUploadSizeMismatch => FailureCodes.StorageUploadSizeMismatch,
            FailureCodes.StorageUploadContentTypeMismatch => FailureCodes.StorageUploadContentTypeMismatch,
            FailureCodes.StorageUploadWriteFailed => FailureCodes.StorageUploadWriteFailed,
            _ => "unknown"
        };
    }

    private static string NormalizeStorageReconciliationMode(string? mode)
    {
        return NormalizeTag(mode) switch
        {
            "dry_run" => "dry_run",
            "report" => "report",
            "quarantine" => "quarantine",
            "delete" => "delete",
            "mixed" => "mixed",
            _ => "unknown"
        };
    }

    private static string NormalizeStorageReconciliationCategory(string? category)
    {
        return NormalizeTag(category) switch
        {
            "metadata" => "metadata",
            "backing_object" => "backing_object",
            _ => "unknown"
        };
    }

    private static string NormalizeStorageReconciliationAction(string? action)
    {
        return NormalizeTag(action) switch
        {
            "scan" => "scan",
            "missing" => "missing",
            "orphan" => "orphan",
            "quarantine" => "quarantine",
            "delete" => "delete",
            "skip" => "skip",
            _ => "unknown"
        };
    }
}
