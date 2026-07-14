// ABOUTME: API DTO for tenant-scoped webhook consumers and provider mappings.
// ABOUTME: Exposes normalized enum ids/names while keeping endpoint secrets out of management payloads.

namespace Explore.Application.DTOs.Webhooks;

public sealed class WebhookConsumerDto
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }

    public Guid? OwnerActorId { get; init; }

    public Guid? OwnerUserId { get; init; }

    public int ConsumerKindId { get; init; }

    public required string ConsumerKindCode { get; init; }

    public required string ConsumerKindName { get; init; }

    public int StatusId { get; init; }

    public required string StatusCode { get; init; }

    public required string StatusName { get; init; }

    public int ProviderModeId { get; init; }

    public required string ProviderModeCode { get; init; }

    public required string ProviderModeName { get; init; }

    public bool ProviderCapabilityAuthorityAvailable { get; init; }

    public required string CapabilityResolutionVersion { get; init; }

    public string? CapabilityUnavailableReasonCode { get; init; }

    public required IReadOnlyList<WebhookProviderCapabilityDto> ProviderCapabilities { get; init; }

    public required string Name { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public sealed class WebhookProviderCapabilityDto
{
    public int CapabilityId { get; init; }

    public required string CapabilityCode { get; init; }

    public required string CapabilityName { get; init; }

    public bool IsAvailable { get; init; }

    public required IReadOnlyList<string> AvailableFromProviderCodes { get; init; }

    public string? UnavailableReasonCode { get; init; }
}
