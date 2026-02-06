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
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    private UserDto? _user;
    private bool _loading = true;
    private bool _isSaving = false;
    private bool _success;
    private MudForm _form = default!;
    private string? _errorMessage;
    private string? _successMessage;

    // Local image fields
    private FileUploadData? _selectedProfileImageData;
    private string? _profileImagePreview;

    protected override async Task OnInitializedAsync()
    {
        await LoadUser();
    }

    private void OnProfileImageFileDataSelected(FileUploadData? fileData)
    {
        _selectedProfileImageData = fileData;
    }

    private async Task LoadUser()
    {
        _loading = true;
        _errorMessage = null;
        try
        {
            _user = await UserService.GetCurrentUserAsync();
            if (_user != null)
            {
                _profileImagePreview = _user.ProfileImageUri;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading user: {ex.Message}";
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
        _errorMessage = null;
        _successMessage = null;

        try
        {
            Guid? profilePictureStorageId = null;

            // Upload a new profile picture if one was selected
            if (_selectedProfileImageData != null)
            {
                var uploadResult = await ImageStorageService.UploadAndCreateRecordFromBytesAsync(_selectedProfileImageData);
                if (uploadResult?.Success == true)
                {
                    profilePictureStorageId = uploadResult.StorageObjectId;
                }
                else
                {
                    _errorMessage = uploadResult?.ErrorMessage ?? "Failed to upload profile picture.";
                    _isSaving = false;
                    return;
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
                _successMessage = "Settings saved successfully";
                await LoadUser();
            }
            else
            {
                _errorMessage = result?.Message ?? "Failed to save settings";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error saving settings: {ex.Message}";
        }
        finally
        {
            _isSaving = false;
        }
    }
}
