// ABOUTME: Verifies a PDS session, enforces linked-account identity, and atomically stores local ATProto state.
// ABOUTME: Issues a platform JWT only after the identity, index, and encrypted OAuth session commit succeeds.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Authentication;
using Explore.Application.Features.Authentication.Atproto.Models;
using Explore.Application.Features.Authentication.Atproto.Requests.Commands;
using Explore.Application.Features.Authentication.Atproto.Services;
using Explore.Application.Features.Authentication.Atproto.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Explore.Application.Features.Authentication.Atproto.Handlers.Commands;

public sealed class BootstrapAtprotoSessionCommandHandler(
    IAtprotoOAuthSecurityGateway securityGateway,
    IAtprotoSessionTokenIssuer tokenIssuer,
    ISender sender,
    IUserExternalLoginRepository externalLoginRepository,
    IInstanceBootstrapStateRepository bootstrapRepository,
    IUserRepository userRepository,
    IActorRepository actorRepository,
    AtprotoSubjectOnboardingOperation onboardingOperation,
    IUnitOfWork unitOfWork,
    IAdminCacheInvalidator adminCacheInvalidator,
    ITenantContext tenantContext,
    IConfiguration configuration,
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

        if (verified.Did != request.ExpectedDid)
        {
            return AtprotoSessionBootstrapResult.Failed("pds_identity_mismatch");
        }

        ProviderAccountKey accountKey = PlatformIdentityPrincipalExtensions.CreateAtprotoAccountKey(verified.Did);
        var login = await externalLoginRepository
            .GetByProviderAndKey("atproto", accountKey).ConfigureAwait(false);
        InstanceBootstrapState? bootstrap = await bootstrapRepository
            .GetCurrent(cancellationToken).ConfigureAwait(false);
        bool configuredAccount = bootstrap?.Mode == InstanceBootstrapMode.ConfiguredAdministrator;
        if (configuredAccount)
        {
            var claim = await sender.Send(
                new ClaimConfiguredInstanceAdministratorCommand
                {
                    AuthenticatedAccount = accountKey,
                    UserId = IsExactLinkedLogin(login, accountKey)
                        ? login!.UserId
                        : Guid.CreateVersion7(),
                    Email = configuration["INSTANCE_BOOTSTRAP_ADMIN_EMAIL"],
                    FirstName = configuration["INSTANCE_BOOTSTRAP_ADMIN_FIRST_NAME"],
                    LastName = configuration["INSTANCE_BOOTSTRAP_ADMIN_LAST_NAME"],
                    EmailVerified = false
                },
                cancellationToken).ConfigureAwait(false);

            if (!claim.IsSuccess)
            {
                if (bootstrap?.Status != InstanceBootstrapStatus.Completed
                    || !IsExactLinkedLogin(login, accountKey)
                    || bootstrap.CompletedByUserId != login!.UserId)
                {
                    return AtprotoSessionBootstrapResult.Failed("account_not_linked");
                }
            }

            login = await externalLoginRepository
                .GetByProviderAndKey("atproto", accountKey).ConfigureAwait(false);
            if (!IsExactLinkedLogin(login, accountKey))
            {
                return AtprotoSessionBootstrapResult.Failed("configured_claim_incomplete");
            }
        }
        else if (!IsExactLinkedLogin(login, accountKey))
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

    private static bool IsExactLinkedLogin(UserExternalLogin? login, ProviderAccountKey accountKey) =>
        login is not null
        && string.Equals(login.Provider, "atproto", StringComparison.Ordinal)
        && string.Equals(login.ProviderKey, accountKey.Value, StringComparison.Ordinal);
}
