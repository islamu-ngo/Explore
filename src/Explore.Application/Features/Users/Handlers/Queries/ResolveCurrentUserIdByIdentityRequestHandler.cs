// ABOUTME: Maps one canonical provider account key to its linked internal user id.
// ABOUTME: Rejects email and raw-subject fallback so identity resolution remains authority-qualified.
using Explore.Application.Authentication;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Users.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Users.Handlers.Queries;

public class ResolveCurrentUserIdByIdentityRequestHandler
    : IRequestHandler<ResolveCurrentUserIdByIdentityRequest, Guid?>
{
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;

    public ResolveCurrentUserIdByIdentityRequestHandler(
        IUserExternalLoginRepository userExternalLoginRepository)
    {
        _userExternalLoginRepository = userExternalLoginRepository;
    }

    public async Task<Guid?> Handle(ResolveCurrentUserIdByIdentityRequest request, CancellationToken cancellationToken)
    {
        var normalizedProvider = request.Provider.Trim().ToLowerInvariant();
        var providerKind = normalizedProvider == "atproto"
            ? Explore.Domain.Enums.InstanceBootstrapProviderKind.Atproto
            : Explore.Domain.Enums.InstanceBootstrapProviderKind.Keycloak;
        var accountKey = new ProviderAccountKey(providerKind, request.ProviderId.Trim());

        var externalLogin = await _userExternalLoginRepository.GetByProviderAndKey(
            normalizedProvider,
            accountKey);
        return externalLogin?.UserId;
    }
}
