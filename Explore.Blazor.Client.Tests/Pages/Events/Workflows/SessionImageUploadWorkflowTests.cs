// ABOUTME: Unit tests for the shared session image upload workflow used by event session editing UI.
// ABOUTME: Verifies validation, preview mutation, upload success, and event-image reset behavior.

using Explore.Blazor.Client.Pages.Events.Models;
using Explore.Blazor.Client.Pages.Events.Workflows;
using Microsoft.AspNetCore.Components.Forms;

namespace Explore.Blazor.Client.Tests.Pages.Events.Workflows;

public class SessionImageUploadWorkflowTests
{
    [Test]
    public async Task UploadAsync_WithUnsupportedContentType_ReturnsValidationError()
    {
        var workflow = new SessionImageUploadWorkflow();
        var session = new SessionEditorModel();
        var imageStorageService = Substitute.For<IImageStorageService>();
        var file = CreateBrowserFile("session.svg", "image/svg+xml", 128);

        var error = await workflow.UploadAsync(session, file, imageStorageService);

        await Assert.That(error).IsEqualTo("Please select a valid image file (JPG, PNG, GIF, or WebP).");
        await imageStorageService.DidNotReceiveWithAnyArgs().ReadFileAsync(default!, default);
    }

    [Test]
    public async Task UploadAsync_WithOversizedFile_ReturnsValidationError()
    {
        var workflow = new SessionImageUploadWorkflow();
        var session = new SessionEditorModel();
        var imageStorageService = Substitute.For<IImageStorageService>();
        var file = CreateBrowserFile("session.png", "image/png", SessionImageUploadWorkflow.MaxFileSize + 1);

        var error = await workflow.UploadAsync(session, file, imageStorageService);

        await Assert.That(error).IsEqualTo("File size must be less than 5MB.");
        await imageStorageService.DidNotReceiveWithAnyArgs().ReadFileAsync(default!, default);
    }

    [Test]
    public async Task UploadAsync_OnSuccessfulUpload_UpdatesSessionImageState()
    {
        var workflow = new SessionImageUploadWorkflow();
        var session = new SessionEditorModel { UseEventImage = true };
        var imageStorageService = Substitute.For<IImageStorageService>();
        var file = CreateBrowserFile("session.png", "image/png", 256);
        var fileData = new FileUploadData
        {
            Content = [1, 2, 3],
            FileName = "session.png",
            ContentType = "image/png"
        };
        var storageObjectId = Guid.NewGuid();

        imageStorageService.ReadFileAsync(file, SessionImageUploadWorkflow.MaxFileSize).Returns(fileData);
        imageStorageService.GenerateLocalPreviewFromBytes(fileData).Returns("data:image/png;base64,abc");
        imageStorageService.UploadAndCreateRecordFromBytesAsync(fileData).Returns(new ImageUploadResult
        {
            Success = true,
            StorageObjectId = storageObjectId
        });

        var error = await workflow.UploadAsync(session, file, imageStorageService);

        await Assert.That(error).IsNull();
        await Assert.That(session.UseEventImage).IsFalse();
        await Assert.That(session.FeaturedImageId).IsEqualTo(storageObjectId);
        await Assert.That(session.FeaturedImagePreviewUrl).IsEqualTo("data:image/png;base64,abc");
        await Assert.That(session.PendingImageBytes).IsEquivalentTo(new byte[] { 1, 2, 3 });
        await Assert.That(session.PendingImageFileName).IsEqualTo("session.png");
    }

    [Test]
    public async Task UploadAsync_OnUploadFailure_ClearsCustomImageState()
    {
        var workflow = new SessionImageUploadWorkflow();
        var session = new SessionEditorModel
        {
            UseEventImage = false,
            FeaturedImagePreviewUrl = "data:image/png;base64,old",
            FeaturedImageId = Guid.NewGuid(),
            PendingImageBytes = [9, 9, 9],
            PendingImageFileName = "old.png"
        };
        var imageStorageService = Substitute.For<IImageStorageService>();
        var file = CreateBrowserFile("session.png", "image/png", 256);
        var fileData = new FileUploadData
        {
            Content = [1, 2, 3],
            FileName = "session.png",
            ContentType = "image/png"
        };

        imageStorageService.ReadFileAsync(file, SessionImageUploadWorkflow.MaxFileSize).Returns(fileData);
        imageStorageService.GenerateLocalPreviewFromBytes(fileData).Returns("data:image/png;base64,new");
        imageStorageService.UploadAndCreateRecordFromBytesAsync(fileData).Returns(new ImageUploadResult
        {
            Success = false,
            ErrorMessage = "Upload failed."
        });

        var error = await workflow.UploadAsync(session, file, imageStorageService);

        await Assert.That(error).IsEqualTo("Upload failed.");
        await Assert.That(session.UseEventImage).IsFalse();
        await Assert.That(session.FeaturedImageId).IsNull();
        await Assert.That(session.FeaturedImagePreviewUrl).IsNull();
        await Assert.That(session.PendingImageBytes).IsNull();
        await Assert.That(session.PendingImageFileName).IsNull();
    }

    [Test]
    public async Task UseEventImage_ResetsCustomImageState_AndRestoresEventPreview()
    {
        var workflow = new SessionImageUploadWorkflow();
        var session = new SessionEditorModel
        {
            UseEventImage = false,
            FeaturedImageId = Guid.NewGuid(),
            FeaturedImagePreviewUrl = "data:image/png;base64,custom",
            PendingImageBytes = [4, 5, 6],
            PendingImageFileName = "custom.png"
        };

        workflow.UseEventImage(session, "https://example.com/event.png");

        await Assert.That(session.UseEventImage).IsTrue();
        await Assert.That(session.FeaturedImageId).IsNull();
        await Assert.That(session.FeaturedImagePreviewUrl).IsEqualTo("https://example.com/event.png");
        await Assert.That(session.PendingImageBytes).IsNull();
        await Assert.That(session.PendingImageFileName).IsNull();
    }

    private static IBrowserFile CreateBrowserFile(string name, string contentType, long size)
    {
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns(name);
        file.ContentType.Returns(contentType);
        file.Size.Returns(size);
        return file;
    }
}
