using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Refit interface for Cerbos Admin API endpoints.
/// Used for pushing policies and schemas, and reloading instances.
/// </summary>
internal interface ICerbosAdminApi
{
    [Post("/admin/schema")]
    Task<IApiResponse> PushSchemasAsync(
        [Header("Authorization")] string authorization,
        [Body(buffered: true)] CerbosSchemaBatchRequest request,
        CancellationToken cancellationToken = default);

    [Post("/admin/policy")]
    Task<IApiResponse> PushPoliciesAsync(
        [Header("Authorization")] string authorization,
        [Body(buffered: true)] CerbosPolicyBatchRequest request,
        CancellationToken cancellationToken = default);

    [Get("/admin/store/reload")]
    Task<IApiResponse> ReloadInstanceAsync(
        [Header("Authorization")] string authorization,
        [AliasAs("wait")] bool wait = true,
        CancellationToken cancellationToken = default);
}
