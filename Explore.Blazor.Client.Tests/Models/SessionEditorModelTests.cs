// ABOUTME: Unit tests for SessionEditorModel — Clone, ToCreateDto, ToUpdateDto, FromDto.
// ABOUTME: Validates session duplication logic and DTO conversion correctness.

using Explore.Blazor.Client.Pages.Events.Models;

namespace Explore.Blazor.Client.Tests.Models;

public class SessionEditorModelTests
{
    [Test]
    public async Task Clone_ShouldCopyAllFieldsExceptId()
    {
        // Arrange
        var original = new SessionEditorModel
        {
            Id = Guid.NewGuid(),
            Title = "Original Session",
            Description = "Description",
            StartTime = new DateTime(2025, 6, 15, 10, 0, 0),
            EndTime = new DateTime(2025, 6, 15, 12, 0, 0),
            LocationId = Guid.NewGuid(),
            MaxAudienceAttendees = 100,
            RegistrationModeId = 2,
            LanguageIds = new HashSet<int> { 1, 3 },
            FeaturedImageId = Guid.NewGuid(),
            FeaturedImagePreviewUrl = "https://example.com/image.jpg",
            UseEventImage = false
        };

        // Act
        var clone = original.Clone();

        // Assert
        await Assert.That(clone.Id).IsNull();
        await Assert.That(clone.Title).IsEqualTo("Original Session (Copy)");
        await Assert.That(clone.Description).IsEqualTo("Description");
        await Assert.That(clone.StartTime).IsEqualTo(original.StartTime.AddDays(1));
        await Assert.That(clone.EndTime).IsEqualTo(original.EndTime.AddDays(1));
        await Assert.That(clone.LocationId).IsEqualTo(original.LocationId);
        await Assert.That(clone.MaxAudienceAttendees).IsEqualTo(100);
        await Assert.That(clone.RegistrationModeId).IsEqualTo(2);
        await Assert.That(clone.LanguageIds).IsNotNull();
        await Assert.That(clone.LanguageIds!.Count).IsEqualTo(2);
        // Clone resets image to event image (UseEventImage = true)
        await Assert.That(clone.UseEventImage).IsTrue();
        await Assert.That(clone.FeaturedImageId).IsNull();
        await Assert.That(clone.PendingImageBytes).IsNull();
        await Assert.That(clone.PendingImageFileName).IsNull();
    }

    [Test]
    public async Task Clone_WithNullTitle_ShouldNotAppendCopy()
    {
        // Arrange
        var original = new SessionEditorModel
        {
            Title = null,
            StartTime = new DateTime(2025, 6, 15, 10, 0, 0),
            EndTime = new DateTime(2025, 6, 15, 12, 0, 0)
        };

        // Act
        var clone = original.Clone();

        // Assert
        await Assert.That(clone.Title).IsNull();
    }

    [Test]
    public async Task Clone_ShouldNotShareLanguageIdReference()
    {
        // Arrange
        var original = new SessionEditorModel
        {
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(2),
            LanguageIds = new HashSet<int> { 1, 2 }
        };

        // Act
        var clone = original.Clone();
        var cloneSet = (HashSet<int>)clone.LanguageIds;
        cloneSet.Add(5);

        // Assert — original should not be affected
        await Assert.That(original.LanguageIds.Count).IsEqualTo(2);
        await Assert.That(clone.LanguageIds.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ToCreateDto_ShouldMapAllFields()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var model = new SessionEditorModel
        {
            Title = "Test Session",
            Description = "Test Desc",
            StartTime = new DateTime(2025, 7, 1, 14, 0, 0),
            EndTime = new DateTime(2025, 7, 1, 16, 0, 0),
            LocationId = locationId,
            MaxAudienceAttendees = 50,
            RegistrationModeId = 3,
            LanguageIds = new HashSet<int> { 2, 4 },
            FeaturedImageId = imageId,
            UseEventImage = false
        };

        // Act
        var dto = model.ToCreateDto(eventId, tenantId);

        // Assert
        await Assert.That(dto.EventId).IsEqualTo(eventId);
        await Assert.That(dto.TenantId).IsEqualTo(tenantId);
        await Assert.That(dto.Title).IsEqualTo("Test Session");
        await Assert.That(dto.Description).IsEqualTo("Test Desc");
        await Assert.That(dto.LocationId).IsEqualTo(locationId);
        await Assert.That(dto.MaxAudienceAttendees).IsEqualTo(50);
        await Assert.That(dto.RegistrationModeId).IsEqualTo(3);
        await Assert.That(dto.FeaturedImageId).IsEqualTo(imageId);
    }

    [Test]
    public async Task ToCreateDto_WithUseEventImage_ShouldNotSetFeaturedImageId()
    {
        // Arrange
        var model = new SessionEditorModel
        {
            StartTime = new DateTime(2025, 7, 1, 14, 0, 0),
            EndTime = new DateTime(2025, 7, 1, 16, 0, 0),
            FeaturedImageId = Guid.NewGuid(),
            UseEventImage = true
        };

        // Act
        var dto = model.ToCreateDto(Guid.NewGuid(), Guid.NewGuid());

        // Assert — when UseEventImage is true, FeaturedImageId should be null
        await Assert.That(dto.FeaturedImageId).IsNull();
    }

    [Test]
    public async Task ToUpdateDto_ShouldMapIdAndEventId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var model = new SessionEditorModel
        {
            Id = sessionId,
            Title = "Updated Session",
            StartTime = new DateTime(2025, 7, 1, 14, 0, 0),
            EndTime = new DateTime(2025, 7, 1, 16, 0, 0),
            FeaturedImageId = imageId,
            UseEventImage = false
        };

        // Act
        var dto = model.ToUpdateDto(eventId);

        // Assert
        await Assert.That(dto.Id).IsEqualTo(sessionId);
        await Assert.That(dto.EventId).IsEqualTo(eventId);
        await Assert.That(dto.Title).IsEqualTo("Updated Session");
        await Assert.That(dto.FeaturedImageId).IsEqualTo(imageId);
    }

    [Test]
    public async Task ToUpdateDto_WithUseEventImage_ShouldClearFeaturedImageId()
    {
        // Arrange
        var model = new SessionEditorModel
        {
            Id = Guid.NewGuid(),
            Title = "Session",
            StartTime = new DateTime(2025, 7, 1, 14, 0, 0),
            EndTime = new DateTime(2025, 7, 1, 16, 0, 0),
            FeaturedImageId = Guid.NewGuid(),
            UseEventImage = true
        };

        // Act
        var dto = model.ToUpdateDto(Guid.NewGuid());

        // Assert
        await Assert.That(dto.FeaturedImageId).IsNull();
    }

    [Test]
    public async Task FromDto_ShouldMapFromSessionDto()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var dto = new EventSessionListDto
        {
            Id = Guid.NewGuid(),
            Title = "From API",
            StartTime = new DateTimeOffset(2025, 8, 10, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2025, 8, 10, 11, 0, 0, TimeSpan.Zero),
            LocationId = Guid.NewGuid(),
            MaxAudienceAttendees = 200,
            RegistrationModeId = 1,
            FeaturedImageId = imageId,
            FeaturedImageUri = "https://cdn.example.com/session-img.jpg"
        };

        // Act
        var model = SessionEditorModel.FromDto(dto);

        // Assert
        await Assert.That(model.Id).IsEqualTo(dto.Id);
        await Assert.That(model.Title).IsEqualTo("From API");
        await Assert.That(model.LocationId).IsEqualTo(dto.LocationId);
        await Assert.That(model.MaxAudienceAttendees).IsEqualTo(200);
        await Assert.That(model.RegistrationModeId).IsEqualTo(1);
        await Assert.That(model.FeaturedImageId).IsEqualTo(imageId);
        await Assert.That(model.FeaturedImagePreviewUrl).IsEqualTo("https://cdn.example.com/session-img.jpg");
        await Assert.That(model.UseEventImage).IsFalse();
    }

    [Test]
    public async Task FromDto_WithNoImage_ShouldDefaultToUseEventImage()
    {
        // Arrange
        var dto = new EventSessionListDto
        {
            Id = Guid.NewGuid(),
            Title = "No Image Session",
            StartTime = new DateTimeOffset(2025, 8, 10, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2025, 8, 10, 11, 0, 0, TimeSpan.Zero),
            FeaturedImageId = null,
            FeaturedImageUri = null
        };

        // Act
        var model = SessionEditorModel.FromDto(dto);

        // Assert
        await Assert.That(model.FeaturedImageId).IsNull();
        await Assert.That(model.FeaturedImagePreviewUrl).IsNull();
        await Assert.That(model.UseEventImage).IsTrue();
    }
}
