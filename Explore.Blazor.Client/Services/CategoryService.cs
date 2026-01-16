using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface ICategoryService
{
    Task<ICollection<CategoryListDto>> GetAllCategoriesAsync();
    Task<ICollection<CategoryListDto>> GetCategories(); // Alias for admin pages
    Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId);
    Task<BaseCommandResponseOfGuid?> CreateCategoryAsync(CreateCategoryDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(Guid categoryId);
    Task<ICollection<CategoryListDto>> GetCategoriesByEventAsync(Guid eventId);
    Task<ICollection<EventListDto>> GetEventsByCategoryAsync(Guid categoryId);
    Task<BaseCommandResponseOfGuid?> AssignCategoryToEventAsync(CreateEventCategoriesDto dto);
    Task<bool> RemoveCategoryFromEventAsync(Guid eventCategoryId);
}

public class CategoryService : ICategoryService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IEventApiClient apiClient, ILogger<CategoryService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<CategoryListDto>> GetAllCategoriesAsync()
    {
        try
        {
            _logger.LogInformation("[CATEGORY SERVICE] Fetching all categories...");
            var response = await _apiClient.CategoryGETAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[CATEGORY SERVICE] Received {Count} categories from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<CategoryListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error fetching categories: {StatusCode}", ex.StatusCode);
            return new List<CategoryListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error fetching categories");
            return new List<CategoryListDto>();
        }
    }

    /// <summary>
    /// Alias for GetAllCategoriesAsync() - used by admin pages.
    /// </summary>
    public Task<ICollection<CategoryListDto>> GetCategories() => GetAllCategoriesAsync();

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId)
    {
        try
        {
            return await _apiClient.CategoryGET2Async(categoryId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[CATEGORY SERVICE] Category not found: {CategoryId}", categoryId);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error fetching category {CategoryId}: {StatusCode}", categoryId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error fetching category {CategoryId}", categoryId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateCategoryAsync(CreateCategoryDto dto)
    {
        try
        {
            _logger.LogInformation("[CATEGORY SERVICE] Creating category: {Name}", dto.FullName);
            return await _apiClient.CategoryPOSTAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error creating category: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error creating category");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        try
        {
            return await _apiClient.CategoryPUTAsync(id, dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error updating category: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error updating category");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> DeleteCategoryAsync(Guid categoryId)
    {
        try
        {
            await _apiClient.CategoryDELETEAsync(categoryId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error deleting category: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error deleting category");
            return false;
        }
    }

    public async Task<ICollection<CategoryListDto>> GetCategoriesByEventAsync(Guid eventId)
    {
        try
        {
            var response = await _apiClient.ByEventAsync(eventId);
            return response ?? new List<CategoryListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error fetching event categories: {StatusCode}", ex.StatusCode);
            return new List<CategoryListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error fetching event categories");
            return new List<CategoryListDto>();
        }
    }

    public async Task<ICollection<EventListDto>> GetEventsByCategoryAsync(Guid categoryId)
    {
        try
        {
            var response = await _apiClient.ByCategoryAsync(categoryId);
            return response ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error fetching events by category: {StatusCode}", ex.StatusCode);
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error fetching events by category");
            return new List<EventListDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> AssignCategoryToEventAsync(CreateEventCategoriesDto dto)
    {
        try
        {
            return await _apiClient.EventCategoriesPOSTAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error assigning category to event: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error assigning category to event");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> RemoveCategoryFromEventAsync(Guid eventCategoryId)
    {
        try
        {
            await _apiClient.EventCategoriesDELETEAsync(eventCategoryId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] API error removing category from event: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CATEGORY SERVICE] Error removing category from event");
            return false;
        }
    }
}
