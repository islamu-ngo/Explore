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
    IIndexedDidRepository indexedDidRepository,
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
            .GetActorByUserIdAndTenantId(login.UserId, tenantId).ConfigureAwait(false);
        if (user is null || actor is null || actor.UserId != user.Id)
        {
            return AtprotoSessionBootstrapResult.Failed("linked_identity_incomplete");
        }

        if (!string.IsNullOrWhiteSpace(actor.Did)
            && !string.Equals(actor.Did, verified.Did, StringComparison.Ordinal))
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

            actor.Did = verified.Did;
            actor.Handle = verified.Handle;
            actor.PdsHost = verified.PdsUri.AbsoluteUri;
            actor.DidCustodyTypeId = (int)DidCustodyTypeEnum.SelfCustody;
            actor.IndexedAt = indexedAt;
            await actorRepository.Update(actor).ConfigureAwait(false);

            var indexedDid = await indexedDidRepository.GetById(verified.Did).ConfigureAwait(false);
            if (indexedDid is null)
            {
                await indexedDidRepository.Create(new IndexedDid
                {
                    Did = verified.Did,
                    Handle = verified.Handle,
                    PdsHost = verified.PdsUri.AbsoluteUri,
                    IsActive = true,
                    LastIndexedAt = indexedAt,
                    LastSeenAt = indexedAt
                }).ConfigureAwait(false);
            }
            else
            {
                indexedDid.Handle = verified.Handle;
                indexedDid.PdsHost = verified.PdsUri.AbsoluteUri;
                indexedDid.IsActive = true;
                indexedDid.LastIndexedAt = indexedAt;
                indexedDid.LastSeenAt = indexedAt;
                await indexedDidRepository.Update(indexedDid).ConfigureAwait(false);
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
