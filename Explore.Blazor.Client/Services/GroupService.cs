// ABOUTME: Service for group creation, membership administration, and settings management in Blazor.
// ABOUTME: Uses Refit BFF reads and generated API client commands/member operations.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Services;

public interface IGroupService
{
    Task<ICollection<GroupPublisherListDto>> GetMyGroupsAsync();
    Task<bool> CreateGroupAsync(string fullName, string? description = null);
    Task<GroupAdminDetailsModel?> GetGroupDetailsAsync(Guid groupId);
    Task<BaseCommandResponseOfGuid?> UpdateGroupAsync(Guid groupId, UpdateGroupDto group);
    Task<ICollection<GroupMemberDto>> GetGroupMembersAsync(Guid groupId);
    Task<GroupMembersResult> GetGroupMembersWithAffordancesAsync(Guid groupId);
    Task<BaseCommandResponseOfGuid?> AddGroupMemberAsync(AddGroupMemberDto member);
    Task<BaseCommandResponseOfGuid?> UpdateGroupMemberRoleAsync(UpdateGroupMemberRoleDto updateDto);
    Task<BaseCommandResponseOfGuid?> DeleteGroupMemberAsync(Guid memberId);
}

public sealed record GroupMembersResult(ICollection<GroupMemberDto> Members, bool CanCreate)
{
    public static GroupMembersResult Empty { get; } = new([], false);
}

public class GroupService : IGroupService
{
    private readonly IGroupApi _groupApi;
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<GroupService> _logger;

    public GroupService(IGroupApi groupApi, IEventApiClient apiClient, ILogger<GroupService> logger)
    {
        _groupApi = groupApi;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<bool> CreateGroupAsync(string fullName, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            _logger.LogWarning("[GroupService.CreateGroupAsync] Group name is required.");
            return false;
        }

        try
        {
            var response = await _apiClient.CreateGroupAsync(new CreateGroupDto
            {
                FullName = fullName.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
            });

            return response.Success == true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[GroupService.CreateGroupAsync] API error creating group. StatusCode: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GroupService.CreateGroupAsync] Unexpected error creating group");
            return false;
        }
    }

    public async Task<ICollection<GroupPublisherListDto>> GetMyGroupsAsync()
    {
        try
        {
            var response = await _groupApi.GetMyGroupsAsync(1, 100, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GroupService.GetMyGroupsAsync] API error fetching groups. StatusCode: {StatusCode}", response.StatusCode);
                return new List<GroupPublisherListDto>();
            }

            return response.Content?._embedded?.Items?
                .Select(MapGroupPublisher)
                .Where(group => group is not null)
                .Select(group => group!)
                .ToList() ?? new List<GroupPublisherListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GroupService.GetMyGroupsAsync] Unexpected error fetching groups");
            return new List<GroupPublisherListDto>();
        }
    }

    public async Task<GroupAdminDetailsModel?> GetGroupDetailsAsync(Guid groupId)
    {
        try
        {
            var response = await _groupApi.GetGroupDetailsAsync(groupId, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GroupService.GetGroupDetailsAsync] API error fetching group. GroupId: {GroupId}, StatusCode: {StatusCode}", groupId, response.StatusCode);
                return null;
            }

            var group = response.Content;
            if (group is null)
            {
                return null;
            }

            return new GroupAdminDetailsModel
            {
                Id = group.Id ?? groupId,
                FullName = group.FullName ?? string.Empty,
                Description = group.Description,
                ActorId = group.ActorId,
                ActorBackgroundColor = group.ActorBackgroundColor,
                ActorBackgroundEffect = group.ActorBackgroundEffect,
                ActorBannerColor = group.ActorBannerColor,
                ActorBannerPictureUri = group.ActorBannerPictureUri,
                ActorProfilePictureUri = group.ActorProfilePictureUri,
                LinkRelations = ReadLinkRelations(group)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GroupService.GetGroupDetailsAsync] Unexpected error fetching group. GroupId: {GroupId}", groupId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateGroupAsync(Guid groupId, UpdateGroupDto group)
    {
        try
        {
            return await _apiClient.UpdateGroupAsync(groupId, group);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[GroupService.UpdateGroupAsync] API error updating group. GroupId: {GroupId}, StatusCode: {StatusCode}", groupId, ex.StatusCode);
            throw;
        }
    }

    public async Task<ICollection<GroupMemberDto>> GetGroupMembersAsync(Guid groupId)
    {
        var result = await GetGroupMembersWithAffordancesAsync(groupId);
        return result.Members;
    }

    public async Task<GroupMembersResult> GetGroupMembersWithAffordancesAsync(Guid groupId)
    {
        try
        {
            var response = await _apiClient.GetGroupMembersAsync(groupId);
            if (response is null)
            {
                return GroupMembersResult.Empty;
            }

            return new GroupMembersResult(
                response.GetItems().ToList(),
                response.HasLink("create"));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[GroupService.GetGroupMembersAsync] API error fetching group members. GroupId: {GroupId}, StatusCode: {StatusCode}", groupId, ex.StatusCode);
            return GroupMembersResult.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GroupService.GetGroupMembersAsync] Unexpected error fetching group members. GroupId: {GroupId}", groupId);
            return GroupMembersResult.Empty;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> AddGroupMemberAsync(AddGroupMemberDto member)
    {
        try
        {
            return await _apiClient.CreateGroupMemberAsync(member);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[GroupService.AddGroupMemberAsync] API error adding group member. StatusCode: {StatusCode}", ex.StatusCode);
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateGroupMemberRoleAsync(UpdateGroupMemberRoleDto updateDto)
    {
        try
        {
            return await _apiClient.UpdateGroupMemberAsync(updateDto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[GroupService.UpdateGroupMemberRoleAsync] API error updating group member role. StatusCode: {StatusCode}", ex.StatusCode);
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> DeleteGroupMemberAsync(Guid memberId)
    {
        try
        {
            return await _apiClient.DeleteGroupMemberAsync(memberId);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[GroupService.DeleteGroupMemberAsync] API error deleting group member. MemberId: {MemberId}, StatusCode: {StatusCode}", memberId, ex.StatusCode);
            throw;
        }
    }

    private static GroupPublisherListDto? MapGroupPublisher(HalResourceOfGroupListDto group)
    {
        if (group.Id is null)
        {
            return null;
        }

        return new GroupPublisherListDto
        {
            Id = group.Id.Value,
            FullName = group.FullName ?? string.Empty,
            CurrentUserRole = group.CurrentUserRole
        };
    }

    private static IReadOnlySet<string> ReadLinkRelations(HalResourceOfGroupDto group)
    {
        if (group._links is null || group._links.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return group._links.Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

public class GroupPublisherListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? CurrentUserRole { get; set; }
}

public class GroupAdminDetailsModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorBackgroundColor { get; set; }
    public string? ActorBackgroundEffect { get; set; }
    public string? ActorBannerColor { get; set; }
    public string? ActorBannerPictureUri { get; set; }
    public string? ActorProfilePictureUri { get; set; }
    public IReadOnlySet<string> LinkRelations { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasHalLink(string relation) => LinkRelations.Contains(relation);
}
