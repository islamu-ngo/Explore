using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

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
            _logger.LogInformation("[ADMIN SERVICE] Fetching all organizations via API client");
            var response = await _apiClient.OrganizationGETAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[ADMIN SERVICE] Received {Count} organizations from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching organizations: {StatusCode}", ex.StatusCode);
            return new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching organizations");
            return new List<OrganizationListDto>();
        }
    }

    public async Task<OrganizationDto?> GetOrganizationDetailsAsync(Guid id)
    {
        try
        {
            return await _apiClient.OrganizationGET2Async(id);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching organization details");
            return null;
        }
    }

    public async Task<bool> ApproveOrganizationAsync(Guid id)
    {
        try
        {
            // Status 2 = Approved
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = 2 };
            _logger.LogInformation("[ADMIN SERVICE] Approving organization {OrganizationId} with status 2", id);
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            _logger.LogInformation("[ADMIN SERVICE] Organization approved successfully");
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 204 || ex.StatusCode == 200)
        {
            _logger.LogInformation("[ADMIN SERVICE] Organization approved successfully (HTTP {StatusCode})", ex.StatusCode);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error approving organization: {StatusCode}", ex.StatusCode);
            return false;
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
            // Status 3 = Rejected
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = 3 };
            _logger.LogInformation("[ADMIN SERVICE] Rejecting organization {OrganizationId} with status 3", id);
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            _logger.LogInformation("[ADMIN SERVICE] Organization rejected successfully");
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 204 || ex.StatusCode == 200)
        {
            _logger.LogInformation("[ADMIN SERVICE] Organization rejected successfully (HTTP {StatusCode})", ex.StatusCode);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error rejecting organization: {StatusCode}", ex.StatusCode);
            return false;
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
            // Status 1 = Pending
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = 1 };
            _logger.LogInformation("[ADMIN SERVICE] Reverting organization {OrganizationId} to pending with status 1", id);
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            _logger.LogInformation("[ADMIN SERVICE] Organization reverted to pending successfully");
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 204 || ex.StatusCode == 200)
        {
            _logger.LogInformation("[ADMIN SERVICE] Organization reverted to pending successfully (HTTP {StatusCode})", ex.StatusCode);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error reverting organization: {StatusCode}", ex.StatusCode);
            return false;
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
            _logger.LogInformation("[ADMIN SERVICE] Fetching event types...");
            var response = await _apiClient.EventTypeAllAsync();
            _logger.LogInformation("[ADMIN SERVICE] Received {Count} event types", response?.Count ?? 0);
            return response ?? new List<EventTypeListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching event types: {StatusCode}", ex.StatusCode);
            return new List<EventTypeListDto>();
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
            _logger.LogInformation("[ADMIN SERVICE] Fetching audience genders...");
            var response = await _apiClient.AudienceGenderAllAsync();
            _logger.LogInformation("[ADMIN SERVICE] Received {Count} audience genders", response?.Count ?? 0);
            return response ?? new List<AudienceGenderListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching audience genders: {StatusCode}", ex.StatusCode);
            return new List<AudienceGenderListDto>();
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
            _logger.LogInformation("[ADMIN SERVICE] Fetching audience ages...");
            var response = await _apiClient.AudienceAgeAllAsync();
            _logger.LogInformation("[ADMIN SERVICE] Received {Count} audience ages", response?.Count ?? 0);
            return response ?? new List<AudienceAgeListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching audience ages: {StatusCode}", ex.StatusCode);
            return new List<AudienceAgeListDto>();
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
            var response = await _apiClient.EventFormatAllAsync();
            return response ?? new List<EventFormatListDto>();
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
            var response = await _apiClient.EventStatusAllAsync();
            return response ?? new List<EventStatusListDto>();
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
            var response = await _apiClient.MadhabAllAsync();
            return response ?? new List<MadhabListDto>();
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
            var response = await _apiClient.VisibilityTypeAllAsync();
            return response ?? new List<VisibilityTypeListDto>();
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
            var response = await _apiClient.RegistrationModeAllAsync();
            return response ?? new List<RegistrationModeListDto>();
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
            var response = await _apiClient.LanguageAllAsync();
            return response ?? new List<LanguageListDto>();
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
            var response = await _apiClient.OrganizationRoleAllAsync();
            return response ?? new List<OrganizationRoleListDto>();
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
            var response = await _apiClient.OrganizationPositionAllAsync();
            return response ?? new List<OrganizationPositionListDto>();
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
            var response = await _apiClient.ActorTypeAllAsync();
            return response ?? new List<ActorTypeListDto>();
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
            var response = await _apiClient.ApprovalStatusAllAsync();
            return response ?? new List<StatusTypeListDto>();
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
            var response = await _apiClient.FileTypeAllAsync();
            return response ?? new List<FileTypeListDto>();
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
            var response = await _apiClient.DidCustodyTypeAllAsync();
            return response ?? new List<DidCustodyTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error fetching DID custody types");
            return new List<DidCustodyTypeListDto>();
        }
    }

    public async Task<ICollection<CategoryListDto>> GetCategoriesAsync()
    {
        try
        {
            _logger.LogInformation("[ADMIN SERVICE] Fetching categories...");
            var response = await _apiClient.CategoryGETAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[ADMIN SERVICE] Received {Count} categories from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<CategoryListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching categories: {StatusCode}", ex.StatusCode);
            return new List<CategoryListDto>();
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
            return await _apiClient.CategoryGET2Async(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[ADMIN SERVICE] Category not found: {CategoryId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching category: {StatusCode}", ex.StatusCode);
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
            await _apiClient.CategoryPOSTAsync(category);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error creating category: {StatusCode}", ex.StatusCode);
            return false;
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
            await _apiClient.CategoryPUTAsync(category.Id, category);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error updating category: {StatusCode}", ex.StatusCode);
            return false;
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
            await _apiClient.CategoryDELETEAsync(id);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error deleting category: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error deleting category");
            return false;
        }
    }

    public async Task<ICollection<TagListDto>> GetTagsAsync()
    {
        try
        {
            _logger.LogInformation("[ADMIN SERVICE] Fetching tags...");
            var response = await _apiClient.TagGETAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[ADMIN SERVICE] Received {Count} tags from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<TagListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching tags: {StatusCode}", ex.StatusCode);
            return new List<TagListDto>();
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
            return await _apiClient.TagGET2Async(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[ADMIN SERVICE] Tag not found: {TagId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching tag: {StatusCode}", ex.StatusCode);
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
            await _apiClient.TagPOSTAsync(tag);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error creating tag: {StatusCode}", ex.StatusCode);
            return false;
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
            await _apiClient.TagPUTAsync(tag.Id, tag);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error updating tag: {StatusCode}", ex.StatusCode);
            return false;
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
            await _apiClient.TagDELETEAsync(id);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error deleting tag: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error deleting tag");
            return false;
        }
    }

    public async Task<ICollection<LocationListDto>> GetLocationsAsync()
    {
        try
        {
            _logger.LogInformation("[ADMIN SERVICE] Fetching locations...");
            var response = await _apiClient.LocationGETAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[ADMIN SERVICE] Received {Count} locations from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<LocationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching locations: {StatusCode}", ex.StatusCode);
            return new List<LocationListDto>();
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
            return await _apiClient.LocationGET2Async(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[ADMIN SERVICE] Location not found: {LocationId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error fetching location: {StatusCode}", ex.StatusCode);
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
            await _apiClient.LocationPOSTAsync(location);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error creating location: {StatusCode}", ex.StatusCode);
            return false;
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
            await _apiClient.LocationPUTAsync(location.Id, location);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error updating location: {StatusCode}", ex.StatusCode);
            return false;
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
            await _apiClient.LocationDELETEAsync(id);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] API error deleting location: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN SERVICE] Error deleting location");
            return false;
        }
    }
}
