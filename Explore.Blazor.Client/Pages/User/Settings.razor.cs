using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.User;

public partial class Settings
{
    [Inject] protected IUserService UserService { get; set; } = null!;
    [Inject] protected IImageStorageService ImageStorageService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    private UserDto? _user;
    private bool _loading = true;
    private bool _isSaving = false;
    private bool _success;
    private MudForm _form = default!;

    // Local image fields
    private IBrowserFile? _selectedProfileImage;
    private string? _profileImagePreview;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        try
        {
            _user = await UserService.GetCurrentUserAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading settings: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnProfileImageSelected(IBrowserFile? file)
    {
        _selectedProfileImage = file;
    }

    private async Task LoadUser()
    {
        _loading = true;
        try
        {
            _user = await UserService.GetCurrentUserAsync();
            if (_user != null)
            {
                // Initialize the preview with the user's current profile picture if it exists
                // The API returns a presigned URL in ProfileImageUri
                _profileImagePreview = _user.ProfileImageUri;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading user: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SaveSettings()
    {
        if (_user == null) return;

        _isSaving = true;
        try
        {
            Guid? profilePictureStorageId = null;

            // Upload a new profile picture if one was selected
            if (_selectedProfileImage != null)
            {
                var uploadResult = await ImageStorageService.UploadImageAndCreateRecordAsync(_selectedProfileImage);
                if (uploadResult?.Success == true)
                {
                    profilePictureStorageId = uploadResult.StorageObjectId;
                }
                else
                {
                    Snackbar.Add(uploadResult?.ErrorMessage ?? "Failed to upload profile picture.", Severity.Error);
                    _isSaving = false;
                    return; // Stop if upload fails
                }
            }

            var updateDto = new UpdateUserDto
            {
                Id = _user.Id,
                FirstName = _user.FirstName,
                LastName = _user.LastName,
                Email = _user.Email,
                Username = _user.Username,
                ProfilePictureId = profilePictureStorageId
            };

            var result = await UserService.UpdateUserAsync(updateDto);
            if (result?.Success == true)
            {
                Snackbar.Add("Settings saved successfully", Severity.Success);
                await LoadUser(); // Refresh user data
            }
            else
            {
                Snackbar.Add(result?.Message ?? "Failed to save settings", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving settings: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }
}
