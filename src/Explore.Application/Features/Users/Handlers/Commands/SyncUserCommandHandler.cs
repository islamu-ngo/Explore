// ABOUTME: Handles user synchronization from external identity providers (Keycloak, Google, ATProto).
// ABOUTME: Uses IUnitOfWork to atomically create/update user, actor, and external login records.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Authentication;
using Explore.Application.DTOs.User;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Features.InstanceOnboarding.Services;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;


namespace Explore.Application.Features.Users.Handlers.Commands;

public class SyncUserCommandHandler : IRequestHandler<SyncUserCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IActorRepository _actorRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IInstanceBootstrapStateRepository _bootstrapRepository;
    private readonly ITenantContext _tenantContext;
    private readonly InstanceOnboardingCompletionOperation _onboardingCompletion;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncUserCommandHandler> _logger;

    public SyncUserCommandHandler(
        IUserRepository userRepository,
        IUserExternalLoginRepository userExternalLoginRepository,
        IActorRepository actorRepository,
        ITenantRepository tenantRepository,
        IInstanceBootstrapStateRepository bootstrapRepository,
        ITenantContext tenantContext,
        InstanceOnboardingCompletionOperation onboardingCompletion,
        HybridCache cache,
        IUnitOfWork unitOfWork,
        ILogger<SyncUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _userExternalLoginRepository = userExternalLoginRepository;
        _actorRepository = actorRepository;
        _tenantRepository = tenantRepository;
        _bootstrapRepository = bootstrapRepository;
        _tenantContext = tenantContext;
        _onboardingCompletion = onboardingCompletion;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var userDto = request.UserDto;

        try
        {
            var provider = NormalizeProvider(userDto.AuthProvider);
            ProviderAccountKey accountKey = request.AccountKey;
            var providerUserId = accountKey.Value;
            var supportsEmailAutoMatch = SupportsEmailAutoMatch(provider);
            var email = NormalizeEmail(userDto.Email);

            bool usesAtprotoAuthority = provider == "atproto";
            bool hasAtprotoAuthority =
                accountKey.ProviderKind == InstanceBootstrapProviderKind.Atproto;
            if (usesAtprotoAuthority != hasAtprotoAuthority)
            {
                return BaseCommandResponse.Validation<Guid>(
                    ["Provider account authority is invalid."],
                    "Provider account authority is invalid.");
            }

            var existingLogin = await _userExternalLoginRepository.GetByProviderAndKey(provider, accountKey);
            InstanceBootstrapState? bootstrap = await _bootstrapRepository.GetCurrent(cancellationToken);
            if (bootstrap is
                {
                    Mode: InstanceBootstrapMode.ConfiguredAdministrator
                })
            {
                if (bootstrap.ProviderKind == InstanceBootstrapProviderKind.Keycloak
                    && provider != "keycloak")
                {
                    return BaseCommandResponse.Failure<Guid>(
                        "configured_administrator_claim_mismatch",
                        "Configured administrator claim did not match.");
                }

                Guid claimUserId = existingLogin?.UserId
                    ?? (userDto.Id == Guid.Empty ? Guid.CreateVersion7() : userDto.Id);
                BaseCommandResponse<Guid> claim = await _onboardingCompletion.ClaimConfiguredAsync(
                    new ClaimConfiguredInstanceAdministratorCommand
                    {
                        AuthenticatedAccount = accountKey,
                        UserId = claimUserId,
                        Email = email,
                        FirstName = userDto.FirstName,
                        LastName = userDto.LastName,
                        EmailVerified = userDto.EmailVerified
                    },
                    cancellationToken);
                if (bootstrap.Status == InstanceBootstrapStatus.Pending)
                {
                    return claim;
                }

                if (!claim.IsSuccess
                    && (existingLogin is null
                        || bootstrap.CompletedByUserId != existingLogin.UserId))
                {
                    return claim;
                }
            }

            if (!supportsEmailAutoMatch && string.IsNullOrWhiteSpace(email))
            {
                if (existingLogin == null)
                {
                    const string message =
                        "AT Protocol identity must be explicitly linked to an existing account before sign-in sync without email.";
                    return BaseCommandResponse.Validation<Guid>([message], message);
                }
            }

            // Pre-reads for user resolution — outside transaction for fast rejection
            User? user = null;

            if (existingLogin != null)
            {
                user = await _userRepository.GetById(existingLogin.UserId);
            }

            if (user == null && userDto.Id != Guid.Empty)
            {
                user = await _userRepository.GetById(userDto.Id);
            }

            if (user == null
                && supportsEmailAutoMatch
                && userDto.EmailVerified == true
                && !string.IsNullOrWhiteSpace(email))
            {
                IReadOnlyList<User> emailMatches =
                    await _userRepository.GetUsersByNormalizedEmailAsync(email, cancellationToken);
                if (emailMatches.Count > 1)
                {
                    const string message =
                        "Verified email resolves to multiple user accounts; explicit linking is required.";
                    return BaseCommandResponse.Validation<Guid>([message], message);
                }

                user = emailMatches.SingleOrDefault();
            }


            // Fast-rejection for missing email on new account creation — before any writes
            string? safeEmail = null;
            if (user == null)
            {
                safeEmail = ResolveEmailForCreation(provider, email);
                if (string.IsNullOrWhiteSpace(safeEmail))
                {
                    return BaseCommandResponse.Validation<Guid>(
                        ["Email is required to create a new account for this provider."],
                        "Email is required to create a new account for this provider.");
                }
            }

            // IDs generated before lambda — captured via closure for retry safety
            var newUserId = userDto.Id != Guid.Empty ? userDto.Id : Guid.CreateVersion7();
            var loginId = Guid.CreateVersion7();

            var syncedUser = await _unitOfWork.ExecuteInTransactionAsync<User>(async ct =>
            {
                if (user == null)
                {
                    var newUser = new User
                    {
                        Id = newUserId,
                        Pii = new UserPii
                        {
                            Email = safeEmail!,
                            FirstName = ResolveFirstName(userDto.FirstName),
                            LastName = ResolveLastName(userDto.LastName)
                        },
                        AuthProvider = provider,
                        AuthProviderId = providerUserId,
                        EmailVerified = userDto.EmailVerified ?? supportsEmailAutoMatch
                    };

                    var createdUser = await _userRepository.Create(newUser);

                    var actor = new Actor
                    {
                        ActorTypeId = (int)ActorTypeEnum.User,
                        ActorType = null!,
                        Pii = new ActorPii
                        {
                            DisplayName = BuildDisplayName(userDto.FirstName, userDto.LastName)
                        },
                        Description = null,
                        UserId = createdUser.Id
                    };

                    await _actorRepository.Create(actor);

                    await EnsureExternalLoginLinkInTransactionAsync(createdUser, provider, accountKey, loginId, ct);
                    return createdUser;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        user.Email = email;
                    }

                    user.FirstName = ResolveFirstName(userDto.FirstName);
                    user.LastName = ResolveLastName(userDto.LastName);
                    user.AuthProvider = provider;
                    user.AuthProviderId = providerUserId;
                    if (userDto.EmailVerified.HasValue)
                    {
                        user.EmailVerified = userDto.EmailVerified;
                    }

                    var actor = await _actorRepository.GetActorByUserId(user.Id);
                    if (actor != null)
                    {
                        actor.DisplayName = BuildDisplayName(userDto.FirstName, userDto.LastName);
                        await _actorRepository.Update(actor);
                    }

                    await _userRepository.Update(user);

                    await EnsureExternalLoginLinkInTransactionAsync(user, provider, accountKey, loginId, ct);
                    return user;
                }
            }, cancellationToken);

            await _cache.RemoveAsync($"user:detail:{syncedUser.Id}", cancellationToken);
            return BaseCommandResponse.Success(syncedUser.Id, "User synchronized successfully.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "User synchronization failed with exception type {ExceptionType}.",
                exception.GetType().FullName);
            const string message = "User synchronization failed.";
            return BaseCommandResponse.Validation<Guid>([message], message);
        }
    }

    private async Task EnsureExternalLoginLinkInTransactionAsync(
        User user,
        string provider,
        ProviderAccountKey accountKey,
        Guid loginId,
        CancellationToken ct)
    {
        var existingByProviderAndKey = await _userExternalLoginRepository.GetByProviderAndKey(provider, accountKey);
        if (existingByProviderAndKey != null)
        {
            if (existingByProviderAndKey.UserId != user.Id)
                throw new InvalidOperationException("This provider identity is already linked to another account.");

            return;
        }

        var login = new UserExternalLogin
        {
            Id = loginId,
            UserId = user.Id,
            User = user,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            Provider = provider,
            ProviderKey = accountKey.Value,
            ProviderDisplayName = GetProviderDisplayName(provider)
        };

        await _userExternalLoginRepository.Create(login);
    }

    private static string NormalizeProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return AuthSchemeNames.Keycloak.ToLowerInvariant();
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "keycloak" => "keycloak",
            "google" => "google",
            "atproto" => "atproto",
            _ => provider.Trim().ToLowerInvariant()
        };
    }


    private static bool SupportsEmailAutoMatch(string provider)
    {
        return provider is "keycloak" or "google";
    }

    private static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToLowerInvariant();
    }

    private static string ResolveEmailForCreation(string provider, string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        return provider == "atproto" ? string.Empty : email;
    }

    private static string ResolveFirstName(string? firstName)
    {
        return string.IsNullOrWhiteSpace(firstName) ? "User" : firstName.Trim();
    }

    private static string ResolveLastName(string? lastName)
    {
        return string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();
    }

    private static string BuildDisplayName(string? firstName, string? lastName)
    {
        return $"{ResolveFirstName(firstName)} {ResolveLastName(lastName)}".Trim();
    }

    private static string GetProviderDisplayName(string provider)
    {
        return provider switch
        {
            "keycloak" => "Keycloak",
            "google" => "Google",
            "atproto" => "AT Protocol",
            _ => provider
        };
    }

}
