// ABOUTME: Unit tests for upload-session reservation metadata validation rules.
// ABOUTME: Verifies unsafe filenames, display names, extensions, and MIME hints are rejected early.

using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.StorageObject.Validators;
using Explore.Domain;

namespace Event.Application.UnitTests.Features.StorageObjects.Validators;

public sealed class CreateStorageUploadSessionDtoValidatorTests
{
    private readonly CreateStorageUploadSessionDtoValidator _validator = new();

    [Test]
    public async Task Validate_WithValidUploadSessionMetadata_IsValid()
    {
        var result = await _validator.ValidateAsync(CreateDto());

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [Arguments("not-a-media-type")]
    [Arguments("image/*")]
    [Arguments("*/png")]
    [Arguments("text/plain\u0000")]
    public async Task Validate_WithMalformedOrWildcardContentType_IsInvalid(string contentType)
    {
        var result = await _validator.ValidateAsync(CreateDto(contentType: contentType));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(CreateStorageUploadSessionDto.ContentType))).IsTrue();
    }

    [Test]
    [Arguments("../secret.pdf")]
    [Arguments("folder/report.pdf")]
    [Arguments("folder\\report.pdf")]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("report\u0000.pdf")]
    [Arguments("CON")]
    public async Task Validate_WithUnsafeOriginalFileName_IsInvalid(string originalFileName)
    {
        var result = await _validator.ValidateAsync(CreateDto(originalFileName: originalFileName));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(CreateStorageUploadSessionDto.OriginalFileName))).IsTrue();
    }

    [Test]
    [Arguments("../secret.pdf")]
    [Arguments("folder/report.pdf")]
    [Arguments("folder\\report.pdf")]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("display\u0000.pdf")]
    [Arguments("NUL.txt")]
    public async Task Validate_WithUnsafeSafeDisplayName_IsInvalid(string safeDisplayName)
    {
        var result = await _validator.ValidateAsync(CreateDto(safeDisplayName: safeDisplayName));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(CreateStorageUploadSessionDto.SafeDisplayName))).IsTrue();
    }

    [Test]
    [Arguments("../pdf")]
    [Arguments("folder/pdf")]
    [Arguments("pdf\u0000")]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("pdf.exe")]
    [Arguments("*")]
    public async Task Validate_WithUnsafeExtension_IsInvalid(string extension)
    {
        var result = await _validator.ValidateAsync(CreateDto(extension: extension));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(CreateStorageUploadSessionDto.Extension))).IsTrue();
    }

    private static CreateStorageUploadSessionDto CreateDto(
        string contentType = "application/pdf",
        string? originalFileName = "report.pdf",
        string? safeDisplayName = "Quarterly report.pdf",
        string? extension = "pdf") =>
        new()
        {
            ExpectedSizeBytes = 42,
            ContentType = contentType,
            OriginalFileName = originalFileName,
            SafeDisplayName = safeDisplayName,
            Extension = extension,
            Purpose = StorageObjectPurposes.Document,
            Visibility = StorageObjectVisibilities.PrivateOwner,
            IdempotencyKey = "upload-session-test"
        };
}
