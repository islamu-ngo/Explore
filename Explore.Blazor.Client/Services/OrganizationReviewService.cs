using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services
{
    public class OrganizationReviewService : IOrganizationReviewService
    {
        private readonly HttpClient _httpClient;

        public OrganizationReviewService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<OrganizationReviewDto>> GetReviewsByOrganizationId(Guid organizationId)
        {
            var response = await _httpClient.GetFromJsonAsync<List<OrganizationReviewDto>>($"/bff/api/OrganizationReview/{organizationId}");
            return response ?? new List<OrganizationReviewDto>();
        }

        public async Task<BaseCommandResponse<Guid>> CreateReview(CreateOrganizationReviewDto review)
        {
            var response = await _httpClient.PostAsJsonAsync("/bff/api/OrganizationReview", review);
            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        }
    }
}
