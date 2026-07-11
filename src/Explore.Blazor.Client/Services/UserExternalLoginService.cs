// ABOUTME: Client service for current-user external login management.
// ABOUTME: Wraps generated API operations and exposes only generated request and response models.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IUserExternalLoginService
{
    Task<ICollection<UserExternalLoginListDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<BaseCommandResponseOfGuid> CreateAsync(
        CreateUserExternalLoginDto request,
        CancellationToken cancellationToken = default);
}

public sealed class UserExternalLoginService(IEventApiClient apiClient) : IUserExternalLoginService
{
    public Task<ICollection<UserExternalLoginListDto>> GetAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetUserExternalLoginsAsync(cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> CreateAsync(
        CreateUserExternalLoginDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.CreateUserExternalLoginAsync(request, cancellationToken: cancellationToken);
}
