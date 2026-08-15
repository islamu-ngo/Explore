// ABOUTME: Handles organizer promotion management mutation flows in the Application layer.
// ABOUTME: Computes promotion code digests transiently and returns only safe management projections.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Features.Promotions.Requests.Commands;
using Explore.Application.Features.Promotions.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.Promotions.Handlers.Commands;

public sealed class CreatePromotionDraftCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    IPromotionManagementRepository promotions,
    IPromotionCodeDigestService digests,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePromotionDraftCommand, PromotionCodeIssuedCommandResponseDto>
{
    public async Task<PromotionCodeIssuedCommandResponseDto> Handle(CreatePromotionDraftCommand request, CancellationToken cancellationToken)
    {
        await new CreatePromotionDraftCommandValidator().ValidateAndThrowAsync(request, cancellationToken);

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, token);
            if (!PromotionManagementHandlerSupport.IsPlatformManaged(eventTarget, tenant.TenantId))
            {
                return PromotionManagementHandlerSupport.NotFound<PromotionCodeIssuedCommandResponseDto>();
            }

            EventTicketCatalogVersion? catalog = await PromotionManagementHandlerSupport.GetScopedCatalogAsync(catalogs, request.EventId, tenant.TenantId, request.TicketCatalogVersionId, token);
            if (catalog is null)
            {
                return PromotionManagementHandlerSupport.NotFound<PromotionCodeIssuedCommandResponseDto>();
            }

            PromotionDefinition definition;
            try
            {
                PromotionScopeMetadata scope = PromotionScopeMetadata.Create(tenant.TenantId, request.EventId, catalog.Id, catalog.VersionNumber, catalog.CurrencyCode);
                definition = PromotionDefinition.CreateDraft(
                    scope,
                    request.DisplayLabel,
                    PromotionManagementHandlerSupport.CreateEligibility(request.EligibleTicketTypeIds),
                    PromotionManagementHandlerSupport.CreateDiscount(scope.CurrencyCode, request.DiscountKind, request.FixedDiscountMinor, request.BasisPointDiscount, request.MaximumDiscountMinor),
                    request.StartsAtUtc,
                    request.EndsAtUtc,
                    request.TotalRedemptionLimit,
                    request.PerVerifiedPurchaserLimit);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed<PromotionCodeIssuedCommandResponseDto>(exception.Message);
            }
            catch (ArgumentException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed<PromotionCodeIssuedCommandResponseDto>(exception.Message);
            }

            await digests.ComputeActiveAsync(tenant.TenantId, request.EventId, request.Code, token);
            await promotions.AddDefinitionAsync(definition, token);
            await promotions.SaveChangesAsync(token);

            return PromotionManagementHandlerSupport.IssuedSuccess(definition.Id, "Promotion draft created.", PromotionManagementMapper.Map(definition, null, eventTarget!), request.Code);
        }, cancellationToken);
    }
}

public sealed class RevisePromotionCommandHandler(
    IEventRepository events,
    IPromotionManagementRepository promotions,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : IRequestHandler<RevisePromotionCommand, PromotionManagementCommandResponseDto>
{
    public async Task<PromotionManagementCommandResponseDto> Handle(RevisePromotionCommand request, CancellationToken cancellationToken)
    {
        await new RevisePromotionCommandValidator().ValidateAndThrowAsync(request, cancellationToken);

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, token);
            if (!PromotionManagementHandlerSupport.IsPlatformManaged(eventTarget, tenant.TenantId))
            {
                return PromotionManagementHandlerSupport.NotFound();
            }

            PromotionDefinition? current = await promotions.GetDefinitionForUpdateAsync(tenant.TenantId, request.EventId, request.PromotionDefinitionId, token);
            if (current is null)
            {
                return PromotionManagementHandlerSupport.NotFound();
            }

            PromotionDefinition revision;
            try
            {
                revision = current.CreateRevision(
                    request.DisplayLabel,
                    PromotionManagementHandlerSupport.CreateEligibility(request.EligibleTicketTypeIds),
                    PromotionManagementHandlerSupport.CreateDiscount(current.ScopeMetadata.CurrencyCode, request.DiscountKind, request.FixedDiscountMinor, request.BasisPointDiscount, request.MaximumDiscountMinor),
                    request.StartsAtUtc,
                    request.EndsAtUtc,
                    request.TotalRedemptionLimit,
                    request.PerVerifiedPurchaserLimit);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed(exception.Message);
            }
            catch (ArgumentException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed(exception.Message);
            }

            await promotions.AddDefinitionAsync(revision, token);
            await promotions.SaveChangesAsync(token);

            return PromotionManagementHandlerSupport.Success(revision.Id, "Promotion revision created.", PromotionManagementMapper.Map(revision, null, eventTarget!));
        }, cancellationToken);
    }
}

public sealed class PublishPromotionCommandHandler(
    IEventRepository events,
    IPromotionManagementRepository promotions,
    IPromotionCodeDigestService digests,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<PublishPromotionCommand, PromotionManagementCommandResponseDto>
{
    public async Task<PromotionManagementCommandResponseDto> Handle(PublishPromotionCommand request, CancellationToken cancellationToken)
    {
        await new PublishPromotionCommandValidator().ValidateAndThrowAsync(request, cancellationToken);
        DateTime publishedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, token);
            if (!PromotionManagementHandlerSupport.IsPlatformManaged(eventTarget, tenant.TenantId))
            {
                return PromotionManagementHandlerSupport.NotFound();
            }

            PromotionDefinition? definition = await promotions.GetDefinitionForUpdateAsync(tenant.TenantId, request.EventId, request.PromotionDefinitionId, token);
            if (definition is null)
            {
                return PromotionManagementHandlerSupport.NotFound();
            }

            try
            {
                definition.Publish(publishedAtUtc);
                PromotionCodeDigest digest = await digests.ComputeActiveAsync(tenant.TenantId, request.EventId, request.Code, token);
                PromotionCode code = PromotionCode.Create(definition, PromotionManagementHandlerSupport.MaskSuffix(digests.NormalizeCode(request.Code)), definition.ScopeMetadata);
                await promotions.AddPublishedCodeAsync(code, digest, token);
                await promotions.SaveChangesAsync(token);
                return PromotionManagementHandlerSupport.Success(definition.Id, "Promotion published.", PromotionManagementMapper.Map(definition, code, eventTarget!));
            }
            catch (ArgumentException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed(exception.Message);
            }
        }, cancellationToken);
    }
}

public sealed class RevokePromotionCommandHandler(
    IEventRepository events,
    IPromotionManagementRepository promotions,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<RevokePromotionCommand, PromotionManagementCommandResponseDto>
{
    public async Task<PromotionManagementCommandResponseDto> Handle(RevokePromotionCommand request, CancellationToken cancellationToken)
    {
        await new RevokePromotionCommandValidator().ValidateAndThrowAsync(request, cancellationToken);
        DateTime decisionAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, token);
            if (!PromotionManagementHandlerSupport.IsPlatformManaged(eventTarget, tenant.TenantId))
            {
                return PromotionManagementHandlerSupport.NotFound();
            }

            PromotionDefinition? definition = await promotions.GetDefinitionForUpdateAsync(tenant.TenantId, request.EventId, request.PromotionDefinitionId, token);
            if (definition is null)
            {
                return PromotionManagementHandlerSupport.NotFound();
            }

            try
            {
                definition.Revoke(decisionAtUtc, decisionAtUtc);
                await promotions.SaveChangesAsync(token);
                return PromotionManagementHandlerSupport.Success(definition.Id, "Promotion revoked.", PromotionManagementMapper.Map(definition, null, eventTarget!));
            }
            catch (ArgumentException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed(exception.Message);
            }
        }, cancellationToken);
    }
}

public sealed class RotatePromotionCodeCommandHandler(
    IEventRepository events,
    IPromotionManagementRepository promotions,
    IPromotionCodeDigestService digests,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<RotatePromotionCodeCommand, PromotionCodeIssuedCommandResponseDto>
{
    public async Task<PromotionCodeIssuedCommandResponseDto> Handle(RotatePromotionCodeCommand request, CancellationToken cancellationToken)
    {
        await new RotatePromotionCodeCommandValidator().ValidateAndThrowAsync(request, cancellationToken);
        DateTime rotatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, token);
            if (!PromotionManagementHandlerSupport.IsPlatformManaged(eventTarget, tenant.TenantId))
            {
                return PromotionManagementHandlerSupport.NotFound<PromotionCodeIssuedCommandResponseDto>();
            }

            PromotionDefinition? definition = await promotions.GetDefinitionForUpdateAsync(tenant.TenantId, request.EventId, request.PromotionDefinitionId, token);
            if (definition is null)
            {
                return PromotionManagementHandlerSupport.NotFound<PromotionCodeIssuedCommandResponseDto>();
            }

            try
            {
                PromotionCodeDigest digest = await digests.ComputeActiveAsync(tenant.TenantId, request.EventId, request.Code, token);
                PromotionCode code = PromotionCode.Create(definition, PromotionManagementHandlerSupport.MaskSuffix(digests.NormalizeCode(request.Code)), definition.ScopeMetadata);
                await promotions.ReplaceActiveCodeAsync(definition, code, digest, rotatedAtUtc, token);
                await promotions.SaveChangesAsync(token);
                return PromotionManagementHandlerSupport.IssuedSuccess(definition.Id, "Promotion code rotated.", PromotionManagementMapper.Map(definition, code, eventTarget!), request.Code);
            }
            catch (ArgumentException exception)
            {
                return PromotionManagementHandlerSupport.ValidationFailed<PromotionCodeIssuedCommandResponseDto>(exception.Message);
            }
        }, cancellationToken);
    }
}

internal static class PromotionManagementHandlerSupport
{
    public const string NotFoundFailureCode = "promotion_management_not_found";
    public const string ValidationFailureCode = "promotion_management_validation_failed";

    public static bool IsPlatformManaged(Event? eventTarget, Guid tenantId) =>
        eventTarget?.TenantId == tenantId
        && eventTarget.ParticipationConfiguration?.ParticipationHandlingModeId == (int)ParticipationHandlingModeEnum.PlatformManaged;

    public static async Task<EventTicketCatalogVersion?> GetScopedCatalogAsync(
        IEventTicketCatalogRepository catalogs,
        Guid eventId,
        Guid tenantId,
        Guid ticketCatalogVersionId,
        CancellationToken cancellationToken)
    {
        EventTicketCatalogVersion? catalog = await catalogs.GetManagementCatalogAsync(eventId, tenantId, cancellationToken)
            ?? await catalogs.GetPublishedCatalogAsync(eventId, tenantId, cancellationToken);

        return catalog?.Id == ticketCatalogVersionId ? catalog : null;
    }

    public static PromotionEligibility CreateEligibility(IReadOnlyCollection<Guid> ticketTypeIds) =>
        ticketTypeIds.Count == 0 ? PromotionEligibility.AllTickets() : PromotionEligibility.ForTicketTypes(ticketTypeIds);

    public static PromotionDiscountRule CreateDiscount(
        string currencyCode,
        string discountKind,
        long? fixedDiscountMinor,
        int? basisPointDiscount,
        long? maximumDiscountMinor) =>
        discountKind switch
        {
            "fixed" => PromotionDiscountRule.FixedMinor(currencyCode, fixedDiscountMinor ?? 0, maximumDiscountMinor),
            "basis_points" => PromotionDiscountRule.BasisPoints(currencyCode, basisPointDiscount ?? 0, maximumDiscountMinor),
            _ => throw new ArgumentException("Promotion discount kind is not supported.", nameof(discountKind))
        };

    public static string MaskSuffix(string normalizedCode) =>
        normalizedCode.Length <= 8 ? normalizedCode : normalizedCode[^8..];

    public static TResponse NotFound<TResponse>() where TResponse : BaseCommandResponse<Guid>, new() => new()
    {
        Success = false,
        Message = "Promotion management resource was not found.",
        FailureCode = NotFoundFailureCode,
        Errors = ["Promotion management resource was not found."]
    };

    public static TResponse ValidationFailed<TResponse>(string message) where TResponse : BaseCommandResponse<Guid>, new() => new()
    {
        Success = false,
        Message = "Promotion management request is invalid.",
        FailureCode = ValidationFailureCode,
        Errors = [message]
    };

    public static PromotionManagementCommandResponseDto NotFound() => NotFound<PromotionManagementCommandResponseDto>();

    public static PromotionManagementCommandResponseDto ValidationFailed(string message) => ValidationFailed<PromotionManagementCommandResponseDto>(message);

    public static PromotionManagementCommandResponseDto Success(Guid id, string message, PromotionManagementDto promotion) => new()
    {
        Id = id,
        Success = true,
        Message = message,
        Promotion = promotion
    };

    public static PromotionCodeIssuedCommandResponseDto IssuedSuccess(Guid id, string message, PromotionManagementDto promotion, string issuedCode) => new()
    {
        Id = id,
        Success = true,
        Message = message,
        Promotion = promotion,
        IssuedCode = issuedCode
    };
}
