// ABOUTME: Shared workflow for validating, previewing, and uploading session images.
// ABOUTME: Mutates SessionEditorModel image state so event session UI can reuse one upload path.

using Explore.Blazor.Client.Pages.Events.Models;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace Explore.Blazor.Client.Pages.Events.Workflows;

public sealed class SessionImageUploadWorkflow
{
    public const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    public void UseEventImage(SessionEditorModel session, string? eventImageUrl = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        session.UseEventImage = true;
        session.FeaturedImageId = null;
        session.FeaturedImagePreviewUrl = eventImageUrl;
        session.PendingImageBytes = null;
        session.PendingImageFileName = null;
    }

    public async Task<string?> UploadAsync(
        SessionEditorModel session,
        IBrowserFile? file,
        IImageStorageService imageStorageService,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(imageStorageService);

        cancellationToken.ThrowIfCancellationRequested();

        if (file is null)
        {
            return null;
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return "Please select a valid image file (JPG, PNG, GIF, or WebP).";
        }

        if (file.Size > MaxFileSize)
        {
            return "File size must be less than 5MB.";
        }

        var fileData = await imageStorageService.ReadFileAsync(file, MaxFileSize);
        if (fileData is null)
        {
            ClearCustomImage(session);
            return "Failed to read the file.";
        }

        session.PendingImageBytes = fileData.Content;
        session.PendingImageFileName = fileData.FileName;

        var preview = imageStorageService.GenerateLocalPreviewFromBytes(fileData);
        if (!string.IsNullOrWhiteSpace(preview))
        {
            session.FeaturedImagePreviewUrl = preview;
        }

        var uploadResult = await imageStorageService.UploadAndCreateRecordFromBytesAsync(fileData);
        if (uploadResult?.Success == true)
        {
            session.FeaturedImageId = uploadResult.StorageObjectId;
            session.UseEventImage = false;
            return null;
        }

        ClearCustomImage(session);
        return uploadResult?.ErrorMessage ?? "Failed to upload image.";
    }

    private static void ClearCustomImage(SessionEditorModel session)
    {
        session.FeaturedImageId = null;
        session.FeaturedImagePreviewUrl = null;
        session.PendingImageBytes = null;
        session.PendingImageFileName = null;
    }
}
