// ABOUTME: Shared EF Core shape plus concrete table mappings for normalized webhook lookup entities.
// ABOUTME: Keeps each lookup relationally independent while enforcing identical stable-key and metadata constraints.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public abstract class WebhookLookupConfiguration<TLookup>(string tableName, string indexName)
    : IEntityTypeConfiguration<TLookup>
    where TLookup : class
{
    public void Configure(EntityTypeBuilder<TLookup> builder)
    {
        builder.ToTable(tableName);
        builder.Property<int>("Id").ValueGeneratedNever();
        builder.Property<string>("MasterCode").HasMaxLength(100).IsRequired();
        builder.Property<string>("FullName").HasMaxLength(200).IsRequired();
        builder.Property<string?>("Description").HasMaxLength(500);
        builder.HasIndex("MasterCode").HasDatabaseName(indexName).IsUnique();
    }
}

public sealed class WebhookConsumerKindLookupConfiguration()
    : WebhookLookupConfiguration<WebhookConsumerKindLookup>(
        "webhook_consumer_kinds",
        "ux_webhook_consumer_kinds_master_code");

public sealed class WebhookConsumerStatusLookupConfiguration()
    : WebhookLookupConfiguration<WebhookConsumerStatusLookup>(
        "webhook_consumer_statuses",
        "ux_webhook_consumer_statuses_master_code");

public sealed class WebhookProviderModeLookupConfiguration()
    : WebhookLookupConfiguration<WebhookProviderModeLookup>(
        "webhook_provider_modes",
        "ux_webhook_provider_modes_master_code");

public sealed class WebhookProviderKindLookupConfiguration()
    : WebhookLookupConfiguration<WebhookProviderKindLookup>(
        "webhook_provider_kinds",
        "ux_webhook_provider_kinds_master_code");

public sealed class WebhookProviderCapabilityLookupConfiguration()
    : WebhookLookupConfiguration<WebhookProviderCapabilityLookup>(
        "webhook_provider_capabilities",
        "ux_webhook_provider_capabilities_master_code");

public sealed class WebhookEndpointStatusLookupConfiguration()
    : WebhookLookupConfiguration<WebhookEndpointStatusLookup>(
        "webhook_endpoint_statuses",
        "ux_webhook_endpoint_statuses_master_code");

public sealed class WebhookLocalDeliveryStatusLookupConfiguration()
    : WebhookLookupConfiguration<WebhookLocalDeliveryStatusLookup>(
        "webhook_local_delivery_statuses",
        "ux_webhook_local_delivery_statuses_master_code");

public sealed class WebhookDeliveryAttemptOutcomeLookupConfiguration()
    : WebhookLookupConfiguration<WebhookDeliveryAttemptOutcomeLookup>(
        "webhook_delivery_attempt_outcomes",
        "ux_webhook_delivery_attempt_outcomes_master_code");

public sealed class IncomingWebhookMessageStatusLookupConfiguration()
    : WebhookLookupConfiguration<IncomingWebhookMessageStatusLookup>(
        "incoming_webhook_message_statuses",
        "ux_incoming_webhook_message_statuses_master_code");

public sealed class IncomingWebhookProcessingAttemptOutcomeLookupConfiguration()
    : WebhookLookupConfiguration<IncomingWebhookProcessingAttemptOutcomeLookup>(
        "incoming_webhook_processing_attempt_outcomes",
        "ux_incoming_webhook_processing_attempt_outcomes_master_code");

public sealed class IncomingWebhookSettlementSourceLookupConfiguration()
    : WebhookLookupConfiguration<IncomingWebhookSettlementSourceLookup>(
        "incoming_webhook_settlement_sources",
        "ux_incoming_webhook_settlement_sources_master_code");

public sealed class IncomingWebhookRedriveResultLookupConfiguration()
    : WebhookLookupConfiguration<IncomingWebhookRedriveResultLookup>(
        "incoming_webhook_redrive_results",
        "ux_incoming_webhook_redrive_results_master_code");

public sealed class WebhookProviderPublicationStatusLookupConfiguration()
    : WebhookLookupConfiguration<WebhookProviderPublicationStatusLookup>(
        "webhook_provider_publication_statuses",
        "ux_webhook_provider_publication_statuses_master_code");

public sealed class WebhookProviderPublicationAttemptOutcomeLookupConfiguration()
    : WebhookLookupConfiguration<WebhookProviderPublicationAttemptOutcomeLookup>(
        "webhook_provider_publication_attempt_outcomes",
        "ux_webhook_provider_publication_attempt_outcomes_master_code");

public sealed class WebhookPayloadProvenanceLookupConfiguration()
    : WebhookLookupConfiguration<WebhookPayloadProvenanceLookup>(
        "webhook_payload_provenances",
        "ux_webhook_payload_provenances_master_code");
