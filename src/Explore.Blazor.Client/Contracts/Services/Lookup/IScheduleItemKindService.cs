// ABOUTME: Contract for ScheduleItemKind lookup operations (read-only).
// ABOUTME: Wraps the NSwag-generated schedule-item-kind lookup client.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IScheduleItemKindService
{
    Task<ICollection<ScheduleItemKindListDto>> GetScheduleItemKindsAsync();
}
