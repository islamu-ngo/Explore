// ABOUTME: Contract for ScheduleItemKind lookup operations (read-only).
// ABOUTME: Wraps the NSwag-generated IEventApiClient methods for ScheduleItemKind lookup.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IScheduleItemKindService
{
    Task<ICollection<ScheduleItemKindListDto>> GetScheduleItemKindsAsync();
}
