// ABOUTME: Repository contract for ScheduleItemKind lookup table.
// ABOUTME: Provides lookup access for agenda item kind options (Break, Ceremony, Keynote, etc.).

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IScheduleItemKindRepository : IGenericRepository<ScheduleItemKind, int>
{
}
