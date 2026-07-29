// ABOUTME: Unit tests for storage object metadata update validation rules.
// ABOUTME: Verifies display names and ownership metadata remain bounded.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.StorageObject.Validators;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.StorageObjects.Validators;

public sealed class StorageObjectMetadataDtoValidatorTests
{
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();

    [Test]
    public async Task UpdateValidate_WithValidMetadata_IsValid()
    {
        var result = await UpdateValidator().ValidateAsync(UpdateDto());

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [Arguments("../file.png")]
    [Arguments("folder/file.png")]
    [Arguments("folder\\file.png")]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("display\u0000.png")]
    [Arguments("NUL.txt")]
    public async Task UpdateValidate_WithUnsafeSafeDisplayName_IsInvalid(string safeDisplayName)
    {
        var result = await UpdateValidator().ValidateAsync(UpdateDto(safeDisplayName: safeDisplayName));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName.EndsWith(nameof(StorageObjectMetadataUpdateDto.SafeDisplayName), StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task UpdateContract_DoesNotExposeContentType()
    {
        var property = typeof(StorageObjectMetadataUpdateDto).GetProperty("ContentType");

        await Assert.That(property).IsNull();
    }

    [Test]
    public async Task UpdateValidate_WithPartialOwningResourceMetadata_IsInvalid()
    {
        var result = await UpdateValidator().ValidateAsync(UpdateDto(owningResourceKind: "event"));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName.EndsWith(nameof(StorageObjectOwnershipUpdateDto.OwningResourceId), StringComparison.Ordinal))).IsTrue();
    }

    private UpdateStorageObjectDtoValidator UpdateValidator()
        => new(_actorRepository);

    private static UpdateStorageObjectDto UpdateDto(
        string fullName = "file.png",
        string? safeDisplayName = "file.png",
        string? owningResourceKind = null,
        Guid? owningResourceId = null) =>
        new()
        {
            Metadata = new StorageObjectMetadataUpdateDto
            {
                FullName = fullName,
                SafeDisplayName = safeDisplayName
            },
            Access = new StorageObjectAccessUpdateDto
            {
                Visibility = StorageObjectVisibilities.PublicImage,
                Purpose = StorageObjectPurposes.LegacyImage
            },
            Ownership = new StorageObjectOwnershipUpdateDto
            {
                OwningResourceKind = owningResourceKind,
                OwningResourceId = owningResourceId
            }
        };
}
