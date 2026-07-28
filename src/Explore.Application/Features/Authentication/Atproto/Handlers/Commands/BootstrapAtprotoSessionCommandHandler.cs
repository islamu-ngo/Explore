// ABOUTME: Verifies a PDS session, enforces linked-account identity, and atomically stores local ATProto state.
// ABOUTME: Issues a platform JWT only after the identity, index, and encrypted OAuth session commit succeeds.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Authentication.Atproto.Models;
using Explore.Application.Features.Authentication.Atproto.Requests.Commands;
using Explore.Application.Features.Authentication.Atproto.Validators;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Authentication.Atproto.Handlers.Commands;

public sealed class BootstrapAtprotoSessionCommandHandler(
    IAtprotoOAuthSecurityGateway securityGateway,
    IAtprotoSessionTokenIssuer tokenIssuer,
    IUserExternalLoginRepository externalLoginRepository,
    IUserRepository userRepository,
    IActorRepository actorRepository,
    IAtprotoIdentityRepository atprotoIdentityRepository,
    ITenantUserRepository tenantUserRepository,
    ITenantUserRoleGrantRepository tenantUserRoleGrantRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationTenantRepository organizationTenantRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupRepository groupRepository,
    IGroupTenantRepository groupTenantRepository,
    IGroupMemberRepository groupMemberRepository,
    IAdminCacheInvalidator adminCacheInvalidator,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    TimeProvider timeProvider)
    : IRequestHandler<BootstrapAtprotoSessionCommand, AtprotoSessionBootstrapResult>
{
    public async Task<AtprotoSessionBootstrapResult> Handle(
        BootstrapAtprotoSessionCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await new BootstrapAtprotoSessionCommandValidator()
            .ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return AtprotoSessionBootstrapResult.Failed("invalid_request");
        }

        var tenantId = tenantContext.TenantId;
        var verification = await securityGateway.VerifyAsync(
            new AtprotoOAuthVerificationInput(
                request.ExpectedDid,
                new Uri(request.ExpectedPdsUri, UriKind.Absolute),
                request.OAuthClientKeyId,
                request.OAuthSessionPayload),
            cancellationToken).ConfigureAwait(false);
        if (verification.Session is not { } verified)
        {
            return AtprotoSessionBootstrapResult.Failed(verification.FailureCode ?? "pds_verification_failed");
        }

        var login = await externalLoginRepository
            .GetByProviderAndKey("atproto", verified.Did).ConfigureAwait(false);
        if (!IsExactLinkedLogin(login, verified.Did))
        {
            return AtprotoSessionBootstrapResult.Failed("account_not_linked");
        }

        var user = await userRepository.GetById(login.UserId).ConfigureAwait(false);
        var actor = await actorRepository
            .GetActorByUserId(login.UserId).ConfigureAwait(false);
        if (user is null || actor is null || actor.UserId != user.Id)
        {
            return AtprotoSessionBootstrapResult.Failed("linked_identity_incomplete");
        }

        var preparedSession = await securityGateway.PreparePersistenceAsync(
            verified,
            tenantId,
            user.Id,
            cancellationToken).ConfigureAwait(false);
        var indexedAt = timeProvider.GetUtcNow().UtcDateTime;
        Guid representedActorId = Guid.Empty;
        Guid participationId = Guid.Empty;
        var transactionFailure = await unitOfWork.ExecuteInTransactionAsync<AtprotoSessionBootstrapResult?>(async transactionToken =>
        {
            var currentLogin = await externalLoginRepository
                .GetByProviderAndKey("atproto", verified.Did).ConfigureAwait(false);
            if (!IsExactLinkedLogin(currentLogin, verified.Did)
                || currentLogin!.UserId != user.Id)
            {
                return AtprotoSessionBootstrapResult.Failed("account_not_linked");
            }

            var tenantUser = await tenantUserRepository
                .GetByTenantAndUserAsync(tenantId, user.Id, transactionToken).ConfigureAwait(false);
            if (tenantUser is not null
                && tenantUser.ActorId is not null
                && tenantUser.ActorId != actor.Id)
            {
                return AtprotoSessionBootstrapResult.Failed("linked_identity_incomplete");
            }

            var identity = await atprotoIdentityRepository
                .GetByDid(verified.Did, transactionToken).ConfigureAwait(false);
            Actor representedActor;
            if (identity is null)
            {
                representedActor = await CreateRepresentedActor(
                    request.Classification,
                    actor,
                    user.Id,
                    verified.Handle,
                    indexedAt).ConfigureAwait(false);
                identity = new AtprotoIdentity
                {
                    Did = verified.Did,
                    ActorId = representedActor.Id,
                    Actor = representedActor,
                    Handle = verified.Handle,
                    PdsHost = verified.PdsUri.AbsoluteUri,
                    IsActive = true,
                    LastResolvedAt = indexedAt,
                    LastSeenAt = indexedAt,
                    CreatedAt = indexedAt,
                    CreatedBy = user.Id
                };
                await atprotoIdentityRepository.Create(identity).ConfigureAwait(false);
            }
            else
            {
                representedActor = identity.Actor;
                if (!MatchesClassification(representedActor, actor, request.Classification))
                {
                    return AtprotoSessionBootstrapResult.Failed("classification_conflict");
                }

                identity.RefreshVerifiedMetadata(
                    verified.Did,
                    verified.Handle,
                    verified.PdsUri.AbsoluteUri,
                    signingKey: null,
                    indexedAt);
                identity.UpdatedAt = indexedAt;
                identity.UpdatedBy = user.Id;
                await atprotoIdentityRepository.Update(identity).ConfigureAwait(false);
            }

            if (tenantUser is null)
            {
                tenantUser = await tenantUserRepository.Create(new TenantUser
                {
                    TenantId = tenantId,
                    Tenant = null!,
                    UserId = user.Id,
                    User = user,
                    ActorId = actor.Id,
                    Actor = actor,
                    StatusId = (int)TenantUserStatusEnum.Active,
                    JoinedAt = indexedAt,
                    CreatedAt = indexedAt,
                    CreatedBy = user.Id
                }).ConfigureAwait(false);
            }
            else if (tenantUser.ActorId is null)
            {
                tenantUser.ActorId = actor.Id;
                tenantUser.Actor = actor;
                tenantUser.UpdatedAt = indexedAt;
                tenantUser.UpdatedBy = user.Id;
                await tenantUserRepository.Update(tenantUser).ConfigureAwait(false);
            }

            var isTenantAdmin = await tenantUserRoleGrantRepository
                .IsTenantAdminInCurrentTenantAsync(tenantId, user.Id, transactionToken)
                .ConfigureAwait(false);
            participationId = await EnsureParticipation(
                request.Classification,
                representedActor,
                tenantUser,
                user,
                tenantId,
                isTenantAdmin,
                indexedAt,
                transactionToken).ConfigureAwait(false);
            representedActorId = representedActor.Id;

            await securityGateway.PersistPreparedAsync(preparedSession, transactionToken).ConfigureAwait(false);
            return null;
        }, cancellationToken).ConfigureAwait(false);

        if (transactionFailure is not null)
        {
            return transactionFailure;
        }

        adminCacheInvalidator.InvalidateUser(user.Id);
        var issued = await tokenIssuer
            .IssueAsync(user.Id, tenantId, verified.Did, cancellationToken).ConfigureAwait(false);
        return AtprotoSessionBootstrapResult.Succeeded(
            user.Id,
            representedActorId,
            participationId,
            request.Classification,
            issued,
            request.CanonicalActorId,
            request.ExpectedCanonicalActorConcurrencyStamp);
    }

    private async Task<Actor> CreateRepresentedActor(
        AtprotoSubjectClassification classification,
        Actor userActor,
        Guid userId,
        string verifiedHandle,
        DateTime createdAt)
    {
        if (classification == AtprotoSubjectClassification.Person)
        {
            return userActor;
        }

        if (classification == AtprotoSubjectClassification.Organization)
        {
            var organization = await organizationRepository.Create(new Organization
            {
                Pii = new OrganizationPii { FullName = verifiedHandle },
                CreatedAt = createdAt,
                CreatedBy = userId
            }).ConfigureAwait(false);
            return await actorRepository.Create(new Actor
            {
                ActorTypeId = (int)ActorTypeEnum.Organization,
                ActorType = null!,
                OrganizationId = organization.Id,
                Organization = organization,
                Pii = new ActorPii { DisplayName = verifiedHandle },
                CreatedAt = createdAt,
                CreatedBy = userId
            }).ConfigureAwait(false);
        }

        var group = await groupRepository.Create(new Group
        {
            FullName = verifiedHandle,
            CreatedAt = createdAt,
            CreatedBy = userId
        }).ConfigureAwait(false);
        return await actorRepository.Create(new Actor
        {
            ActorTypeId = (int)ActorTypeEnum.Group,
            ActorType = null!,
            GroupId = group.Id,
            Group = group,
            Pii = new ActorPii { DisplayName = verifiedHandle },
            CreatedAt = createdAt,
            CreatedBy = userId
        }).ConfigureAwait(false);
    }

    private async Task<Guid> EnsureParticipation(
        AtprotoSubjectClassification classification,
        Actor representedActor,
        TenantUser tenantUser,
        User user,
        Guid tenantId,
        bool isTenantAdmin,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        if (classification == AtprotoSubjectClassification.Person)
        {
            return tenantUser.Id;
        }

        if (classification == AtprotoSubjectClassification.Organization)
        {
            var organizationId = representedActor.OrganizationId!.Value;
            var participation = await organizationTenantRepository
                .GetByOrganizationAndTenant(organizationId, tenantId, cancellationToken)
                .ConfigureAwait(false)
                ?? await organizationTenantRepository.Create(new OrganizationTenant
                {
                    TenantId = tenantId,
                    Tenant = null!,
                    OrganizationId = organizationId,
                    Organization = null!,
                    ApprovalStatusId = isTenantAdmin
                        ? (int)ApprovalStatusEnum.Approved
                        : (int)ApprovalStatusEnum.Pending,
                    ApprovalStatus = null!,
                    IsVisible = isTenantAdmin,
                    IsOrganizerEligible = isTenantAdmin,
                    ApprovedAt = isTenantAdmin ? createdAt : null,
                    ApprovedBy = isTenantAdmin ? user.Id : null,
                    CreatedAt = createdAt,
                    CreatedBy = user.Id
                }).ConfigureAwait(false);
            if (!await organizationMemberRepository.Exists(organizationId, user.Id).ConfigureAwait(false))
            {
                await organizationMemberRepository.Create(new OrganizationMember
                {
                    OrganizationTenantId = participation.Id,
                    OrganizationTenant = participation,
                    UserId = user.Id,
                    User = user,
                    RoleId = (int)RoleEnum.OrgAdmin,
                    Role = null!,
                    TenantId = tenantId,
                    Tenant = null!,
                    CreatedAt = createdAt,
                    CreatedBy = user.Id
                }).ConfigureAwait(false);
            }

            return participation.Id;
        }

        var groupId = representedActor.GroupId!.Value;
        var groupParticipation = await groupTenantRepository
            .GetByGroupAndTenant(groupId, tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? await groupTenantRepository.Create(new GroupTenant
            {
                TenantId = tenantId,
                Tenant = null!,
                GroupId = groupId,
                Group = null!,
                ApprovalStatusId = (int)ApprovalStatusEnum.Pending,
                ApprovalStatus = null!,
                CreatedAt = createdAt,
                CreatedBy = user.Id
            }).ConfigureAwait(false);
        if (!await groupMemberRepository.Exists(groupId, user.Id).ConfigureAwait(false))
        {
            await groupMemberRepository.Create(new GroupMember
            {
                GroupTenantId = groupParticipation.Id,
                GroupTenant = groupParticipation,
                UserId = user.Id,
                User = user,
                RoleId = (int)RoleEnum.GroupAdmin,
                Role = null!,
                TenantId = tenantId,
                Tenant = null!,
                CreatedAt = createdAt,
                CreatedBy = user.Id
            }).ConfigureAwait(false);
        }

        return groupParticipation.Id;
    }

    private static bool MatchesClassification(
        Actor representedActor,
        Actor userActor,
        AtprotoSubjectClassification classification) => classification switch
        {
            AtprotoSubjectClassification.Person => representedActor.Id == userActor.Id
                && representedActor.UserId == userActor.UserId,
            AtprotoSubjectClassification.Organization => representedActor.OrganizationId is not null,
            AtprotoSubjectClassification.Group => representedActor.GroupId is not null,
            _ => false
        };

    private static bool IsExactLinkedLogin(UserExternalLogin? login, string did) =>
        login is not null
        && string.Equals(login.Provider, "atproto", StringComparison.Ordinal)
        && string.Equals(login.ProviderKey, did, StringComparison.Ordinal);
}
