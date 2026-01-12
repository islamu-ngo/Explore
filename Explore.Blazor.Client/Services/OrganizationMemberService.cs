using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IOrganizationMemberService
{
    Task<ICollection<OrganizationMemberDto>> GetMembersAsync(Guid organizationId);
    Task<BaseCommandResponseOfGuid?> InviteMemberAsync(AddOrganizationMemberDto member);
    Task<BaseCommandResponseOfGuid?> UpdateMemberRoleAsync(UpdateOrganizationMemberRoleDto updateDto);
    Task<ICollection<OrganizationInvitationDto>> GetMyInvitationsAsync();
    Task<BaseCommandResponseOfGuid?> AcceptInvitationAsync(Guid invitationId);
    Task<BaseCommandResponseOfGuid?> DeclineInvitationAsync(Guid invitationId);
    Task<BaseCommandResponseOfGuid?> DeleteMemberAsync(Guid memberId);
}

public class OrganizationMemberService : IOrganizationMemberService
{
    private readonly IEventApiClient _apiClient;

    public OrganizationMemberService(IEventApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ICollection<OrganizationMemberDto>> GetMembersAsync(Guid organizationId)
    {
        try
        {
            var response = await _apiClient.OrganizationMemberAllAsync(organizationId);
            return response ?? new List<OrganizationMemberDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error fetching organization members: {ex.StatusCode} - {ex.Message}");
            return new List<OrganizationMemberDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching organization members: {ex.Message}");
            return new List<OrganizationMemberDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> InviteMemberAsync(AddOrganizationMemberDto member)
    {
        try
        {
            return await _apiClient.OrganizationMemberPOSTAsync(member);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error inviting member: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inviting member: {ex.Message}");
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateMemberRoleAsync(UpdateOrganizationMemberRoleDto updateDto)
    {
        try
        {
            return await _apiClient.RoleAsync(updateDto);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error updating member role: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating member role: {ex.Message}");
            throw;
        }
    }

    public async Task<ICollection<OrganizationInvitationDto>> GetMyInvitationsAsync()
    {
        try
        {
            var response = await _apiClient.InvitationsAsync();
            return response ?? new List<OrganizationInvitationDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error fetching invitations: {ex.StatusCode} - {ex.Message}");
            return new List<OrganizationInvitationDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching invitations: {ex.Message}");
            return new List<OrganizationInvitationDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> AcceptInvitationAsync(Guid invitationId)
    {
        try
        {
            return await _apiClient.AcceptAsync(invitationId);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error accepting invitation: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting invitation: {ex.Message}");
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> DeclineInvitationAsync(Guid invitationId)
    {
        try
        {
            return await _apiClient.DeclineAsync(invitationId);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error declining invitation: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error declining invitation: {ex.Message}");
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> DeleteMemberAsync(Guid memberId)
    {
        try
        {
            return await _apiClient.OrganizationMemberDELETEAsync(memberId);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error deleting member: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting member: {ex.Message}");
            throw;
        }
    }
}
