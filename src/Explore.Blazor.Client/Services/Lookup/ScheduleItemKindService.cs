// ABOUTME: Lookup service for ScheduleItemKind read-only data.
// ABOUTME: Thin wrapper around NSwag-generated client method.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class ScheduleItemKindService : IScheduleItemKindService
{
    private readonly IEventApiClient _client;

    public ScheduleItemKindService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<ScheduleItemKindListDto>> GetScheduleItemKindsAsync()
    {
        return await _client.GetScheduleItemKindsAsync();
    }
}
