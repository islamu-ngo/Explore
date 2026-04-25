// ABOUTME: Setting definitions for message queue / messaging infrastructure configuration.
// ABOUTME: Covers provider selection, connection parameters, resilience, and observability toggles.

namespace Explore.Domain.Settings.Definitions;

public static class MessagingSettingDefinitions
{
    public static readonly SettingDefinition Provider = new(
        Key: "messaging.provider",
        ValueType: SettingValueType.String,
        DefaultValue: "\"none\"",
        Category: "Messaging",
        Description: "Message queue provider (none, rabbitmq, inmemory)",
        MaxScope: SettingScope.Tenant,
        AllowedValues: ["none", "rabbitmq", "inmemory"]);

    public static readonly SettingDefinition Enabled = new(
        Key: "messaging.enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: "Messaging",
        Description: "Whether the messaging subsystem is active for this tenant",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition HostName = new(
        Key: "messaging.host_name",
        ValueType: SettingValueType.String,
        DefaultValue: "\"localhost\"",
        Category: "Messaging",
        Description: "Message broker hostname",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition Port = new(
        Key: "messaging.port",
        ValueType: SettingValueType.Integer,
        DefaultValue: "5672",
        Category: "Messaging",
        Description: "Message broker port",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition UserName = new(
        Key: "messaging.user_name",
        ValueType: SettingValueType.String,
        DefaultValue: "\"guest\"",
        Category: "Messaging",
        Description: "Message broker authentication username",
        MaxScope: SettingScope.Tenant,
        IsSensitive: true);

    public static readonly SettingDefinition Password = new(
        Key: "messaging.password",
        ValueType: SettingValueType.String,
        DefaultValue: "\"guest\"",
        Category: "Messaging",
        Description: "Message broker authentication password",
        MaxScope: SettingScope.Tenant,
        IsSensitive: true);

    public static readonly SettingDefinition VirtualHost = new(
        Key: "messaging.virtual_host",
        ValueType: SettingValueType.String,
        DefaultValue: "\"/\"",
        Category: "Messaging",
        Description: "Message broker virtual host",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition MaxInboundMessageBodySize = new(
        Key: "messaging.max_inbound_message_body_size",
        ValueType: SettingValueType.Integer,
        DefaultValue: "4194304",
        Category: "Messaging",
        Description: "Maximum inbound message body size in bytes (default 4MB)",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition CircuitBreakerFailureThreshold = new(
        Key: "messaging.circuit_breaker_failure_threshold",
        ValueType: SettingValueType.Integer,
        DefaultValue: "5",
        Category: "Messaging",
        Description: "Number of consecutive failures before the circuit breaker opens",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition CircuitBreakerBreakDurationSeconds = new(
        Key: "messaging.circuit_breaker_break_duration_seconds",
        ValueType: SettingValueType.Integer,
        DefaultValue: "30",
        Category: "Messaging",
        Description: "Duration in seconds the circuit breaker stays open before attempting a half-open probe",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition RetryAttempts = new(
        Key: "messaging.retry_attempts",
        ValueType: SettingValueType.Integer,
        DefaultValue: "3",
        Category: "Messaging",
        Description: "Number of retry attempts for failed message processing",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition EnableOpenTelemetry = new(
        Key: "messaging.enable_open_telemetry",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Messaging",
        Description: "Whether to emit OpenTelemetry traces for messaging operations",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition EnableCompression = new(
        Key: "messaging.enable_compression",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Messaging",
        Description: "Whether to compress message payloads",
        MaxScope: SettingScope.Tenant);

    public static IReadOnlyList<SettingDefinition> All =>
    [
        Provider, Enabled, HostName, Port, UserName, Password, VirtualHost,
        MaxInboundMessageBodySize, CircuitBreakerFailureThreshold,
        CircuitBreakerBreakDurationSeconds, RetryAttempts, EnableOpenTelemetry,
        EnableCompression
    ];
}
