// ABOUTME: EF implementation of IScheduleItemKindRepository for the ScheduleItemKind lookup table.
// ABOUTME: Delegates all operations to GenericRepository.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class ScheduleItemKindRepository : GenericRepository<ScheduleItemKind, int>, IScheduleItemKindRepository
{
    public ScheduleItemKindRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
