// ABOUTME: Refit interface for group BFF endpoints used by Blazor services.
// ABOUTME: Keeps current-user group reads and detail payloads behind the secure BFF handler pipeline.

using Explore.Blazor.Client.Clients;
using Refit;

namespace Explore.Blazor.Client.Services;

public interface IGroupApi
{
    [Get("/api/Group/my")]
    Task<IApiResponse<HalCollectionResourceOfGroupListDto>> GetMyGroupsAsync(
        [AliasAs("pageNumber")] int pageNumber,
        [AliasAs("pageSize")] int pageSize,
        CancellationToken cancellationToken);

    [Get("/api/Group/{groupId}")]
    Task<IApiResponse<HalResourceOfGroupDto>> GetGroupDetailsAsync(
        Guid groupId,
        CancellationToken cancellationToken);
}
