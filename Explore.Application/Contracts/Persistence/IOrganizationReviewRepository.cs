using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IOrganizationReviewRepository : IGenericRepository<OrganizationReview, Guid>
    {
        Task<List<OrganizationReview>> GetByOrganizationId(Guid organizationId);
        Task<bool> HasUserReviewedProgram(Guid userId, Guid programId);
    }
}
