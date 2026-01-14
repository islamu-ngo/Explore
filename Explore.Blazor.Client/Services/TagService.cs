using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface ITagService
{
    Task<ICollection<TagListDto>> GetAllTagsAsync();
    Task<TagDto?> GetTagByIdAsync(Guid tagId);
    Task<BaseCommandResponseOfGuid?> CreateTagAsync(CreateTagDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateTagAsync(Guid id, UpdateTagDto dto);
    Task<bool> DeleteTagAsync(Guid tagId);
    Task<ICollection<TagListDto>> GetTagsByEventAsync(Guid eventId);
    Task<ICollection<EventListDto>> GetEventsByTagAsync(Guid tagId);
    Task<BaseCommandResponseOfGuid?> AssignTagToEventAsync(CreateEventTagsDto dto);
    Task<bool> RemoveTagFromEventAsync(Guid eventTagId);
}

public class TagService : ITagService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<TagService> _logger;

    public TagService(IEventApiClient apiClient, ILogger<TagService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<TagListDto>> GetAllTagsAsync()
    {
        try
        {
            _logger.LogInformation("[TAG SERVICE] Fetching all tags...");
            var response = await _apiClient.TagAllAsync();
            _logger.LogInformation("[TAG SERVICE] Received {Count} tags", response?.Count ?? 0);
            return response ?? new List<TagListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error fetching tags: {StatusCode}", ex.StatusCode);
            return new List<TagListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error fetching tags");
            return new List<TagListDto>();
        }
    }

    public async Task<TagDto?> GetTagByIdAsync(Guid tagId)
    {
        try
        {
            return await _apiClient.TagGETAsync(tagId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[TAG SERVICE] Tag not found: {TagId}", tagId);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error fetching tag {TagId}: {StatusCode}", tagId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error fetching tag {TagId}", tagId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateTagAsync(CreateTagDto dto)
    {
        try
        {
            _logger.LogInformation("[TAG SERVICE] Creating tag: {Name}", dto.FullName);
            return await _apiClient.TagPOSTAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error creating tag: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error creating tag");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateTagAsync(Guid id, UpdateTagDto dto)
    {
        try
        {
            return await _apiClient.TagPUTAsync(id, dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error updating tag: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error updating tag");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> DeleteTagAsync(Guid tagId)
    {
        try
        {
            await _apiClient.TagDELETEAsync(tagId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error deleting tag: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error deleting tag");
            return false;
        }
    }

    public async Task<ICollection<TagListDto>> GetTagsByEventAsync(Guid eventId)
    {
        try
        {
            var response = await _apiClient.ByEvent3Async(eventId);
            return response ?? new List<TagListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error fetching event tags: {StatusCode}", ex.StatusCode);
            return new List<TagListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error fetching event tags");
            return new List<TagListDto>();
        }
    }

    public async Task<ICollection<EventListDto>> GetEventsByTagAsync(Guid tagId)
    {
        try
        {
            var response = await _apiClient.ByTagAsync(tagId);
            return response ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error fetching events by tag: {StatusCode}", ex.StatusCode);
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error fetching events by tag");
            return new List<EventListDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> AssignTagToEventAsync(CreateEventTagsDto dto)
    {
        try
        {
            return await _apiClient.EventTagsPOSTAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error assigning tag to event: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error assigning tag to event");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> RemoveTagFromEventAsync(Guid eventTagId)
    {
        try
        {
            await _apiClient.EventTagsDELETEAsync(eventTagId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error removing tag from event: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] Error removing tag from event");
            return false;
        }
    }
}
