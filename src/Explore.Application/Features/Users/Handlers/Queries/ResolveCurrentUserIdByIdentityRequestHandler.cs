// ABOUTME: Maps provider identity to internal user id using external-login links and verified email fallback.
// ABOUTME: Enables non-GUID provider subjects (e.g., Google, ATProto DID) to resolve local user records.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Users.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Users.Handlers.Queries;

public class ResolveCurrentUserIdByIdentityRequestHandler
    : IRequestHandler<ResolveCurrentUserIdByIdentityRequest, Guid?>
{
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IUserRepository _userRepository;

    public ResolveCurrentUserIdByIdentityRequestHandler(
        IUserExternalLoginRepository userExternalLoginRepository,
        IUserRepository userRepository)
    {
        _userExternalLoginRepository = userExternalLoginRepository;
        _userRepository = userRepository;
    }

    public async Task<Guid?> Handle(ResolveCurrentUserIdByIdentityRequest request, CancellationToken cancellationToken)
    {
        var normalizedProvider = request.Provider.Trim().ToLowerInvariant();
        var providerId = request.ProviderId.Trim();

        var externalLogin = await _userExternalLoginRepository.GetByProviderAndKey(normalizedProvider, providerId);
        if (externalLogin != null)
        {
            return externalLogin.UserId;
        }

        if (request.EmailVerified &&
            !string.IsNullOrWhiteSpace(request.Email) &&
            (normalizedProvider == "keycloak" || normalizedProvider == "google"))
        {
            var user = await _userRepository.GetUserByEmail(request.Email.Trim().ToLowerInvariant());
            return user?.Id;
        }

        return null;
    }
}
