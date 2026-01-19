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
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected ILogger<CreateOrganization> Logger { get; set; } = null!;

    // Organization data for the API
    private CreateOrganizationDto organization = new();
    private bool acceptTerms = false;
    private bool confirmInformation = false;
    private bool isSubmitting = false;
    private string logoPreview = string.Empty;
    private int currentStep = 0;

    // Image upload
    private ImageUpload? _imageUpload;
    private FileUploadData? _selectedLogoData;
    private bool _isUploadingLogo = false;
    private Guid? _uploadedLogoStorageObjectId = null;
    private string? _logoUploadError;

    // API response handling
    private bool submitSuccess = false;
    private string errorMessage = string.Empty;

    private class StepInfo
    {
        public int Step { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    private readonly StepInfo[] steps = new[]
    {
        new StepInfo { Step = 0, Title = "Basic Information", Icon = Icons.Material.Filled.Edit },
        new StepInfo { Step = 1, Title = "Contact & Address",  Icon = Icons.Material.Filled.Description },
        new StepInfo { Step = 2, Title = "Review & Submit",    Icon = Icons.Material.Filled.Done }
    };

    protected override async Task OnInitializedAsync()
    {
        // Initialiseer de organisatie met standaard waarden
        organization = new CreateOrganizationDto();
        await base.OnInitializedAsync();
    }

    private string GetStepClass(int step)
    {
        if (step < currentStep) return "completed";
        if (step == currentStep) return "active";
        return "";
    }

    private bool IsStepCompleted(int step) => step < currentStep;

    private string GetProgressWidth() => currentStep switch
    {
        0 => "0%",
        1 => "50%",
        2 => "100%",
        _ => "0%"
    };

    private Task GoToNextStep()
    {
        // Stap-validatie: controleer verplichte velden stap per stap
        if (currentStep == 0)
        {
            if (string.IsNullOrWhiteSpace(organization.FullName))
            {
                errorMessage = "Vul de organisatienaam in.";
                return Task.CompletedTask;
            }
        }
        else if (currentStep == 1)
        {
            if (string.IsNullOrWhiteSpace(organization.Email)
                || string.IsNullOrWhiteSpace(organization.Address)
                || string.IsNullOrWhiteSpace(organization.City)
                || string.IsNullOrWhiteSpace(organization.Country)
                || organization.Postcode <= 0)
            {
                errorMessage = "Vul alle vereiste velden in.";
                return Task.CompletedTask;
            }
        }

        errorMessage = string.Empty;
        if (currentStep < steps.Length - 1) currentStep++;
        return Task.CompletedTask;
    }

    private Task GoToPreviousStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    private async Task GoBack()
    {
        if (currentStep == 0)
            NavigationManager.NavigateTo("/");
        else
            await GoToPreviousStep();
    }

    private async Task HandleSubmit()
    {
        if (!CanSubmit()) return;

        isSubmitting = true;
        errorMessage = string.Empty;

        try
        {
            // Roep de API aan om de organisatie te maken
            var createdOrganization = await OrganizationService.CreateOrganizationAsync(organization);

            if (createdOrganization != null)
            {
                submitSuccess = true;
                Logger.LogInformation("Organization successfully created with ID: {OrganizationId}", createdOrganization.Id);

                // Wacht een moment en navigeer naar een succespagina
                await Task.Delay(1500);
                NavigationManager.NavigateTo("/organization/success");
            }
            else
            {
                errorMessage = "Er is een fout opgetreden bij het aanmaken van de organisatie. Probeer het opnieuw.";
                Logger.LogWarning("API returned null for organization creation");
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Fout bij het aanmaken van de organisatie: {ex.Message}";
            Logger.LogError(ex, "Exception during organization creation");
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles file selection with FileUploadData (bytes already in memory).
    /// This is the preferred method for reliable uploads in Blazor WASM.
    /// </summary>
    private async Task OnLogoFileDataSelected(FileUploadData? fileData)
    {
        _selectedLogoData = fileData;
        _logoUploadError = null;

        if (fileData == null)
        {
            // File was removed
            _uploadedLogoStorageObjectId = null;
            organization.ProfilePictureId = null;
            Logger.LogInformation("Logo selection cleared");
            return;
        }

        _isUploadingLogo = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Logger.LogInformation("Starting logo upload for {FileName} ({Size} bytes)", fileData.FileName, fileData.Size);

            // Upload the image using the bytes-based method (avoids stream issues)
            var uploadResult = await ImageStorageService.UploadAndCreateRecordFromBytesAsync(fileData);

            if (uploadResult?.Success == true)
            {
                _uploadedLogoStorageObjectId = uploadResult.StorageObjectId;
                organization.ProfilePictureId = uploadResult.StorageObjectId;
                _logoUploadError = null;
                Logger.LogInformation("Logo uploaded successfully. StorageObjectId: {StorageObjectId}", uploadResult.StorageObjectId);
                Snackbar.Add("Logo uploaded successfully!", Severity.Success);
            }
            else
            {
                var errorMsg = uploadResult?.ErrorMessage ?? "Failed to upload logo. Please try again.";
                Logger.LogWarning("Logo upload failed: {ErrorMessage}", errorMsg);
                _logoUploadError = errorMsg;
                Snackbar.Add(errorMsg, Severity.Error);

                // Clear the preview and selected file on failure
                await ClearLogoUploadState();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during logo upload for {FileName}", fileData.FileName);
            _logoUploadError = $"Upload error: {ex.Message}";
            Snackbar.Add("An error occurred while uploading the logo", Severity.Error);

            // Clear on error
            await ClearLogoUploadState();
        }
        finally
        {
            _isUploadingLogo = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Clears the logo upload state on error or cancellation.
    /// </summary>
    private async Task ClearLogoUploadState()
    {
        try
        {
            if (_imageUpload != null)
            {
                await _imageUpload.RemoveImage();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error clearing logo upload component");
        }

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
               !_isUploadingLogo; // Don't allow submit while uploading
    }
}
