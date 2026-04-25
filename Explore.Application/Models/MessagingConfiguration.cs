// ABOUTME: Messaging configuration POCO resolved from the cascading settings engine.
// ABOUTME: Supports RabbitMQ, InMemory, or None — provider-agnostic configuration.

using Explore.Domain.Enums;

namespace Explore.Application.Models;

public class MessagingConfiguration
{
    public MessagingProviderEnum Provider { get; set; } = MessagingProviderEnum.None;

    public bool IsEnabled { get; set; }

    public string? HostName { get; set; }

    public int Port { get; set; } = 5672;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? VirtualHost { get; set; } = "/";

    public int MaxInboundMessageBodySize { get; set; } = 1024 * 1024 * 4;

    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;

    public int RetryAttempts { get; set; } = 3;

    public bool EnableOpenTelemetry { get; set; } = true;

    public bool EnableCompression { get; set; } = true;
}
