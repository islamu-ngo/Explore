// ABOUTME: Application boundary for resolving immutable webhook retention policy cutoffs.
// ABOUTME: Keeps runtime configuration outside Domain while materialized work receives stable evidence horizons.

namespace Explore.Application.Contracts.Webhooks;

public sealed record WebhookRetentionPolicySnapshot(
    string PolicyVersion,
    DateTimeOffset InboundPayloadRetentionUntil,
    DateTimeOffset OutboundPayloadRetentionUntil,
    DateTimeOffset ProcessingAttemptRetentionUntil,
    DateTimeOffset DeadLetterEvidenceRetentionUntil,
    DateTimeOffset ProviderPublicationRetentionUntil,
    DateTimeOffset OperationalLogRetentionUntil,
    DateTimeOffset AdministrativeAuditRetentionUntil,
    DateTimeOffset ReplayWindowUntil);

public interface IWebhookRetentionPolicyResolver
{
    WebhookRetentionPolicySnapshot Resolve(
        DateTimeOffset sourceOccurredAt,
        DateTimeOffset materializedAt,
        int? outboundPayloadRetentionDays = null);
}
