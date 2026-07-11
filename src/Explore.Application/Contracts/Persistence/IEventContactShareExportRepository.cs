// ABOUTME: Repository interface for EventContactShareExport entities.
// ABOUTME: Used for recording export audit entries when organisation members download shared contact data.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventContactShareExportRepository : IGenericRepository<EventContactShareExport, Guid>
{
}
