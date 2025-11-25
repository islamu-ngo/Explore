using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs.OrganizationMember;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services
{
    public class OrganizationMemberService : IOrganizationMemberService
    {
        private readonly HttpClient _httpClient;

        public OrganizationMemberService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<OrganizationMemberDto>> GetMembersAsync(Guid organizationId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<OrganizationMemberDto>>($"/bff/api/OrganizationMember/{organizationId}");
                return response ?? new List<OrganizationMemberDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching organization members: {ex.Message}");
                return new List<OrganizationMemberDto>();
            }
        }

        public async Task<OrganizationMemberDto> InviteMemberAsync(AddOrganizationMemberDto member)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/bff/api/OrganizationMember", member);
                
                if (response.IsSuccessStatusCode)
                {
                    var commandResponse = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
                    
                    if (commandResponse != null && commandResponse.Success)
                    {
                        // Return a placeholder DTO since the API only returns the ID
                        // In a real app, we might want to fetch the created member or construct it
                        return new OrganizationMemberDto
                        {
                            Id = commandResponse.Id,
                            OrganizationId = member.OrganizationId,
                            Email = member.Email,
                            Role = member.Role,
                            // Other fields will be empty until refreshed
                        };
                    }
                    else
                    {
                        var errors = commandResponse?.Errors != null 
                            ? string.Join(", ", commandResponse.Errors) 
                            : commandResponse?.Message ?? "Unknown error";
                        throw new Exception(errors);
                    }
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inviting member: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateMemberRoleAsync(UpdateOrganizationMemberRoleDto updateDto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("/bff/api/OrganizationMember/role", updateDto);
                
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating member role: {ex.Message}");
                throw;
            }
        }

        public async Task<List<OrganizationInvitationDto>> GetMyInvitationsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<OrganizationInvitationDto>>("/bff/api/OrganizationMember/invitations");
                return response ?? new List<OrganizationInvitationDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching invitations: {ex.Message}");
                return new List<OrganizationInvitationDto>();
            }
        }

        public async Task AcceptInvitationAsync(Guid invitationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/bff/api/OrganizationMember/invitations/{invitationId}/accept", null);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting invitation: {ex.Message}");
                throw;
            }
        }

        public async Task DeclineInvitationAsync(Guid invitationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/bff/api/OrganizationMember/invitations/{invitationId}/decline", null);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error declining invitation: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteMemberAsync(Guid memberId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/bff/api/OrganizationMember/{memberId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting member: {ex.Message}");
                throw;
            }
        }
    }
}
