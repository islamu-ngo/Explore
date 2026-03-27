using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for managing organization member operations.
/// </summary>
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

/// <summary>
/// Implementation of organization member service.
/// </summary>
public class OrganizationMemberService : IOrganizationMemberService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<OrganizationMemberService> _logger;

    public OrganizationMemberService(IEventApiClient apiClient, ILogger<OrganizationMemberService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<OrganizationMemberDto>> GetMembersAsync(Guid organizationId)
    {
        try
        {
            var response = await _apiClient.OrganizationmemberAllAsync(organizationId);
            return response ?? new List<OrganizationMemberDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching organization members: {StatusCode}", ex.StatusCode);
            return new List<OrganizationMemberDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching organization members");
            return new List<OrganizationMemberDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> InviteMemberAsync(AddOrganizationMemberDto member)
    {
        try
        {
            return await _apiClient.OrganizationmemberPOSTAsync(member);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error inviting member: {StatusCode}", ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting member");
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateMemberRoleAsync(UpdateOrganizationMemberRoleDto updateDto)
    {
        try
        {
            return await _apiClient.RolePUT2Async(updateDto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error updating member role: {StatusCode}", ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member role");
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
            _logger.LogError(ex, "API error fetching invitations: {StatusCode}", ex.StatusCode);
            return new List<OrganizationInvitationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invitations");
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
            _logger.LogError(ex, "API error accepting invitation: {StatusCode}", ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation");
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
            _logger.LogError(ex, "API error declining invitation: {StatusCode}", ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining invitation");
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> DeleteMemberAsync(Guid memberId)
    {
        try
        {
            return await _apiClient.OrganizationmemberDELETEAsync(memberId);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error deleting member: {StatusCode}", ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting member");
            throw;
        }
    }
}

