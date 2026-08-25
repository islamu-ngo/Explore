// ABOUTME: Handles user synchronization from external identity providers (Keycloak, Google, ATProto).
// ABOUTME: Uses IUnitOfWork to atomically create/update user, actor, and external login records.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.User;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;

namespace Explore.Application.Features.Users.Handlers.Commands;

public class SyncUserCommandHandler : IRequestHandler<SyncUserCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IActorRepository _actorRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public SyncUserCommandHandler(
        IUserRepository userRepository,
        IUserExternalLoginRepository userExternalLoginRepository,
        IActorRepository actorRepository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IConfiguration configuration,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userExternalLoginRepository = userExternalLoginRepository;
        _actorRepository = actorRepository;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _configuration = configuration;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var userDto = request.UserDto;

        try
        {
            var provider = NormalizeProvider(userDto.AuthProvider);
            var providerUserId = ResolveProviderUserId(userDto);
            var supportsEmailAutoMatch = SupportsEmailAutoMatch(provider);
            var email = NormalizeEmail(userDto.Email);

            if (string.IsNullOrWhiteSpace(providerUserId))
            {
                return BaseCommandResponse.Validation<Guid>(
                    ["Provider user id is required to synchronize the user."],
                    "Provider user id is required to synchronize the user.");
            }

            if (!supportsEmailAutoMatch && string.IsNullOrWhiteSpace(email))
            {
                var existingProviderLogin = await _userExternalLoginRepository.GetByProviderAndKey(provider, providerUserId);
                if (existingProviderLogin == null)
                {
                    const string message =
                        "AT Protocol identity must be explicitly linked to an existing account before sign-in sync without email.";
                    return BaseCommandResponse.Validation<Guid>([message], message);
                }
            }

            // Pre-reads for user resolution — outside transaction for fast rejection
            User? user = null;

            var existingLogin = await _userExternalLoginRepository.GetByProviderAndKey(provider, providerUserId);
            if (existingLogin != null)
            {
                user = await _userRepository.GetById(existingLogin.UserId);
            }

            if (user == null && userDto.Id != Guid.Empty)
            {
                user = await _userRepository.GetById(userDto.Id);
            }

            if (user == null && supportsEmailAutoMatch && userDto.EmailVerified == true && !string.IsNullOrWhiteSpace(email))
            {
                user = await _userRepository.GetUserByEmail(email);
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

                    await EnsureExternalLoginLinkInTransactionAsync(createdUser, provider, providerUserId, loginId, ct);
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

                    await EnsureExternalLoginLinkInTransactionAsync(user, provider, providerUserId, loginId, ct);
                    return user;
                }
            }, cancellationToken);

            await _cache.RemoveAsync($"user:detail:{syncedUser.Id}", cancellationToken);
            return BaseCommandResponse.Success(syncedUser.Id, "User synchronized successfully.");
        }
        catch (Exception ex)
        {
            string message = $"Error syncing user: {ex.Message}";
            return BaseCommandResponse.Validation<Guid>([message], message);
        }
    }

    private async Task EnsureExternalLoginLinkInTransactionAsync(
        User user,
        string provider,
        string providerUserId,
        Guid loginId,
        CancellationToken ct)
    {
        var existingByProviderAndKey = await _userExternalLoginRepository.GetByProviderAndKey(provider, providerUserId);
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
            ProviderKey = providerUserId,
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

    private static string ResolveProviderUserId(UserDto userDto)
    {
        if (!string.IsNullOrWhiteSpace(userDto.AuthProviderId))
        {
            return userDto.AuthProviderId.Trim();
        }

        return userDto.Id == Guid.Empty ? string.Empty : userDto.Id.ToString();
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
