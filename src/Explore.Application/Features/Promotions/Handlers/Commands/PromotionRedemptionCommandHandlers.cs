// ABOUTME: Handles promotion code apply/remove orchestration for registration orders at Application boundaries.
// ABOUTME: Uses entity repositories, manual validators, serializable units, and generic failures to avoid code enumeration.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Features.Promotions.Requests.Commands;
using Explore.Application.Features.Promotions.Validators;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.Promotions.Handlers.Commands;

public sealed class ApplyPromotionCodeToRegistrationOrderCommandHandler(
    IRegistrationInventoryRepository inventory,
    IPromotionRedemptionRepository promotions,
    IPlatformFeePolicyRepository feePolicies,
    IPromotionCodeDigestService digests,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<ApplyPromotionCodeToRegistrationOrderCommand, PromotionRedemptionResponseDto>
{
    public async Task<PromotionRedemptionResponseDto> Handle(
        ApplyPromotionCodeToRegistrationOrderCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new ApplyPromotionCodeToRegistrationOrderCommandValidator();
        FluentValidation.Results.ValidationResult validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Invalid(request.OrderId);
        }

        Guid reservationId = Guid.CreateVersion7();
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            RegistrationOrder? order = await inventory.GetOrderForUpdateWithPiiAsync(request.OrderId, tenant.TenantId, token);
            if (order is null)
            {
                return Unavailable(request.OrderId);
            }

            IReadOnlyList<int> keyVersions = await promotions.GetDistinctLookupKeyVersionsAsync(
                tenant.TenantId,
                order.EventId,
                order.TicketCatalogVersionId,
                token);
            if (keyVersions.Count == 0)
            {
                return Unavailable(order.Id);
            }

            IReadOnlyCollection<PromotionCodeDigest> candidates = await digests.ComputeCandidatesAsync(
                tenant.TenantId,
                order.EventId,
                request.Code,
                keyVersions,
                token);
            PromotionCodeMatch? match = await promotions.GetCodeForUpdateAsync(
                tenant.TenantId,
                order.EventId,
                order.TicketCatalogVersionId,
                candidates,
                token);
            if (match is null || await promotions.GetActiveReservationForUpdateAsync(tenant.TenantId, order.Id, token) is not null)
            {
                return Unavailable(order.Id);
            }

            int totalRedemptions = await promotions.GetTotalActiveOrConsumedCountAsync(tenant.TenantId, match.Definition.Id, token);
            VerifiedPurchaserIdentity? purchaser = order.GetVerifiedPurchaserIdentity();
            int purchaserRedemptions = purchaser is null
                ? 0
                : await promotions.GetVerifiedPurchaserActiveOrConsumedCountAsync(tenant.TenantId, match.Definition.Id, purchaser, token);
            (bool feePolicyAvailable, PlatformFeePolicy? feePolicy) = await PromotionFeePolicyLoader.LoadAsync(order, feePolicies, token);
            if (!feePolicyAvailable)
            {
                return Unavailable(order.Id);
            }

            try
            {
                PromotionReservation reservation = PromotionReservation.Reserve(reservationId, order, match.Definition, match.Code, now);
                order.ApplyPromotion(reservation, match.Definition, match.Code, now, totalRedemptions, purchaserRedemptions, feePolicy);
                await promotions.AddReservationAsync(reservation, token);
                await inventory.SaveChangesAsync(token);
                return Success(order, "Promotion applied.");
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Unavailable(order.Id);
            }
        }, cancellationToken);
    }

    private static PromotionRedemptionResponseDto Invalid(Guid orderId) => Failure(orderId, PromotionRedemptionFailureCodes.ValidationFailed);

    private static PromotionRedemptionResponseDto Unavailable(Guid orderId) => Failure(orderId, PromotionRedemptionFailureCodes.Unavailable);

    private static PromotionRedemptionResponseDto Failure(Guid orderId, string code) => new()
    {
        Id = orderId,
        Success = false,
        Message = "Promotion cannot be applied to this order.",
        FailureCode = code,
        Errors = [code]
    };

    private static PromotionRedemptionResponseDto Success(RegistrationOrder order, string message) => new()
    {
        Id = order.Id,
        Success = true,
        Message = message,
        AppliedPromotionDisplayLabel = order.AppliedPromotionDisplayLabelSnapshot,
        PromotionDiscountTotalMinor = order.PromotionDiscountTotalMinorSnapshot,
        TotalDueMinor = order.TotalDueMinorSnapshot,
        PlatformFeeTotalMinor = order.PlatformFeeTotalMinorSnapshot,
        PlatformContributionTotalMinor = order.PlatformContributionTotalMinorSnapshot
    };
}

public sealed class RemovePromotionFromRegistrationOrderCommandHandler(
    IRegistrationInventoryRepository inventory,
    IPromotionRedemptionRepository promotions,
    IPlatformFeePolicyRepository feePolicies,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<RemovePromotionFromRegistrationOrderCommand, PromotionRedemptionResponseDto>
{
    public async Task<PromotionRedemptionResponseDto> Handle(
        RemovePromotionFromRegistrationOrderCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new RemovePromotionFromRegistrationOrderCommandValidator();
        FluentValidation.Results.ValidationResult validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Failure(request.OrderId);
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            RegistrationOrder? order = await inventory.GetOrderForUpdateWithLinesAsync(request.OrderId, tenant.TenantId, token);
            if (order is null)
            {
                return Failure(request.OrderId);
            }

            PromotionReservation? reservation = await promotions.GetActiveReservationForUpdateAsync(tenant.TenantId, order.Id, token);
            if (reservation is null)
            {
                return Failure(order.Id);
            }

            (bool feePolicyAvailable, PlatformFeePolicy? feePolicy) = await PromotionFeePolicyLoader.LoadAsync(order, feePolicies, token);
            if (!feePolicyAvailable)
            {
                return Failure(order.Id);
            }

            try
            {
                order.RemovePromotion(reservation, now, feePolicy);
                await inventory.SaveChangesAsync(token);
                return Success(order, "Promotion removed.");
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Failure(order.Id);
            }
        }, cancellationToken);
    }

    private static PromotionRedemptionResponseDto Failure(Guid orderId) => new()
    {
        Id = orderId,
        Success = false,
        Message = "Promotion cannot be changed for this order.",
        FailureCode = PromotionRedemptionFailureCodes.Unavailable,
        Errors = [PromotionRedemptionFailureCodes.Unavailable]
    };

    private static PromotionRedemptionResponseDto Success(RegistrationOrder order, string message) => new()
    {
        Id = order.Id,
        Success = true,
        Message = message,
        AppliedPromotionDisplayLabel = order.AppliedPromotionDisplayLabelSnapshot,
        PromotionDiscountTotalMinor = order.PromotionDiscountTotalMinorSnapshot,
        TotalDueMinor = order.TotalDueMinorSnapshot,
        PlatformFeeTotalMinor = order.PlatformFeeTotalMinorSnapshot,
        PlatformContributionTotalMinor = order.PlatformContributionTotalMinorSnapshot
    };
}

file static class PromotionFeePolicyLoader
{
    public static async Task<(bool Available, PlatformFeePolicy? Policy)> LoadAsync(
        RegistrationOrder order,
        IPlatformFeePolicyRepository feePolicies,
        CancellationToken cancellationToken)
    {
        int[] versions = order.Lines
            .Select(line => line.PlatformFeePolicyVersionSnapshot)
            .Where(version => version.HasValue)
            .Select(version => version!.Value)
            .Distinct()
            .ToArray();
        if (versions.Length == 0)
        {
            return (true, null);
        }

        if (versions.Length > 1)
        {
            return (false, null);
        }

        PlatformFeePolicy? policy = await feePolicies.GetVersionAsync(versions[0], cancellationToken);
        return (policy is not null, policy);
    }
}
