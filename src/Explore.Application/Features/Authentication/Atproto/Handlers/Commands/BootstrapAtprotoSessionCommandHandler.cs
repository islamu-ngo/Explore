// ABOUTME: Verifies a PDS session, enforces linked-account identity, and atomically stores local ATProto state.
// ABOUTME: Issues a platform JWT only after the identity, index, and encrypted OAuth session commit succeeds.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Authentication.Atproto.Models;
using Explore.Application.Features.Authentication.Atproto.Requests.Commands;
using Explore.Application.Features.Authentication.Atproto.Services;
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
    AtprotoSubjectOnboardingOperation onboardingOperation,
    IUnitOfWork unitOfWork,
    IAdminCacheInvalidator adminCacheInvalidator,
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

        var preparedSession = await securityGateway.PreparePersistenceAsync(
            verified,
            tenantId,
            login!.UserId,
            cancellationToken).ConfigureAwait(false);
        var indexedAt = timeProvider.GetUtcNow().UtcDateTime;
        var onboarding = await unitOfWork.ExecuteSerializableAsync(async transactionToken =>
        {
            var user = await userRepository.GetById(login.UserId).ConfigureAwait(false);
            var actor = await actorRepository
                .GetTrackedActorByUserId(login.UserId, transactionToken).ConfigureAwait(false);
            if (user is null || actor is null || actor.UserId != user.Id)
            {
                return AtprotoSubjectOnboardingResult.Failed("linked_identity_incomplete");
            }

            var result = await onboardingOperation.ExecuteAsync(
                request,
                verified,
                user,
                actor,
                tenantId,
                indexedAt,
                transactionToken).ConfigureAwait(false);
            if (result.Success)
            {
                await securityGateway.PersistPreparedAsync(preparedSession, transactionToken).ConfigureAwait(false);
            }

            return result;
        }, cancellationToken).ConfigureAwait(false);
        if (!onboarding.Success) return AtprotoSessionBootstrapResult.Failed(onboarding.FailureCode);

        adminCacheInvalidator.InvalidateUser(login.UserId);
        var issued = await tokenIssuer
            .IssueAsync(login.UserId, tenantId, verified.Did, cancellationToken).ConfigureAwait(false);
        return AtprotoSessionBootstrapResult.Succeeded(
            login.UserId,
            onboarding.ActorId!.Value,
            onboarding.ParticipationId!.Value,
            request.Classification,
            issued,
            request.CanonicalActorId,
            request.ExpectedCanonicalActorConcurrencyStamp);
    }

    private static bool IsExactLinkedLogin(UserExternalLogin? login, string did) =>
        login is not null
        && string.Equals(login.Provider, "atproto", StringComparison.Ordinal)
        && string.Equals(login.ProviderKey, did, StringComparison.Ordinal);
}
