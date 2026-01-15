using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventRegistrationRepository : IGenericRepository<EventRegistration, Guid>
    {
        Task<EventRegistration?> GetRegistrationByUserAndSession(Guid userId, Guid eventSessionId);
        Task<List<EventRegistration>> GetRegistrationsBySession(Guid eventSessionId);
        Task<List<EventRegistration>> GetRegistrationsByUser(Guid userId);
        Task<bool> IsUserRegisteredForSession(Guid userId, Guid eventSessionId);
        Task<(List<EventRegistration> Items, int TotalCount)> GetRegistrationsWithDetailsPaged(int pageNumber, int pageSize);
    }
}
