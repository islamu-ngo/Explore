// ABOUTME: Defines the identity-free registration-order payload returned to a capability holder.
// ABOUTME: Projects safe order state without exposing account or purchaser actor identifiers.

using Explore.Application.Responses;

namespace Explore.Application.DTOs.RegistrationOrders;

public sealed class GuestRegistrationOrderDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public int StatusId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public long OrganizerDirectedTotalMinor { get; init; }
    public long PlatformFeeTotalMinor { get; init; }
    public long OrganizerEarningsTotalMinor { get; init; }
    public long PlatformContributionTotalMinor { get; init; }
    public long TotalDueMinor { get; init; }
    public RegistrationOrderPlatformContributionDto? PlatformContribution { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public IReadOnlyList<RegistrationOrderLineDto> Lines { get; init; } = [];

    public static GuestRegistrationOrderDto From(RegistrationOrderDto order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return new GuestRegistrationOrderDto
        {
            Id = order.Id,
            EventId = order.EventId,
            StatusId = order.StatusId,
            StatusCode = order.StatusCode,
            StatusName = order.StatusName,
            CurrencyCode = order.CurrencyCode,
            OrganizerDirectedTotalMinor = order.OrganizerDirectedTotalMinor,
            PlatformFeeTotalMinor = order.PlatformFeeTotalMinor,
            OrganizerEarningsTotalMinor = order.OrganizerEarningsTotalMinor,
            PlatformContributionTotalMinor = order.PlatformContributionTotalMinor,
            TotalDueMinor = order.TotalDueMinor,
            PlatformContribution = order.PlatformContribution,
            ExpiresAt = order.ExpiresAt,
            SubmittedAt = order.SubmittedAt,
            ConfirmedAt = order.ConfirmedAt,
            RejectedAt = order.RejectedAt,
            CancelledAt = order.CancelledAt,
            Lines = order.Lines
        };
    }
}

public sealed class GuestRegistrationOrderLifecycleResponse : BaseCommandResponse<Guid>
{
    public GuestRegistrationOrderDto? Order { get; init; }

    public static GuestRegistrationOrderLifecycleResponse From(RegistrationOrderLifecycleResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new GuestRegistrationOrderLifecycleResponse
        {
            Id = response.Id,
            Success = response.Success,
            Message = response.Message,
            FailureCode = response.FailureCode,
            Errors = response.Errors,
            Order = response.Order is null ? null : GuestRegistrationOrderDto.From(response.Order)
        };
    }
}
