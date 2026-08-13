// ABOUTME: Implements local organizer payment connection commands without provider I/O.
// ABOUTME: Enforces explicit actor control, scoped idempotency, uniqueness, replacement, and safe query mapping.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizerPaymentConnections;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.OrganizerPaymentConnections;

public sealed class RecordOrganizerPaymentConnectionCommandHandler(
    IOrganizerPaymentProviderConnectionRepository repository,
    IActorRepository actorRepository,
    ITenantUserRepository tenantUserRepository,
    IOrganizationTenantRepository organizationTenantRepository,
    IGroupTenantRepository groupTenantRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupMemberRepository groupMemberRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : IRequestHandler<RecordOrganizerPaymentConnectionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(RecordOrganizerPaymentConnectionCommand request, CancellationToken cancellationToken)
    {
        if (!await OrganizerPaymentActorAccess.AuthorizeAsync(request.TenantId, request.OrganizerActorId, tenantContext, currentUserService, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, cancellationToken))
        {
            return Failure(Guid.Empty, "organizer_payment_actor_denied", "The organizer actor is not controlled by the current user in this tenant.");
        }

        string providerCode;
        string connectPlatformId;
        string externalAccountId;
        try
        {
            OrganizerPaymentProviderConnection candidate = OrganizerPaymentProviderConnection.Create(Guid.CreateVersion7(), request.TenantId, request.OrganizerActorId, request.ProviderCode, request.ConnectPlatformId, request.ExternalAccountId, timeProvider.GetUtcNow().UtcDateTime);
            providerCode = candidate.ProviderCode;
            connectPlatformId = candidate.ConnectPlatformId;
            externalAccountId = candidate.ExternalAccountId;
        }
        catch (ArgumentException exception)
        {
            return Failure(Guid.Empty, "organizer_payment_connection_validation_failed", exception.Message);
        }

        Guid newId = Guid.CreateVersion7();
        DateTime createdAt = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            OrganizerPaymentProviderConnection? externalOwner = await repository.GetHistoricalByExternalAccountAsync(providerCode, connectPlatformId, externalAccountId, token);
            if (externalOwner is not null
                && (externalOwner.TenantId != request.TenantId
                    || externalOwner.OrganizerActorId != request.OrganizerActorId
                    || externalOwner.StatusId is (int)OrganizerPaymentProviderConnectionStatusEnum.Disabled or (int)OrganizerPaymentProviderConnectionStatusEnum.Replaced))
            {
                return Failure(externalOwner.Id, "organizer_payment_external_account_bound", "External account is already bound to another organizer scope.");
            }

            OrganizerPaymentProviderConnection? existing = await repository.GetActiveByScopeAsync(request.TenantId, request.OrganizerActorId, providerCode, connectPlatformId, token);
            if (existing is not null)
            {
                return existing.ExternalAccountId == externalAccountId
                    ? Success(existing.Id, "Organizer payment connection already exists.")
                    : Failure(existing.Id, "organizer_payment_connection_replace_required", "Active organizer payment connection must be replaced to change accounts.");
            }

            OrganizerPaymentProviderConnection connection = OrganizerPaymentProviderConnection.Create(newId, request.TenantId, request.OrganizerActorId, providerCode, connectPlatformId, externalAccountId, createdAt);
            await repository.CreateAsync(connection, token);
            await repository.SaveChangesAsync(token);
            return Success(connection.Id, "Organizer payment connection recorded.");
        }, cancellationToken);
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => OrganizerPaymentConnectionResponses.Success(id, message);
    private static BaseCommandResponse<Guid> Failure(Guid id, string code, string message) => OrganizerPaymentConnectionResponses.Failure(id, code, message);
}

public sealed class ReplaceOrganizerPaymentConnectionCommandHandler(
    IOrganizerPaymentProviderConnectionRepository repository,
    IActorRepository actorRepository,
    ITenantUserRepository tenantUserRepository,
    IOrganizationTenantRepository organizationTenantRepository,
    IGroupTenantRepository groupTenantRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupMemberRepository groupMemberRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : IRequestHandler<ReplaceOrganizerPaymentConnectionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(ReplaceOrganizerPaymentConnectionCommand request, CancellationToken cancellationToken)
    {
        if (!await OrganizerPaymentActorAccess.AuthorizeAsync(request.TenantId, request.OrganizerActorId, tenantContext, currentUserService, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, cancellationToken))
        {
            return OrganizerPaymentConnectionResponses.Failure(request.CurrentConnectionId, "organizer_payment_actor_denied", "The organizer actor is not controlled by the current user in this tenant.");
        }

        Guid replacementId = Guid.CreateVersion7();
        DateTime replacedAt = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            OrganizerPaymentProviderConnection? current = await repository.GetByTenantAndIdForUpdateAsync(request.TenantId, request.CurrentConnectionId, token);
            if (current is null || current.OrganizerActorId != request.OrganizerActorId)
            {
                return OrganizerPaymentConnectionResponses.Failure(request.CurrentConnectionId, "organizer_payment_connection_not_found", "Organizer payment connection was not found for this actor.");
            }

            string normalizedAccount;
            try
            {
                OrganizerPaymentProviderConnection probe = OrganizerPaymentProviderConnection.Create(Guid.CreateVersion7(), current.TenantId, current.OrganizerActorId, current.ProviderCode, current.ConnectPlatformId, request.NewExternalAccountId, replacedAt);
                normalizedAccount = probe.ExternalAccountId;
            }
            catch (ArgumentException exception)
            {
                return OrganizerPaymentConnectionResponses.Failure(current.Id, "organizer_payment_connection_validation_failed", exception.Message);
            }

            OrganizerPaymentProviderConnection? externalOwner = await repository.GetHistoricalByExternalAccountAsync(current.ProviderCode, current.ConnectPlatformId, normalizedAccount, token);
            if (externalOwner is not null)
            {
                return OrganizerPaymentConnectionResponses.Failure(externalOwner.Id, "organizer_payment_external_account_bound", "External account is already bound.");
            }

            try
            {
                OrganizerPaymentProviderConnection replacement = current.ReplaceWith(replacementId, normalizedAccount, replacedAt);
                await repository.CreateAsync(replacement, token);
                await repository.SaveChangesAsync(token);
                return OrganizerPaymentConnectionResponses.Success(replacement.Id, "Organizer payment connection replaced.");
            }
            catch (ArgumentException exception)
            {
                return OrganizerPaymentConnectionResponses.Failure(current.Id, "organizer_payment_connection_validation_failed", exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return OrganizerPaymentConnectionResponses.Failure(current.Id, "organizer_payment_connection_terminal", exception.Message);
            }
        }, cancellationToken);
    }
}

public sealed class DisableOrganizerPaymentConnectionCommandHandler(
    IOrganizerPaymentProviderConnectionRepository repository,
    IActorRepository actorRepository,
    ITenantUserRepository tenantUserRepository,
    IOrganizationTenantRepository organizationTenantRepository,
    IGroupTenantRepository groupTenantRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupMemberRepository groupMemberRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : IRequestHandler<DisableOrganizerPaymentConnectionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(DisableOrganizerPaymentConnectionCommand request, CancellationToken cancellationToken)
    {
        if (!await OrganizerPaymentActorAccess.AuthorizeAsync(request.TenantId, request.OrganizerActorId, tenantContext, currentUserService, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, cancellationToken))
        {
            return OrganizerPaymentConnectionResponses.Failure(request.ConnectionId, "organizer_payment_actor_denied", "The organizer actor is not controlled by the current user in this tenant.");
        }

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            OrganizerPaymentProviderConnection? connection = await repository.GetByTenantAndIdForUpdateAsync(request.TenantId, request.ConnectionId, token);
            if (connection is null || connection.OrganizerActorId != request.OrganizerActorId)
            {
                return OrganizerPaymentConnectionResponses.Failure(request.ConnectionId, "organizer_payment_connection_not_found", "Organizer payment connection was not found for this actor.");
            }

            try
            {
                connection.Disable(request.ReasonCode, timeProvider.GetUtcNow().UtcDateTime);
                await repository.SaveChangesAsync(token);
                return OrganizerPaymentConnectionResponses.Success(connection.Id, "Organizer payment connection disabled.");
            }
            catch (ArgumentException exception)
            {
                return OrganizerPaymentConnectionResponses.Failure(connection.Id, "organizer_payment_connection_validation_failed", exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return OrganizerPaymentConnectionResponses.Failure(connection.Id, "organizer_payment_connection_terminal", exception.Message);
            }
        }, cancellationToken);
    }
}

public sealed class ListOrganizerPaymentConnectionsQueryHandler(
    IOrganizerPaymentProviderConnectionRepository repository,
    IActorRepository actorRepository,
    ITenantUserRepository tenantUserRepository,
    IOrganizationTenantRepository organizationTenantRepository,
    IGroupTenantRepository groupTenantRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupMemberRepository groupMemberRepository,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<ListOrganizerPaymentConnectionsQuery, IReadOnlyList<OrganizerPaymentConnectionDto>>
{
    public async Task<IReadOnlyList<OrganizerPaymentConnectionDto>> Handle(ListOrganizerPaymentConnectionsQuery request, CancellationToken cancellationToken)
    {
        if (!await OrganizerPaymentActorAccess.AuthorizeAsync(request.TenantId, request.OrganizerActorId, tenantContext, currentUserService, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, cancellationToken))
        {
            return [];
        }

        IReadOnlyList<OrganizerPaymentProviderConnection> connections = await repository.ListByTenantAndActorAsync(request.TenantId, request.OrganizerActorId, cancellationToken);
        return connections.Select(OrganizerPaymentConnectionMapper.ToDto).ToArray();
    }
}

public sealed class GetOrganizerPaymentConnectionQueryHandler(
    IOrganizerPaymentProviderConnectionRepository repository,
    IActorRepository actorRepository,
    ITenantUserRepository tenantUserRepository,
    IOrganizationTenantRepository organizationTenantRepository,
    IGroupTenantRepository groupTenantRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupMemberRepository groupMemberRepository,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetOrganizerPaymentConnectionQuery, OrganizerPaymentConnectionDto?>
{
    public async Task<OrganizerPaymentConnectionDto?> Handle(GetOrganizerPaymentConnectionQuery request, CancellationToken cancellationToken)
    {
        if (!await OrganizerPaymentActorAccess.AuthorizeAsync(request.TenantId, request.OrganizerActorId, tenantContext, currentUserService, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, cancellationToken))
        {
            return null;
        }

        OrganizerPaymentProviderConnection? connection = await repository.GetByTenantAndIdForUpdateAsync(request.TenantId, request.ConnectionId, cancellationToken);
        return connection is not null && connection.OrganizerActorId == request.OrganizerActorId
            ? OrganizerPaymentConnectionMapper.ToDto(connection)
            : null;
    }
}

internal static class OrganizerPaymentActorAccess
{
    internal static async Task<bool> AuthorizeAsync(
        Guid tenantId,
        Guid organizerActorId,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IActorRepository actorRepository,
        ITenantUserRepository tenantUserRepository,
        IOrganizationTenantRepository organizationTenantRepository,
        IGroupTenantRepository groupTenantRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository,
        CancellationToken cancellationToken) =>
        tenantId != Guid.Empty
        && organizerActorId != Guid.Empty
        && tenantContext.TenantId == tenantId
        && currentUserService.UserId is { } userId
        && await EventOrganizerClaims.ClaimantActorAccessEvaluator.CanControlAsync(
            organizerActorId,
            userId,
            tenantId,
            actorRepository,
            tenantUserRepository,
            organizationTenantRepository,
            groupTenantRepository,
            organizationMemberRepository,
            groupMemberRepository,
            cancellationToken);
}

internal static class OrganizerPaymentConnectionMapper
{
    internal static OrganizerPaymentConnectionDto ToDto(OrganizerPaymentProviderConnection connection) => new()
    {
        Id = connection.Id,
        TenantId = connection.TenantId,
        OrganizerActorId = connection.OrganizerActorId,
        ProviderCode = connection.ProviderCode,
        ConnectPlatformId = connection.ConnectPlatformId,
        ExternalAccountId = connection.ExternalAccountId,
        StatusId = connection.StatusId,
        MerchantCountryCode = connection.MerchantCountryCode,
        ChargeCapabilityStateId = connection.ChargeCapabilityStateId,
        RequirementsStateId = connection.RequirementsStateId,
        SupportedCurrencyCodes = connection.SupportedCurrencyCodes,
        LastReadinessObservedAt = connection.LastReadinessObservedAt,
        LastReadinessEvidenceRevision = connection.LastReadinessEvidenceRevision,
        ReplacesConnectionId = connection.ReplacesConnectionId,
        ReplacedByConnectionId = connection.ReplacedByConnectionId,
        ReplacedAt = connection.ReplacedAt,
        DisabledAt = connection.DisabledAt,
        DisabledReasonCode = connection.DisabledReasonCode
    };
}

internal static class OrganizerPaymentConnectionResponses
{
    internal static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    internal static BaseCommandResponse<Guid> Failure(Guid id, string code, string message) => new()
    {
        Success = false,
        Id = id,
        FailureCode = code,
        Message = message,
        Errors = [message]
    };
}
