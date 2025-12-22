using Explore.Blazor.Client.Models.DTOs;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services
{
    public interface IOrganizationReviewService
    {
        Task<List<OrganizationReviewDto>> GetReviewsByOrganizationId(Guid organizationId);
        Task<List<OrganizationReviewDto>> GetReviewsByUserId(Guid userId);
        Task<BaseCommandResponse<Guid>> CreateReview(CreateOrganizationReviewDto review);
    }
}
