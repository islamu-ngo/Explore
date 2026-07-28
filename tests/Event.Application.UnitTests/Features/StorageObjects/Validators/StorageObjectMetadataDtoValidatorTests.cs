// ABOUTME: Unit tests for storage object metadata create/update validation rules.
// ABOUTME: Verifies object keys, filenames, extensions, MIME hints, checksum, and ownership metadata are bounded.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.StorageObject.Validators;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.StorageObjects.Validators;

public sealed class StorageObjectMetadataDtoValidatorTests
{
    private readonly IFileTypeRepository _fileTypeRepository = Substitute.For<IFileTypeRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();

    public StorageObjectMetadataDtoValidatorTests()
    {
        _fileTypeRepository.Exists(1).Returns(true);
    }

    [Test]
    public async Task CreateValidate_WithValidMetadata_IsValid()
    {
        var result = await CreateValidator().ValidateAsync(CreateDto());

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task UpdateValidate_WithValidMetadata_IsValid()
    {
        var result = await UpdateValidator().ValidateAsync(UpdateDto());

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [Arguments("../tenant/file.png")]
    [Arguments("tenant/../file.png")]
    [Arguments("tenant/./file.png")]
    [Arguments("/tenant/file.png")]
    [Arguments("tenant//file.png")]
    [Arguments("tenant\\file.png")]
    [Arguments("tenant/file.png\u0000")]
    public async Task CreateValidate_WithUnsafeObjectKey_IsInvalid(string objectKey)
    {
        var result = await CreateValidator().ValidateAsync(CreateDto(objectKey: objectKey));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(CreateStorageObjectDto.ObjectKey))).IsTrue();
    }

    [Test]
    [Arguments("../file.png")]
    [Arguments("folder/file.png")]
    [Arguments("folder\\file.png")]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("file\u0000.png")]
    [Arguments("CON")]
    public async Task CreateValidate_WithUnsafeFullName_IsInvalid(string fullName)
    {
        var result = await CreateValidator().ValidateAsync(CreateDto(fullName: fullName));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(CreateStorageObjectDto.FullName))).IsTrue();
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
    [Arguments("../png")]
    [Arguments("folder/png")]
    [Arguments("png\u0000")]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("png.exe")]
    [Arguments("*")]
    public async Task CreateValidate_WithUnsafeExtension_IsInvalid(string extension)
    {
        var result = await CreateValidator().ValidateAsync(CreateDto(extension: extension));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(CreateStorageObjectDto.Extension))).IsTrue();
    }

    [Test]
    [Arguments("not-a-media-type")]
    [Arguments("image/*")]
    [Arguments("*/png")]
    [Arguments("application/pdf/extra")]
    [Arguments("text/plain\u0000")]
    public async Task UpdateValidate_WithMalformedOrWildcardContentType_IsInvalid(string contentType)
    {
        var result = await UpdateValidator().ValidateAsync(UpdateDto(contentType: contentType));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName.EndsWith(nameof(StorageObjectMetadataUpdateDto.ContentType), StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task CreateValidate_WithNonHexSha256Checksum_IsInvalid()
    {
        var result = await CreateValidator().ValidateAsync(CreateDto(sha256Checksum: new string('z', 64)));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(CreateStorageObjectDto.Sha256Checksum))).IsTrue();
    }

    [Test]
    public async Task UpdateValidate_WithPartialOwningResourceMetadata_IsInvalid()
    {
        var result = await UpdateValidator().ValidateAsync(UpdateDto(owningResourceKind: "event"));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName.EndsWith(nameof(StorageObjectOwnershipUpdateDto.OwningResourceId), StringComparison.Ordinal))).IsTrue();
    }

    private CreateStorageObjectDtoValidator CreateValidator()
        => new(_fileTypeRepository, _actorRepository);

    private UpdateStorageObjectDtoValidator UpdateValidator()
        => new(_fileTypeRepository, _actorRepository);

    private static CreateStorageObjectDto CreateDto(
        string uri = "/api/storageobject/018f0000-0000-7000-8000-000000000001/content",
        string? objectKey = "tenants/default/file.png",
        string fullName = "file.png",
        string? safeDisplayName = "file.png",
        string extension = "png",
        string? contentType = "image/png",
        string? sha256Checksum = null,
        string? owningResourceKind = null,
        Guid? owningResourceId = null) =>
        new()
        {
            FileTypeId = 1,
            Uri = uri,
            ObjectKey = objectKey,
            Provider = StorageProviders.Local,
            FullName = fullName,
            SafeDisplayName = safeDisplayName,
            Extension = extension,
            ContentType = contentType,
            Sha256Checksum = sha256Checksum,
            Size = 1024,
            Visibility = StorageObjectVisibilities.PublicImage,
            Purpose = StorageObjectPurposes.LegacyImage,
            LifecycleState = StorageObjectLifecycleStates.Active,
            OwningResourceKind = owningResourceKind,
            OwningResourceId = owningResourceId
        };

    private static UpdateStorageObjectDto UpdateDto(
        string fullName = "file.png",
        string? safeDisplayName = "file.png",
        string extension = "png",
        string? contentType = "image/png",
        string? owningResourceKind = null,
        Guid? owningResourceId = null) =>
        new()
        {
            Metadata = new StorageObjectMetadataUpdateDto
            {
                FileTypeId = 1,
                FullName = fullName,
                SafeDisplayName = safeDisplayName,
                Extension = extension,
                ContentType = contentType
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
