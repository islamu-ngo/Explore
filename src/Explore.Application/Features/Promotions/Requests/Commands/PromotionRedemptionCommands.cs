// ABOUTME: Defines promotion redemption command inputs and safe response DTOs for registration orders.
// ABOUTME: Keeps plaintext codes write-only and returns only bounded public pricing state.

using Explore.Application.Responses;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.Promotions.Requests.Commands;

public sealed record ApplyPromotionCodeToRegistrationOrderCommand(Guid OrderId, string Code)
    : IRequest<PromotionRedemptionResponseDto>;

public sealed record RemovePromotionFromRegistrationOrderCommand(Guid OrderId)
    : IRequest<PromotionRedemptionResponseDto>;

public sealed record ApplyGuestPromotionCodeToRegistrationOrderCommand(
    Guid EventId,
    Guid OrderId,
    string? CapabilityToken,
    string Code)
    : IRequest<PromotionRedemptionResponseDto>, IGuestRegistrationOrderAccessCommand;

public sealed record RemoveGuestPromotionFromRegistrationOrderCommand(
    Guid EventId,
    Guid OrderId,
    string? CapabilityToken)
    : IRequest<PromotionRedemptionResponseDto>, IGuestRegistrationOrderAccessCommand;

public sealed record ApplyAuthenticatedPromotionCodeToRegistrationOrderCommand(
    Guid EventId,
    Guid OrderId,
    string Code)
    : IRequest<PromotionRedemptionResponseDto>, IAuthenticatedRegistrationOrderAccessCommand;

public sealed record RemoveAuthenticatedPromotionFromRegistrationOrderCommand(Guid EventId, Guid OrderId)
    : IRequest<PromotionRedemptionResponseDto>, IAuthenticatedRegistrationOrderAccessCommand;

public sealed class PromotionRedemptionResponseDto : BaseCommandResponse<Guid>
{
    public string? AppliedPromotionDisplayLabel { get; init; }

    public long PromotionDiscountTotalMinor { get; init; }

    public long TotalDueMinor { get; init; }

    public long PlatformFeeTotalMinor { get; init; }

    public long PlatformContributionTotalMinor { get; init; }
}

public static class PromotionRedemptionFailureCodes
{
    public const string Unavailable = "promotion_unavailable";
    public const string ValidationFailed = "promotion_request_invalid";
}
