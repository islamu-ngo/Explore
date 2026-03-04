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

    public SyncUserCommandHandler(
        IUserRepository userRepository,
        IUserExternalLoginRepository userExternalLoginRepository,
        IActorRepository actorRepository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IConfiguration configuration,
        HybridCache cache)
    {
        _userRepository = userRepository;
        _userExternalLoginRepository = userExternalLoginRepository;
        _actorRepository = actorRepository;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var userDto = request.UserDto;

        try
        {
            var provider = NormalizeProvider(userDto.AuthProvider);
            var providerUserId = ResolveProviderUserId(userDto);
            var supportsEmailAutoMatch = SupportsEmailAutoMatch(provider);
            var email = NormalizeEmail(userDto.Email);

            if (string.IsNullOrWhiteSpace(providerUserId))
            {
                response.Success = false;
                response.Message = "Provider user id is required to synchronize the user.";
                return response;
            }

            if (!supportsEmailAutoMatch && string.IsNullOrWhiteSpace(email))
            {
                var existingProviderLogin = await _userExternalLoginRepository.GetByProviderAndKey(provider, providerUserId);
                if (existingProviderLogin == null)
                {
                    response.Success = false;
                    response.Message =
                        "AT Protocol identity must be explicitly linked to an existing account before sign-in sync without email.";
                    return response;
                }
            }

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

            if (user == null)
            {
                var newUserId = userDto.Id != Guid.Empty ? userDto.Id : Guid.NewGuid();
                var safeEmail = ResolveEmailForCreation(provider, email);
                if (string.IsNullOrWhiteSpace(safeEmail))
                {
                    response.Success = false;
                    response.Message = "Email is required to create a new account for this provider.";
                    return response;
                }

                user = new User
                {
                    Id = newUserId,
                    Pii = new UserPii
                    {
                        Email = safeEmail,
                        FirstName = ResolveFirstName(userDto.FirstName),
                        LastName = ResolveLastName(userDto.LastName)
                    },
                    ActorId = null,
                    AuthProvider = provider,
                    AuthProviderId = providerUserId,
                    EmailVerified = userDto.EmailVerified ?? supportsEmailAutoMatch,
                    DefaultActorId = null
                };

                user = await _userRepository.Create(user);

                var defaultTenantId = await GetDefaultTenantIdAsync();
                var actor = new Actor
                {
                    ActorTypeId = (int)ActorTypeEnum.User,
                    ActorType = null!,
                    TenantId = defaultTenantId,
                    Tenant = null!,
                    Pii = new ActorPii
                    {
                        DisplayName = BuildDisplayName(userDto.FirstName, userDto.LastName),
                        Handle = GenerateHandle(userDto.Username, safeEmail, providerUserId),
                        Did = provider == AuthSchemeNames.Atproto.ToLowerInvariant() ? providerUserId : null
                    },
                    Description = null,
                    UserId = user.Id,
                    OrganizationId = null,
                    DidCustodyTypeId = provider == AuthSchemeNames.Atproto.ToLowerInvariant()
                        ? (int)DidCustodyTypeEnum.SelfCustody
                        : (int)DidCustodyTypeEnum.Custodial
                };

                actor = await _actorRepository.Create(actor);
                user.ActorId = actor.Id;
                user.DefaultActorId = actor.Id;
                await _userRepository.Update(user);
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

                if (user.ActorId.HasValue)
                {
                    var actor = await _actorRepository.GetById(user.ActorId.Value);
                    if (actor != null)
                    {
                        actor.DisplayName = BuildDisplayName(userDto.FirstName, userDto.LastName);
                        if (provider == AuthSchemeNames.Atproto.ToLowerInvariant() && string.IsNullOrWhiteSpace(actor.Did))
                        {
                            actor.Did = providerUserId;
                            actor.DidCustodyTypeId = (int)DidCustodyTypeEnum.SelfCustody;
                        }

                        await _actorRepository.Update(actor);
                    }
                }

                await _userRepository.Update(user);
            }

            var linkResult = await EnsureExternalLoginLinkAsync(user, provider, providerUserId);
            if (!linkResult.Success)
            {
                response.Success = false;
                response.Message = linkResult.Message;
                return response;
            }

            response.Success = true;
            response.Message = "User synchronized successfully.";
            response.Id = user.Id;
            await _cache.RemoveAsync($"user:detail:{user.Id}", cancellationToken);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error syncing user: {ex.Message}";
        }

        return response;
    }

    private async Task<(bool Success, string Message)> EnsureExternalLoginLinkAsync(
        User user,
        string provider,
        string providerUserId)
    {
        var existingByProviderAndKey = await _userExternalLoginRepository.GetByProviderAndKey(provider, providerUserId);
        if (existingByProviderAndKey != null)
        {
            if (existingByProviderAndKey.UserId != user.Id)
            {
                return (false, "This provider identity is already linked to another account.");
            }

            return (true, string.Empty);
        }

        var login = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            Provider = provider,
            ProviderKey = providerUserId,
            ProviderDisplayName = GetProviderDisplayName(provider)
        };

        await _userExternalLoginRepository.Create(login);
        return (true, string.Empty);
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

    private async Task<Guid> GetDefaultTenantIdAsync()
    {
        // Try to get from configuration first
        var configuredTenantId = _configuration["DefaultTenantId"];
        if (!string.IsNullOrEmpty(configuredTenantId) && Guid.TryParse(configuredTenantId, out var tenantId))
        {
            return tenantId;
        }

        // Fallback: get the first active tenant
        var tenants = await _tenantRepository.GetAll();
        var defaultTenant = tenants.FirstOrDefault(t => t.IsActive);

        if (defaultTenant == null)
        {
            throw new InvalidOperationException("No active tenant found in the system.");
        }

        return defaultTenant.Id;
    }

    private static string GenerateHandle(string? username, string email, string providerUserId)
    {
        // Use username if available, otherwise use email prefix
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username.ToLowerInvariant().Replace(" ", "-");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return providerUserId.Replace(":", "-").Replace(".", "-").ToLowerInvariant();
        }

        var emailPrefix = email.Split('@')[0];
        return emailPrefix.ToLowerInvariant().Replace(".", "-").Replace(" ", "-");
    }
}
