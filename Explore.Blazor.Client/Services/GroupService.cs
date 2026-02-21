// ABOUTME: Service for loading group publishing options for the current authenticated user.
// ABOUTME: Uses direct HTTP access to support Group endpoints before client regeneration.

using System.Net.Http.Json;
using System.Text.Json;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services;

public interface IGroupService
{
    Task<ICollection<GroupPublisherListDto>> GetMyGroupsAsync();
    Task<bool> CreateGroupAsync(string fullName, string? description = null);
}

public class GroupService : IGroupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroupService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GroupService(HttpClient httpClient, ILogger<GroupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> CreateGroupAsync(string fullName, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            _logger.LogWarning("[GroupService.CreateGroupAsync] Group name is required.");
            return false;
        }

        var request = new CreateGroupRequest
        {
            FullName = fullName.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/group", request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GroupService.CreateGroupAsync] API error creating group. StatusCode: {StatusCode}", response.StatusCode);
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return true;
            }

            var commandResponse = JsonSerializer.Deserialize<BaseCommandResponse<Guid>>(content, JsonOptions);
            return commandResponse?.Success ?? true;
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

    private sealed class CreateGroupRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

public class GroupPublisherListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;

    public int? CurrentUserRole { get; set; }
}
