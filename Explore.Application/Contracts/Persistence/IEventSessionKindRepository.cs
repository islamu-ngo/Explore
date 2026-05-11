// ABOUTME: Repository contract for EventSessionKind lookup table.
// ABOUTME: Provides lookup access for program item/session kind options.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionKindRepository : IGenericRepository<EventSessionKind, int>
{
}
