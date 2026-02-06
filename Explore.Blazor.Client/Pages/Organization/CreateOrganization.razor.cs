using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Organization;

public partial class CreateOrganization
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected IOrganizationService OrganizationService { get; set; } = null!;
    [Inject] protected IImageStorageService ImageStorageService { get; set; } = null!;
    [Inject] protected ILogger<CreateOrganization> Logger { get; set; } = null!;

    private CreateOrganizationDto organization = new();
    private bool acceptTerms = false;
    private bool confirmInformation = false;
    private bool isSubmitting = false;
    private string logoPreview = string.Empty;
    
    private ImageUpload? _imageUpload;
    private FileUploadData? _selectedLogoData;
    private bool _isUploadingLogo = false;
    private Guid? _uploadedLogoStorageObjectId = null;
    private string? _logoUploadError;

    private bool submitSuccess = false;
    private string errorMessage = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        organization = new CreateOrganizationDto();
        await base.OnInitializedAsync();
    }

    private async Task HandleSubmit()
    {
        if (!CanSubmit()) return;

        isSubmitting = true;
        errorMessage = string.Empty;

        try
        {
            var createdOrganization = await OrganizationService.CreateOrganizationAsync(organization);

            if (createdOrganization != null)
            {
                submitSuccess = true;
                Logger.LogInformation("Organization successfully created with ID: {OrganizationId}", createdOrganization.Id);
                await Task.Delay(1000);
                NavigationManager.NavigateTo("/organization/success");
            }
            else
            {
                errorMessage = "An error occurred while creating the organization. Please try again.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error creating organization: {ex.Message}";
            Logger.LogError(ex, "Exception during organization creation");
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task OnLogoFileDataSelected(FileUploadData? fileData)
    {
        _selectedLogoData = fileData;
        _logoUploadError = null;

        if (fileData == null)
        {
            _uploadedLogoStorageObjectId = null;
            organization.ProfilePictureId = null;
            return;
        }

        _isUploadingLogo = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var uploadResult = await ImageStorageService.UploadAndCreateRecordFromBytesAsync(fileData);

            if (uploadResult?.Success == true)
            {
                _uploadedLogoStorageObjectId = uploadResult.StorageObjectId;
                organization.ProfilePictureId = uploadResult.StorageObjectId;
                _logoUploadError = null;
            }
            else
            {
                _logoUploadError = uploadResult?.ErrorMessage ?? "Failed to upload logo.";
                await ClearLogoUploadState();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during logo upload");
            _logoUploadError = $"Upload error: {ex.Message}";
            await ClearLogoUploadState();
        }
        finally
        {
            _isUploadingLogo = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ClearLogoUploadState()
    {
        try
        {
            if (_imageUpload != null)
            {
                await _imageUpload.RemoveImage();
            }
        }
        catch { }

        _selectedLogoData = null;
        _uploadedLogoStorageObjectId = null;
        organization.ProfilePictureId = null;
    }

    private void OnLogoPreviewUrlChanged(string? value)
    {
        logoPreview = value ?? string.Empty;
    }

    private bool CanSubmit()
    {
        return acceptTerms &&
               confirmInformation &&
               !string.IsNullOrWhiteSpace(organization.FullName) &&
               !string.IsNullOrWhiteSpace(organization.Email) &&
               !string.IsNullOrWhiteSpace(organization.Address) &&
               !string.IsNullOrWhiteSpace(organization.City) &&
               !string.IsNullOrWhiteSpace(organization.Country) &&
               organization.Postcode > 0 &&
               !_isUploadingLogo;
    }
}