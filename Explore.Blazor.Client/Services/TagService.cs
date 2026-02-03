// ABOUTME: Service for managing tag-related operations.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface ITagService
{
    Task<ICollection<TagListDto>> GetTagsAsync();
    Task<ICollection<TagListDto>> GetAllTagsAsync(); // Alias for GetTagsAsync
    Task<TagDto?> GetTagByIdAsync(Guid tagId);
    Task<BaseCommandResponseOfGuid?> CreateTagAsync(CreateTagDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateTagAsync(Guid id, UpdateTagDto dto);
    Task<bool> DeleteTagAsync(Guid tagId);
    Task<ICollection<object>> GetTagsByEventAsync(Guid eventId); // Neutralized
    Task<ICollection<object>> GetEventsByTagAsync(Guid tagId); // Neutralized
    Task<BaseCommandResponseOfGuid?> AssignTagToEventAsync(object dto); // Neutralized
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

    public async Task<ICollection<TagListDto>> GetTagsAsync()
    {
        try
        {
            var result = await _apiClient.GetTagsAsync(pageNumber: 1, pageSize: 100);
            return result?.GetItems() ?? new List<TagListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error fetching tags: {StatusCode}", ex.StatusCode);
            return new List<TagListDto>();
        }
    }

    public Task<ICollection<TagListDto>> GetAllTagsAsync() => GetTagsAsync();

    public async Task<TagDto?> GetTagByIdAsync(Guid tagId)
    {
        try
        {
            var result = await _apiClient.GetTagByIdAsync(tagId);
            return result?.ToDto();
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
    }

    public async Task<BaseCommandResponseOfGuid?> CreateTagAsync(CreateTagDto dto)
    {
        try
        {
            return await _apiClient.CreateTagAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error creating tag: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid { Success = false, Message = $"API error: {ex.Message}", Errors = new List<string> { ex.Response ?? ex.Message } };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateTagAsync(Guid id, UpdateTagDto dto)
    {
        try
        {
            return await _apiClient.UpdateTagAsync(id, dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error updating tag: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid { Success = false, Message = $"API error: {ex.Message}", Errors = new List<string> { ex.Response ?? ex.Message } };
        }
    }

    public async Task<bool> DeleteTagAsync(Guid tagId)
    {
        try
        {
            await _apiClient.DeleteTagAsync(tagId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error deleting tag: {StatusCode}", ex.StatusCode);
            return false;
        }
    }

    public Task<ICollection<object>> GetTagsByEventAsync(Guid eventId)
    {
        // TODO: Fix this when API client is regenerated.
        _logger.LogWarning("[TAG SERVICE] GetTagsByEventAsync is not implemented.");
        return Task.FromResult<ICollection<object>>(new List<object>());
    }

    public Task<ICollection<object>> GetEventsByTagAsync(Guid tagId)
    {
        // TODO: Fix this when API client is regenerated.
        _logger.LogWarning("[TAG SERVICE] GetEventsByTagAsync is not implemented.");
        return Task.FromResult<ICollection<object>>(new List<object>());
    }

    public Task<BaseCommandResponseOfGuid?> AssignTagToEventAsync(object dto)
    {
        // TODO: Fix this when API client is regenerated.
        _logger.LogWarning("[TAG SERVICE] AssignTagToEventAsync is not implemented.");
        return Task.FromResult<BaseCommandResponseOfGuid?>(null);
    }

    public async Task<bool> RemoveTagFromEventAsync(Guid eventTagId)
    {
        try
        {
            // Note: This method may not exist in the regenerated client
            // await _apiClient.EventTagsDELETEAsync(eventTagId);
            _logger.LogWarning("[TAG SERVICE] RemoveTagFromEventAsync - endpoint may not exist");
            return false;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[TAG SERVICE] API error removing tag from event: {StatusCode}", ex.StatusCode);
            return false;
        }
    }
}
