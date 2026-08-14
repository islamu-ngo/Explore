// ABOUTME: Implements local organizer payment connection commands without provider I/O.
// ABOUTME: Enforces explicit actor control, scoped idempotency, uniqueness, replacement, and safe query mapping.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.OrganizerPaymentConnections.Commands;
using Explore.Application.DTOs.OrganizerPaymentConnections;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.OrganizerPaymentConnections;

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
