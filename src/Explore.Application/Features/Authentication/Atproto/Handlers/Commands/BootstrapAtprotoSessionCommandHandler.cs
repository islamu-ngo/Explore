// ABOUTME: Verifies a PDS session, enforces linked-account identity, and atomically stores local ATProto state.
// ABOUTME: Issues a platform JWT only after the identity, index, and encrypted OAuth session commit succeeds.

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
        if (!IsExactLinkedLogin(login, verified.Did, tenantId))
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

        var linkedIdentity = await atprotoIdentityRepository
            .GetByDid(verified.Did, cancellationToken).ConfigureAwait(false);
        if (linkedIdentity is not null && linkedIdentity.ActorId != actor.Id)
        {
            return AtprotoSessionBootstrapResult.Failed("identity_conflict");
        }

        var indexedAt = timeProvider.GetUtcNow().UtcDateTime;
        var transactionFailure = await unitOfWork.ExecuteInTransactionAsync<AtprotoSessionBootstrapResult?>(async transactionToken =>
        {
            var currentLogin = await externalLoginRepository
                .GetByProviderAndKey("atproto", verified.Did).ConfigureAwait(false);
            if (!IsExactLinkedLogin(currentLogin, verified.Did, tenantId)
                || currentLogin!.UserId != user.Id)
            {
                return AtprotoSessionBootstrapResult.Failed("account_not_linked");
            }

            var identity = await atprotoIdentityRepository
                .GetByDid(verified.Did, transactionToken).ConfigureAwait(false);
            if (identity is null)
            {
                identity = new AtprotoIdentity
                {
                    Did = verified.Did,
                    ActorId = actor.Id,
                    Actor = actor,
                    Handle = verified.Handle,
                    PdsHost = verified.PdsUri.AbsoluteUri,
                    IsActive = true,
                    LastResolvedAt = indexedAt,
                    LastSeenAt = indexedAt
                };
                await atprotoIdentityRepository.Create(identity).ConfigureAwait(false);
            }
            else
            {
                if (identity.ActorId != actor.Id)
                {
                    return AtprotoSessionBootstrapResult.Failed("identity_conflict");
                }

                identity.RefreshVerifiedMetadata(
                    verified.Did,
                    verified.Handle,
                    verified.PdsUri.AbsoluteUri,
                    signingKey: null,
                    indexedAt);
                await atprotoIdentityRepository.Update(identity).ConfigureAwait(false);
            }

            await securityGateway.PersistAsync(
                verified,
                tenantId,
                user.Id,
                transactionToken).ConfigureAwait(false);
            return null;
        }, cancellationToken).ConfigureAwait(false);

        if (transactionFailure is not null)
        {
            return transactionFailure;
        }

        var issued = await tokenIssuer
            .IssueAsync(user.Id, tenantId, verified.Did, cancellationToken).ConfigureAwait(false);
        return AtprotoSessionBootstrapResult.Succeeded(user.Id, issued);
    }

    private static bool IsExactLinkedLogin(UserExternalLogin? login, string did, Guid tenantId) =>
        login is not null
        && login.TenantId == tenantId
        && string.Equals(login.Provider, "atproto", StringComparison.Ordinal)
        && string.Equals(login.ProviderKey, did, StringComparison.Ordinal);
}
