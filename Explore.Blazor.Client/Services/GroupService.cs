// ABOUTME: Service for group creation, membership administration, and settings management in Blazor.
// ABOUTME: Uses generated API client where possible and JSON parsing for Group detail HAL payloads.

using System.Net.Http.Json;
using System.Text.Json;
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
    private readonly HttpClient _httpClient;
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<GroupService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GroupService(HttpClient httpClient, IEventApiClient apiClient, ILogger<GroupService> logger)
    {
        _httpClient = httpClient;
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
            var response = await _httpClient.GetAsync("api/Group/my?pageNumber=1&pageSize=100");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GroupService.GetMyGroupsAsync] API error fetching groups. StatusCode: {StatusCode}", response.StatusCode);
                return new List<GroupPublisherListDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<GroupPublisherListDto>();
            }

            using var doc = JsonDocument.Parse(content);
            if (TryReadHalEmbeddedItems(doc.RootElement, out var items))
            {
                return items;
            }

            if (doc.RootElement.TryGetProperty("items", out var itemsProperty) && itemsProperty.ValueKind == JsonValueKind.Array)
            {
                var rawItems = JsonSerializer.Deserialize<List<GroupPublisherListDto>>(itemsProperty.GetRawText(), JsonOptions);
                return rawItems ?? new List<GroupPublisherListDto>();
            }

            return new List<GroupPublisherListDto>();
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
            var response = await _httpClient.GetAsync($"api/Group/{groupId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GroupService.GetGroupDetailsAsync] API error fetching group. GroupId: {GroupId}, StatusCode: {StatusCode}", groupId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            return new GroupAdminDetailsModel
            {
                Id = ReadGuid(root, "id") ?? groupId,
                FullName = ReadString(root, "fullName") ?? string.Empty,
                Description = ReadString(root, "description"),
                ActorId = ReadGuid(root, "actorId"),
                ActorBackgroundColor = ReadString(root, "actorBackgroundColor"),
                ActorBackgroundEffect = ReadString(root, "actorBackgroundEffect"),
                ActorBannerColor = ReadString(root, "actorBannerColor"),
                ActorBannerPictureUri = ReadString(root, "actorBannerPictureUri"),
                ActorProfilePictureUri = ReadString(root, "actorProfilePictureUri")
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

    private static bool TryReadHalEmbeddedItems(JsonElement root, out List<GroupPublisherListDto> items)
    {
        items = new List<GroupPublisherListDto>();

        if (!root.TryGetProperty("_embedded", out var embedded) || embedded.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in embedded.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in property.Value.EnumerateArray())
            {
                var parsed = JsonSerializer.Deserialize<GroupPublisherListDto>(item.GetRawText(), JsonOptions);
                if (parsed != null)
                {
                    items.Add(parsed);
                }
            }

            return true;
        }

        return false;
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Null => null,
            _ => property.ToString()
        };
    }

    private static Guid? ReadGuid(JsonElement root, string propertyName)
    {
        var raw = ReadString(root, propertyName);
        return Guid.TryParse(raw, out var value) ? value : null;
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
}
