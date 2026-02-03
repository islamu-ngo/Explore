// ABOUTME: Admin service for managing organizations, lookup tables, and CRUD operations for categories/tags/locations.
// This is the Anti-Corruption Layer that converts HAL responses to clean DTOs for UI consumption.

using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Known Approval Status IDs from the backend (ApprovalStatusEnum).
/// </summary>
public static class ApprovalStatusId
{
    public const int Pending = 1;
    public const int Approved = 2;
    public const int Rejected = 3;
}

public interface IAdminService
{
    // Organization management
    Task<ICollection<OrganizationListDto>> GetOrganizationRequestsAsync();
    Task<OrganizationDto?> GetOrganizationDetailsAsync(Guid id);
    Task<bool> ApproveOrganizationAsync(Guid id);
    Task<bool> RejectOrganizationAsync(Guid id);
    Task<bool> RevertToPendingAsync(Guid id);

    // Lookup tables - Event related
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
    Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync();
    Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync();
    Task<ICollection<EventFormatListDto>> GetEventFormatsAsync();
    Task<ICollection<EventStatusListDto>> GetEventStatusesAsync();
    Task<ICollection<MadhabListDto>> GetMadhabsAsync();
    Task<ICollection<VisibilityTypeListDto>> GetVisibilityTypesAsync();
    Task<ICollection<RegistrationModeListDto>> GetRegistrationModesAsync();
    Task<ICollection<LanguageListDto>> GetLanguagesAsync();

    // Lookup tables - Organization/Actor related
    Task<ICollection<OrganizationRoleListDto>> GetOrganizationRolesAsync();
    Task<ICollection<OrganizationPositionListDto>> GetOrganizationPositionsAsync();
    Task<ICollection<ActorTypeListDto>> GetActorTypesAsync();
    Task<ICollection<StatusTypeListDto>> GetApprovalStatusesAsync();

    // Lookup tables - Other
    Task<ICollection<FileTypeListDto>> GetFileTypesAsync();
    Task<ICollection<DidCustodyTypeListDto>> GetDidCustodyTypesAsync();

    // Category CRUD
    Task<ICollection<CategoryListDto>> GetCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id);
    Task<bool> CreateCategoryAsync(CreateCategoryDto category);
    Task<bool> UpdateCategoryAsync(UpdateCategoryDto category);
    Task<bool> DeleteCategoryAsync(Guid id);

    // Tag CRUD
    Task<ICollection<TagListDto>> GetTagsAsync();
    Task<TagDto?> GetTagByIdAsync(Guid id);
    Task<bool> CreateTagAsync(CreateTagDto tag);
    Task<bool> UpdateTagAsync(UpdateTagDto tag);
    Task<bool> DeleteTagAsync(Guid id);

    // Location CRUD
    Task<ICollection<LocationListDto>> GetLocationsAsync();
    Task<LocationDto?> GetLocationByIdAsync(Guid id);
    Task<bool> CreateLocationAsync(CreateLocationDto location);
    Task<bool> UpdateLocationAsync(UpdateLocationDto location);
    Task<bool> DeleteLocationAsync(Guid id);
}

public class AdminService : IAdminService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IEventApiClient apiClient, ILogger<AdminService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<OrganizationListDto>> GetOrganizationRequestsAsync()
    {
        try
        {
            var response = await _apiClient.GetOrganizationsAsync(pageNumber: 1, pageSize: 100);
            return response?.GetItems() ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching organizations: {StatusCode}", ex.StatusCode);
            return new List<OrganizationListDto>();
        }
    }

    public async Task<OrganizationDto?> GetOrganizationDetailsAsync(Guid id)
    {
        try
        {
            var response = await _apiClient.GetOrganizationByIdAsync(id);
            return response?.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[ADMIN SERVICE] Organization not found: {OrganizationId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching organization details: {StatusCode}", ex.StatusCode);
            return null;
        }
    }

    public async Task<bool> ApproveOrganizationAsync(Guid id)
    {
        try
        {
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = ApprovalStatusId.Approved };
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode is 204 or 200)
        {
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error approving organization");
            return false;
        }
    }

    public async Task<bool> RejectOrganizationAsync(Guid id)
    {
        try
        {
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = ApprovalStatusId.Rejected };
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode is 204 or 200)
        {
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error rejecting organization");
            return false;
        }
    }

    public async Task<bool> RevertToPendingAsync(Guid id)
    {
        try
        {
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = ApprovalStatusId.Pending };
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode is 204 or 200)
        {
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error reverting organization");
            return false;
        }
    }

    public async Task<ICollection<EventTypeListDto>> GetEventTypesAsync()
    {
        try
        {
            return await _apiClient.EventTypeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching event types");
            return new List<EventTypeListDto>();
        }
    }

    public async Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync()
    {
        try
        {
            return await _apiClient.AudienceGenderAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching audience genders");
            return new List<AudienceGenderListDto>();
        }
    }

    public async Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync()
    {
        try
        {
            return await _apiClient.AudienceAgeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching audience ages");
            return new List<AudienceAgeListDto>();
        }
    }

    public async Task<ICollection<EventFormatListDto>> GetEventFormatsAsync()
    {
        try
        {
            return await _apiClient.EventFormatAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching event formats");
            return new List<EventFormatListDto>();
        }
    }

    public async Task<ICollection<EventStatusListDto>> GetEventStatusesAsync()
    {
        try
        {
            return await _apiClient.EventStatusAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching event statuses");
            return new List<EventStatusListDto>();
        }
    }

    public async Task<ICollection<MadhabListDto>> GetMadhabsAsync()
    {
        try
        {
            return await _apiClient.MadhabAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching madhabs");
            return new List<MadhabListDto>();
        }
    }

    public async Task<ICollection<VisibilityTypeListDto>> GetVisibilityTypesAsync()
    {
        try
        {
            return await _apiClient.VisibilityTypeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching visibility types");
            return new List<VisibilityTypeListDto>();
        }
    }

    public async Task<ICollection<RegistrationModeListDto>> GetRegistrationModesAsync()
    {
        try
        {
            return await _apiClient.RegistrationModeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching registration modes");
            return new List<RegistrationModeListDto>();
        }
    }

    public async Task<ICollection<LanguageListDto>> GetLanguagesAsync()
    {
        try
        {
            return await _apiClient.LanguageAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching languages");
            return new List<LanguageListDto>();
        }
    }

    public async Task<ICollection<OrganizationRoleListDto>> GetOrganizationRolesAsync()
    {
        try
        {
            return await _apiClient.OrganizationRoleAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching organization roles");
            return new List<OrganizationRoleListDto>();
        }
    }

    public async Task<ICollection<OrganizationPositionListDto>> GetOrganizationPositionsAsync()
    {
        try
        {
            return await _apiClient.OrganizationPositionAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching organization positions");
            return new List<OrganizationPositionListDto>();
        }
    }

    public async Task<ICollection<ActorTypeListDto>> GetActorTypesAsync()
    {
        try
        {
            return await _apiClient.ActorTypeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching actor types");
            return new List<ActorTypeListDto>();
        }
    }

    public async Task<ICollection<StatusTypeListDto>> GetApprovalStatusesAsync()
    {
        try
        {
            return await _apiClient.ApprovalStatusAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching approval statuses");
            return new List<StatusTypeListDto>();
        }
    }

    public async Task<ICollection<FileTypeListDto>> GetFileTypesAsync()
    {
        try
        {
            return await _apiClient.FileTypeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching file types");
            return new List<FileTypeListDto>();
        }
    }

    public async Task<ICollection<DidCustodyTypeListDto>> GetDidCustodyTypesAsync()
    {
        try
        {
            return await _apiClient.DidCustodyTypeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching DID custody types");
            return new List<DidCustodyTypeListDto>();
        }
    }

    // Category CRUD
    public async Task<ICollection<CategoryListDto>> GetCategoriesAsync()
    {
        try
        {
            var response = await _apiClient.GetCategoriesAsync(1, 100);
            return response?.GetItems() ?? new List<CategoryListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching categories");
            return new List<CategoryListDto>();
        }
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        try
        {
            var response = await _apiClient.GetCategoryByIdAsync(id);
            return response?.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching category");
            return null;
        }
    }

    public async Task<bool> CreateCategoryAsync(CreateCategoryDto category)
    {
        try
        {
            await _apiClient.CreateCategoryAsync(category);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error creating category");
            return false;
        }
    }

    public async Task<bool> UpdateCategoryAsync(UpdateCategoryDto category)
    {
        try
        {
            if (!category.Id.HasValue)
            {
                _logger.LogWarning("[ADMIN SERVICE] Category ID is null");
                return false;
            }
            await _apiClient.UpdateCategoryAsync(category.Id.Value, category);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error updating category");
            return false;
        }
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        try
        {
            await _apiClient.DeleteCategoryAsync(id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error deleting category");
            return false;
        }
    }

    // Tag CRUD
    public async Task<ICollection<TagListDto>> GetTagsAsync()
    {
        try
        {
            var response = await _apiClient.GetTagsAsync(1, 100);
            return response?.GetItems() ?? new List<TagListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching tags");
            return new List<TagListDto>();
        }
    }

    public async Task<TagDto?> GetTagByIdAsync(Guid id)
    {
        try
        {
            var response = await _apiClient.GetTagByIdAsync(id);
            return response?.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching tag");
            return null;
        }
    }

    public async Task<bool> CreateTagAsync(CreateTagDto tag)
    {
        try
        {
            await _apiClient.CreateTagAsync(tag);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error creating tag");
            return false;
        }
    }

    public async Task<bool> UpdateTagAsync(UpdateTagDto tag)
    {
        try
        {
            if (!tag.Id.HasValue)
            {
                _logger.LogWarning("[ADMIN SERVICE] Tag ID is null");
                return false;
            }
            await _apiClient.UpdateTagAsync(tag.Id.Value, tag);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error updating tag");
            return false;
        }
    }

    public async Task<bool> DeleteTagAsync(Guid id)
    {
        try
        {
            await _apiClient.DeleteTagAsync(id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error deleting tag");
            return false;
        }
    }

    // Location CRUD
    public async Task<ICollection<LocationListDto>> GetLocationsAsync()
    {
        try
        {
            var response = await _apiClient.GetLocationsAsync(1, 100);
            return response?.GetItems() ?? new List<LocationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching locations");
            return new List<LocationListDto>();
        }
    }

    public async Task<LocationDto?> GetLocationByIdAsync(Guid id)
    {
        try
        {
            var response = await _apiClient.GetLocationByIdAsync(id);
            return response?.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching location");
            return null;
        }
    }

    public async Task<bool> CreateLocationAsync(CreateLocationDto location)
    {
        try
        {
            await _apiClient.CreateLocationAsync(location);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error creating location");
            return false;
        }
    }

    public async Task<bool> UpdateLocationAsync(UpdateLocationDto location)
    {
        try
        {
            if (!location.Id.HasValue)
            {
                _logger.LogWarning("[ADMIN SERVICE] Location ID is null");
                return false;
            }
            await _apiClient.UpdateLocationAsync(location.Id.Value, location);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error updating location");
            return false;
        }
    }

    public async Task<bool> DeleteLocationAsync(Guid id)
    {
        try
        {
            await _apiClient.DeleteLocationAsync(id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error deleting location");
            return false;
        }
    }
}
