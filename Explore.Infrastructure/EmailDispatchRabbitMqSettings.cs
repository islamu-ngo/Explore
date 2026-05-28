// ABOUTME: Configuration for optional RabbitMQ Dispatch Mode for EmailDispatch pointer messages.
// ABOUTME: Defaults keep Basic PostgreSQL plus SMTP dispatch independent when RabbitMQ is not enabled.

namespace Explore.Infrastructure;

public sealed class EmailDispatchRabbitMqSettings
{
    public const string SectionName = "EmailDispatchRabbitMq";

    public bool Enabled { get; set; }

    public string ConnectionStringName { get; set; } = "messaging";

    public string? ConnectionString { get; set; }

    public string ExchangeName { get; set; } = "explore.email-dispatch";

    public string DispatchQueueName { get; set; } = "explore.email-dispatch.dispatch";

    public string DispatchRoutingKey { get; set; } = "email-dispatch.dispatch";

    public string DeadLetterExchangeName { get; set; } = "explore.email-dispatch.dlx";

    public string DeadLetterQueueName { get; set; } = "explore.email-dispatch.dead-letter";

    public string DeadLetterRoutingKey { get; set; } = "email-dispatch.dead-letter";

    public string ParkingQueueName { get; set; } = "explore.email-dispatch.parking";

    public string ParkingRoutingKey { get; set; } = "email-dispatch.parking";

    public string ClientProvidedName { get; set; } = "explore-email-dispatch-rabbitmq";

    public int PublishTimeoutSeconds { get; set; } = 15;
}
