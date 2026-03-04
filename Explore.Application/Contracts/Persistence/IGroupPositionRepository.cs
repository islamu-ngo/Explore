// ABOUTME: Repository contract for GroupPosition lookup table.
// ABOUTME: Read-only lookup — inherits generic CRUD but only queries are used.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IGroupPositionRepository : IGenericRepository<GroupPosition, int>
{
}
