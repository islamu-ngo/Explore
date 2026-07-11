// ABOUTME: Unit tests for legacy presigned upload request validation rules.
// ABOUTME: Verifies unsafe filenames and malformed MIME types are rejected before storage use.

using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.StorageObject.Validators;

namespace Event.Application.UnitTests.Features.StorageObjects.Validators;

public sealed class UploadRequestDtoValidatorTests
{
    private readonly UploadRequestDtoValidator _validator = new();

    [Test]
    public async Task Validate_WithSimpleFileNameAndMimeType_IsValid()
    {
        var result = await _validator.ValidateAsync(new UploadRequestDto
        {
            FileName = "report.pdf",
            ContentType = "application/pdf"
        });

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [Arguments("../secret.pdf")]
    [Arguments("folder/report.pdf")]
    [Arguments("folder\\report.pdf")]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("report\u0000.pdf")]
    [Arguments("CON")]
    public async Task Validate_WithUnsafeFileName_IsInvalid(string fileName)
    {
        var result = await _validator.ValidateAsync(new UploadRequestDto
        {
            FileName = fileName,
            ContentType = "application/pdf"
        });

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error => error.PropertyName == nameof(UploadRequestDto.FileName))).IsTrue();
    }

    [Test]
    [Arguments("not-a-media-type")]
    [Arguments("image/*")]
    [Arguments("*/png")]
    [Arguments("application/pdf/extra")]
    [Arguments("text/plain\u0000")]
    public async Task Validate_WithMalformedOrWildcardContentType_IsInvalid(string contentType)
    {
        var result = await _validator.ValidateAsync(new UploadRequestDto
        {
            FileName = "report.pdf",
            ContentType = contentType
        });

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error => error.PropertyName == nameof(UploadRequestDto.ContentType))).IsTrue();
    }
}
