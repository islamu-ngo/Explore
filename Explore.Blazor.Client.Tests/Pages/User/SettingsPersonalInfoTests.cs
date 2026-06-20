// ABOUTME: Component tests for account personal information saves and profile image uploads.
// ABOUTME: Protects API-client DTO compatibility for username and ProfilePictureId persistence.

using System.Reflection;
using Explore.Blazor.Client.Pages.User.Components;

namespace Explore.Blazor.Client.Tests.Pages.User;

public sealed class SettingsPersonalInfoTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IImageStorageService _imageStorageService = Substitute.For<IImageStorageService>();

    public SettingsPersonalInfoTests()
    {
        _ctx.Services.AddSingleton(_userService);
        _ctx.Services.AddSingleton(_imageStorageService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task SaveSettings_UploadsSelectedProfileImageAndSendsRequiredUsername()
    {
        var userId = Guid.NewGuid();
        var uploadedImageId = Guid.NewGuid();
        var fileData = new FileUploadData
        {
            Content = [1, 2, 3],
            FileName = "avatar.png",
            ContentType = "image/png"
        };
        UpdateUserDto? capturedUpdate = null;

        _userService.GetCurrentUserAsync().Returns(new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Email = "amina.rahman@example.test",
            Username = null
        });
        _imageStorageService.GenerateLocalPreviewFromBytes(fileData)
            .Returns("data:image/png;base64,AQID");
        _imageStorageService.UploadAndCreateRecordFromBytesAsync(fileData)
            .Returns(new ImageUploadResult
            {
                Success = true,
                StorageObjectId = uploadedImageId,
                ViewUrl = "/api/storageobject/avatar/public"
            });
        _userService.UpdateUserAsync(Arg.Do<UpdateUserDto>(dto => capturedUpdate = dto))
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = true,
                Id = userId,
                Message = "User updated successfully"
            });

        var cut = _ctx.RenderMudComponent<SettingsPersonalInfo>();
        cut.WaitForState(() => cut.Markup.Contains("Save Changes", StringComparison.Ordinal), TimeSpan.FromSeconds(3));

        await cut.InvokeAsync(() => InvokeFileSelectedAsync(cut.Instance, fileData));
        await cut.InvokeAsync(() => InvokeSaveSettingsAsync(cut.Instance));

        await _imageStorageService.Received(1).UploadAndCreateRecordFromBytesAsync(fileData);
        await _userService.Received(1).UpdateUserAsync(Arg.Any<UpdateUserDto>());
        await Assert.That(capturedUpdate).IsNotNull();
        await Assert.That(capturedUpdate!.Id).IsEqualTo(userId);
        await Assert.That(capturedUpdate.Username).IsEqualTo("amina.rahman");
        await Assert.That(capturedUpdate.ProfilePictureId).IsEqualTo(uploadedImageId);
    }

    private static Task InvokeFileSelectedAsync(SettingsPersonalInfo component, FileUploadData fileData)
    {
        var method = typeof(SettingsPersonalInfo).GetMethod(
            "OnProfileImageFileDataSelected",
            BindingFlags.Instance | BindingFlags.NonPublic);

        return (Task)method!.Invoke(component, [fileData])!;
    }

    private static Task InvokeSaveSettingsAsync(SettingsPersonalInfo component)
    {
        var method = typeof(SettingsPersonalInfo).GetMethod(
            "SaveSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);

        return (Task)method!.Invoke(component, null)!;
    }
}
