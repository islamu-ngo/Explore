// ABOUTME: EF Core repository for EventContactShareExport audit records.
// ABOUTME: Inherits GenericRepository; no custom queries needed beyond CRUD.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class EventContactShareExportRepository : GenericRepository<EventContactShareExport, Guid>, IEventContactShareExportRepository
{
    public EventContactShareExportRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
