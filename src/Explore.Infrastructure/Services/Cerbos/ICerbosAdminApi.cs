// ABOUTME: Refit contract for Cerbos Admin API policy, schema, and reload operations.
// ABOUTME: Keeps mutable policy-store calls centralized behind the infrastructure package publisher.

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
    Task<ApiResponse<string>> PushSchemasAsync(
        [Header("Authorization")] string authorization,
        [Body(buffered: true)] CerbosSchemaBatchRequest request,
        CancellationToken cancellationToken = default);

    [Post("/admin/policy")]
    Task<ApiResponse<string>> PushPoliciesAsync(
        [Header("Authorization")] string authorization,
        [Body(buffered: true)] CerbosPolicyBatchRequest request,
        CancellationToken cancellationToken = default);

    [Get("/admin/store/reload")]
    Task<ApiResponse<string>> ReloadInstanceAsync(
        [Header("Authorization")] string authorization,
        [AliasAs("wait")] string wait = "true",
        CancellationToken cancellationToken = default);
}
