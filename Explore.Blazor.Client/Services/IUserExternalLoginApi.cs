// ABOUTME: Refit interface for current-user external login management endpoints.
// ABOUTME: Supports account-security provider linking without the raw BFF HttpClient wrapper.

using Refit;

namespace Explore.Blazor.Client.Services;

public interface IUserExternalLoginApi
{
    [Get("/api/userexternallogin")]
    Task<IApiResponse<List<UserExternalLoginListItem>>> GetAsync(CancellationToken cancellationToken);

    [Post("/api/userexternallogin")]
    Task<IApiResponse<UserExternalLoginCommandResponse>> CreateAsync(
        [Body] CreateUserExternalLoginRequest request,
        CancellationToken cancellationToken);
}

public sealed class UserExternalLoginListItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
}

public sealed class CreateUserExternalLoginRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
}

public sealed class UserExternalLoginCommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
