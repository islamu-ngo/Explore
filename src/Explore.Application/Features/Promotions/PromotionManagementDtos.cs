// ABOUTME: Browser-safe promotion management DTOs for organizer authoring screens.
// ABOUTME: Hides commercial authority metadata and never exposes digests, key versions, or stored secrets.

using System.Text.Json.Serialization;
using Explore.Application.Responses;

namespace Explore.Application.Features.Promotions;

public class PromotionManagementCommandResponseDto : BaseCommandResponse<Guid>
{
    public PromotionManagementDto? Promotion { get; set; }
}

public sealed class PromotionCodeIssuedCommandResponseDto : PromotionManagementCommandResponseDto
{
    public string? IssuedCode { get; set; }
}

public sealed record PromotionManagementDto
{
    public Guid EventId { get; set; }

    public Guid TicketCatalogVersionId { get; set; }

    public int TicketCatalogVersionNumber { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public Guid DefinitionId { get; set; }

    public Guid DefinitionGroupId { get; set; }

    public int VersionNumber { get; set; }

    public int StatusId { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string StatusName { get; set; } = string.Empty;

    public string DisplayLabel { get; set; } = string.Empty;

    public DateTime StartsAtUtc { get; set; }

    public DateTime EndsAtUtc { get; set; }

    public int? TotalRedemptionLimit { get; set; }

    public int? PerVerifiedPurchaserLimit { get; set; }

    public string DiscountKind { get; set; } = string.Empty;

    public long? FixedDiscountMinor { get; set; }

    public int? BasisPointDiscount { get; set; }

    public long? MaximumDiscountMinor { get; set; }

    public bool IncludesAllTickets { get; set; }

    public IReadOnlyList<Guid> EligibleTicketTypeIds { get; set; } = [];

    public string? PromotionCodeDisplayLabel { get; set; }

    [JsonIgnore]
    public Guid TenantId { get; set; }

    [JsonIgnore]
    public Guid? ActorId { get; set; }

    [JsonIgnore]
    public Guid? ActorUserId { get; set; }

    [JsonIgnore]
    public Guid? ActorOrganizationId { get; set; }

    [JsonIgnore]
    public Guid? ActorGroupId { get; set; }

    [JsonIgnore]
    public Guid? OrganizerActorId { get; set; }

    [JsonIgnore]
    public Guid? OrganizerUserId { get; set; }

    [JsonIgnore]
    public Guid? OrganizerOrganizationId { get; set; }

    [JsonIgnore]
    public Guid? OrganizerGroupId { get; set; }
}
