// ABOUTME: Admin service for managing organizations, lookup tables, and CRUD operations for categories/tags/locations.
// This is the Anti-Corruption Layer that converts HAL responses to clean DTOs for UI consumption.

using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Constants;
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
            var response = await _apiClient.GetOrganizationsAsync(pageNumber: ApiConstants.FirstPage, pageSize: ApiConstants.DefaultPageSize);
            return response?.GetItems() ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetOrganizationRequestsAsync] API error fetching organizations. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetOrganizationRequestsAsync] Unexpected error fetching organizations");
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
            _logger.LogWarning("[AdminService.GetOrganizationDetailsAsync] Organization not found. OrganizationId: {OrganizationId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetOrganizationDetailsAsync] API error fetching organization details. OrganizationId: {OrganizationId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetOrganizationDetailsAsync] Unexpected error fetching organization details. OrganizationId: {OrganizationId}", id);
            return null;
        }
    }

    public async Task<bool> ApproveOrganizationAsync(Guid id)
    {
        try
        {
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = ApprovalStatusId.Approved };
            await _apiClient.UpdateOrganizationApprovalStatusAsync(id, updateDto);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode is 204 or 200)
        {
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.ApproveOrganizationAsync] API error approving organization. OrganizationId: {OrganizationId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.ApproveOrganizationAsync] Unexpected error approving organization. OrganizationId: {OrganizationId}", id);
            return false;
        }
    }

    public async Task<bool> RejectOrganizationAsync(Guid id)
    {
        try
        {
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = ApprovalStatusId.Rejected };
            await _apiClient.UpdateOrganizationApprovalStatusAsync(id, updateDto);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode is 204 or 200)
        {
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.RejectOrganizationAsync] API error rejecting organization. OrganizationId: {OrganizationId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.RejectOrganizationAsync] Unexpected error rejecting organization. OrganizationId: {OrganizationId}", id);
            return false;
        }
    }

    public async Task<bool> RevertToPendingAsync(Guid id)
    {
        try
        {
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = ApprovalStatusId.Pending };
            await _apiClient.UpdateOrganizationApprovalStatusAsync(id, updateDto);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode is 204 or 200)
        {
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.RevertToPendingAsync] API error reverting organization to pending. OrganizationId: {OrganizationId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.RevertToPendingAsync] Unexpected error reverting organization to pending. OrganizationId: {OrganizationId}", id);
            return false;
        }
    }

    public async Task<ICollection<EventTypeListDto>> GetEventTypesAsync()
    {
        try
        {
            return await _apiClient.GetEventTypesAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetEventTypesAsync] API error fetching event types. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<EventTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetEventTypesAsync] Unexpected error fetching event types");
            return new List<EventTypeListDto>();
        }
    }

    public async Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync()
    {
        try
        {
            return await _apiClient.GetAudienceGenderOptionsAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetAudienceGendersAsync] API error fetching audience genders. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<AudienceGenderListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetAudienceGendersAsync] Unexpected error fetching audience genders");
            return new List<AudienceGenderListDto>();
        }
    }

    public async Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync()
    {
        try
        {
            return await _apiClient.GetAudienceAgeOptionsAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetAudienceAgesAsync] API error fetching audience ages. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<AudienceAgeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetAudienceAgesAsync] Unexpected error fetching audience ages");
            return new List<AudienceAgeListDto>();
        }
    }

    public async Task<ICollection<EventFormatListDto>> GetEventFormatsAsync()
    {
        try
        {
            return await _apiClient.GetEventFormatOptionsAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetEventFormatsAsync] API error fetching event formats. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<EventFormatListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetEventFormatsAsync] Unexpected error fetching event formats");
            return new List<EventFormatListDto>();
        }
    }

    public async Task<ICollection<EventStatusListDto>> GetEventStatusesAsync()
    {
        try
        {
            return await _apiClient.GetEventStatusesAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetEventStatusesAsync] API error fetching event statuses. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<EventStatusListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetEventStatusesAsync] Unexpected error fetching event statuses");
            return new List<EventStatusListDto>();
        }
    }

    public async Task<ICollection<MadhabListDto>> GetMadhabsAsync()
    {
        try
        {
            return await _apiClient.GetMadhabsAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetMadhabsAsync] API error fetching madhabs. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<MadhabListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetMadhabsAsync] Unexpected error fetching madhabs");
            return new List<MadhabListDto>();
        }
    }

    public async Task<ICollection<VisibilityTypeListDto>> GetVisibilityTypesAsync()
    {
        try
        {
            return await _apiClient.GetVisibilityTypesAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetVisibilityTypesAsync] API error fetching visibility types. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<VisibilityTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetVisibilityTypesAsync] Unexpected error fetching visibility types");
            return new List<VisibilityTypeListDto>();
        }
    }

    public async Task<ICollection<RegistrationModeListDto>> GetRegistrationModesAsync()
    {
        try
        {
            return await _apiClient.GetRegistrationModesAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetRegistrationModesAsync] API error fetching registration modes. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<RegistrationModeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetRegistrationModesAsync] Unexpected error fetching registration modes");
            return new List<RegistrationModeListDto>();
        }
    }

    public async Task<ICollection<LanguageListDto>> GetLanguagesAsync()
    {
        try
        {
            return await _apiClient.GetLanguagesAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetLanguagesAsync] API error fetching languages. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<LanguageListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetLanguagesAsync] Unexpected error fetching languages");
            return new List<LanguageListDto>();
        }
    }

    public async Task<ICollection<OrganizationPositionListDto>> GetOrganizationPositionsAsync()
    {
        try
        {
            return await _apiClient.GetOrganizationPositionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetOrganizationPositionsAsync] Unexpected error fetching organization positions");
            return new List<OrganizationPositionListDto>();
        }
    }

    public async Task<ICollection<ActorTypeListDto>> GetActorTypesAsync()
    {
        try
        {
            return await _apiClient.GetActorTypesAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetActorTypesAsync] API error fetching actor types. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<ActorTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetActorTypesAsync] Unexpected error fetching actor types");
            return new List<ActorTypeListDto>();
        }
    }

    public async Task<ICollection<StatusTypeListDto>> GetApprovalStatusesAsync()
    {
        try
        {
            return await _apiClient.GetApprovalStatusOptionsAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetApprovalStatusesAsync] API error fetching approval statuses. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<StatusTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetApprovalStatusesAsync] Unexpected error fetching approval statuses");
            return new List<StatusTypeListDto>();
        }
    }

    public async Task<ICollection<FileTypeListDto>> GetFileTypesAsync()
    {
        try
        {
            return await _apiClient.GetFileTypesAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetFileTypesAsync] API error fetching file types. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<FileTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetFileTypesAsync] Unexpected error fetching file types");
            return new List<FileTypeListDto>();
        }
    }

    public async Task<ICollection<DidCustodyTypeListDto>> GetDidCustodyTypesAsync()
    {
        try
        {
            return await _apiClient.GetDidCustodyTypeOptionsAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetDidCustodyTypesAsync] API error fetching DID custody types. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<DidCustodyTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetDidCustodyTypesAsync] Unexpected error fetching DID custody types");
            return new List<DidCustodyTypeListDto>();
        }
    }

    // Category CRUD
    public async Task<ICollection<CategoryListDto>> GetCategoriesAsync()
    {
        try
        {
            var response = await _apiClient.GetCategoriesAsync(ApiConstants.FirstPage, ApiConstants.DefaultPageSize);
            return response?.GetItems() ?? new List<CategoryListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetCategoriesAsync] API error fetching categories. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<CategoryListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetCategoriesAsync] Unexpected error fetching categories");
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
            _logger.LogWarning("[AdminService.GetCategoryByIdAsync] Category not found. CategoryId: {CategoryId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetCategoryByIdAsync] API error fetching category. CategoryId: {CategoryId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetCategoryByIdAsync] Unexpected error fetching category. CategoryId: {CategoryId}", id);
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
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.CreateCategoryAsync] API error creating category. StatusCode: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.CreateCategoryAsync] Unexpected error creating category");
            return false;
        }
    }

    public async Task<bool> UpdateCategoryAsync(UpdateCategoryDto category)
    {
        try
        {
            if (!category.Id.HasValue)
            {
                _logger.LogWarning("[AdminService.UpdateCategoryAsync] Category ID is null, cannot update");
                return false;
            }
            await _apiClient.UpdateCategoryAsync(category.Id.Value, category);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.UpdateCategoryAsync] API error updating category. CategoryId: {CategoryId}, StatusCode: {StatusCode}", category.Id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.UpdateCategoryAsync] Unexpected error updating category. CategoryId: {CategoryId}", category.Id);
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
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.DeleteCategoryAsync] API error deleting category. CategoryId: {CategoryId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.DeleteCategoryAsync] Unexpected error deleting category. CategoryId: {CategoryId}", id);
            return false;
        }
    }

    // Tag CRUD
    public async Task<ICollection<TagListDto>> GetTagsAsync()
    {
        try
        {
            var response = await _apiClient.GetTagsAsync(ApiConstants.FirstPage, ApiConstants.DefaultPageSize);
            return response?.GetItems() ?? new List<TagListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetTagsAsync] API error fetching tags. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<TagListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetTagsAsync] Unexpected error fetching tags");
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
            _logger.LogWarning("[AdminService.GetTagByIdAsync] Tag not found. TagId: {TagId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetTagByIdAsync] API error fetching tag. TagId: {TagId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetTagByIdAsync] Unexpected error fetching tag. TagId: {TagId}", id);
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
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.CreateTagAsync] API error creating tag. StatusCode: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.CreateTagAsync] Unexpected error creating tag");
            return false;
        }
    }

    public async Task<bool> UpdateTagAsync(UpdateTagDto tag)
    {
        try
        {
            if (!tag.Id.HasValue)
            {
                _logger.LogWarning("[AdminService.UpdateTagAsync] Tag ID is null, cannot update");
                return false;
            }
            await _apiClient.UpdateTagAsync(tag.Id.Value, tag);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.UpdateTagAsync] API error updating tag. TagId: {TagId}, StatusCode: {StatusCode}", tag.Id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.UpdateTagAsync] Unexpected error updating tag. TagId: {TagId}", tag.Id);
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
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.DeleteTagAsync] API error deleting tag. TagId: {TagId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.DeleteTagAsync] Unexpected error deleting tag. TagId: {TagId}", id);
            return false;
        }
    }

    // Location CRUD
    public async Task<ICollection<LocationListDto>> GetLocationsAsync()
    {
        try
        {
            var response = await _apiClient.GetLocationsAsync(ApiConstants.FirstPage, ApiConstants.DefaultPageSize);
            return response?.GetItems() ?? new List<LocationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetLocationsAsync] API error fetching locations. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<LocationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetLocationsAsync] Unexpected error fetching locations");
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
            _logger.LogWarning("[AdminService.GetLocationByIdAsync] Location not found. LocationId: {LocationId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.GetLocationByIdAsync] API error fetching location. LocationId: {LocationId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.GetLocationByIdAsync] Unexpected error fetching location. LocationId: {LocationId}", id);
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
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.CreateLocationAsync] API error creating location. StatusCode: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.CreateLocationAsync] Unexpected error creating location");
            return false;
        }
    }

    public async Task<bool> UpdateLocationAsync(UpdateLocationDto location)
    {
        try
        {
            if (!location.Id.HasValue)
            {
                _logger.LogWarning("[AdminService.UpdateLocationAsync] Location ID is null, cannot update");
                return false;
            }
            await _apiClient.UpdateLocationAsync(location.Id.Value, location);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.UpdateLocationAsync] API error updating location. LocationId: {LocationId}, StatusCode: {StatusCode}", location.Id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.UpdateLocationAsync] Unexpected error updating location. LocationId: {LocationId}", location.Id);
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
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[AdminService.DeleteLocationAsync] API error deleting location. LocationId: {LocationId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminService.DeleteLocationAsync] Unexpected error deleting location. LocationId: {LocationId}", id);
            return false;
        }
    }
}

