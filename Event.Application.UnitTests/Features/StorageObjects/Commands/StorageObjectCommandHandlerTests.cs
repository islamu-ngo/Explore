// ABOUTME: Unit tests for storage object metadata command authorization and update handling.
// ABOUTME: Verifies client-controlled tenant fields cannot drive authorization context or persistence writes.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Handlers.Commands;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.StorageObjects.Commands;

public sealed class StorageObjectCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly IFileTypeRepository _fileTypeRepository = Substitute.For<IFileTypeRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public StorageObjectCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _fileTypeRepository.Exists(1).Returns(true);
    }

    [Test]
    public async Task CreateCommand_DoesNotExposeClientTenantIdAsAuthorizationContext()
    {
        ISecureRequest command = new CreateStorageObjectCommand
        {
            StorageObjectDto = CreateCreateDto(tenantId: Guid.CreateVersion7())
        };

        await Assert.That(command.ResourceId).IsNull();
        await Assert.That(command.ResourceAttributes).IsNotNull();
        await Assert.That(command.ResourceAttributes!.ContainsKey("tenantId")).IsFalse();
        await Assert.That(command.ResourceAttributes["authorizationScope"]).IsEqualTo("collection");
    }

    [Test]
    public async Task UpdateCommand_DoesNotExposeClientTenantIdAsAuthorizationContext()
    {
        var storageObjectId = Guid.CreateVersion7();

        ISecureRequest command = new UpdateStorageObjectCommand
        {
            StorageObjectDto = CreateUpdateDto(storageObjectId, tenantId: Guid.CreateVersion7())
        };

        await Assert.That(command.ResourceId).IsEqualTo(storageObjectId.ToString("D"));
        await Assert.That(command.ResourceAttributes).IsNull();
    }

    [Test]
    public async Task UpdateHandle_WhenDtoTenantIdDiffers_PreservesPersistedTenantAndAppliesAllowedFields()
    {
        var storageObjectId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var owningResourceId = Guid.CreateVersion7();
        var entity = CreateStorageObject(storageObjectId, _tenantId);
        var dto = CreateUpdateDto(
            storageObjectId,
            tenantId: Guid.CreateVersion7(),
            actorId: actorId,
            owningResourceKind: "islamuevent_event",
            owningResourceId: owningResourceId,
            safeDisplayName: null);

        _actorRepository.Exists(actorId).Returns(true);
        _storageObjectRepository.GetById(storageObjectId).Returns(entity);

        var result = await CreateUpdateHandler().Handle(
            new UpdateStorageObjectCommand { StorageObjectDto = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(storageObjectId);
        await Assert.That(entity.TenantId).IsEqualTo(_tenantId);
        await Assert.That(entity.Uri).IsEqualTo(dto.Uri);
        await Assert.That(entity.ObjectKey).IsEqualTo(dto.ObjectKey);
        await Assert.That(entity.Provider).IsEqualTo(dto.Provider);
        await Assert.That(entity.FullName).IsEqualTo(dto.FullName);
        await Assert.That(entity.SafeDisplayName).IsEqualTo(dto.FullName);
        await Assert.That(entity.Extension).IsEqualTo(dto.Extension);
        await Assert.That(entity.ContentType).IsEqualTo(dto.ContentType);
        await Assert.That(entity.Sha256Checksum).IsEqualTo(dto.Sha256Checksum);
        await Assert.That(entity.Size).IsEqualTo(dto.Size);
        await Assert.That(entity.Visibility).IsEqualTo(dto.Visibility);
        await Assert.That(entity.Purpose).IsEqualTo(dto.Purpose);
        await Assert.That(entity.LifecycleState).IsEqualTo(dto.LifecycleState);
        await Assert.That(entity.OwningResourceKind).IsEqualTo(dto.OwningResourceKind);
        await Assert.That(entity.OwningResourceId).IsEqualTo(owningResourceId);
        await Assert.That(entity.ActorId).IsEqualTo(actorId);

        await _storageObjectRepository.Received(1).Update(Arg.Is<StorageObject>(storageObject =>
            ReferenceEquals(storageObject, entity) &&
            storageObject.TenantId == _tenantId));
    }

    [Test]
    public async Task UpdateHandle_WhenStorageObjectIsOutsideCurrentTenant_ReturnsFailureWithoutUpdate()
    {
        var storageObjectId = Guid.CreateVersion7();
        var entity = CreateStorageObject(storageObjectId, tenantId: Guid.CreateVersion7());

        _storageObjectRepository.GetById(storageObjectId).Returns(entity);

        var result = await CreateUpdateHandler().Handle(
            new UpdateStorageObjectCommand
            {
                StorageObjectDto = CreateUpdateDto(storageObjectId, tenantId: _tenantId)
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Storage object not found.");
        await _storageObjectRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    private UpdateStorageObjectCommandHandler CreateUpdateHandler()
        => new(_storageObjectRepository, _fileTypeRepository, _actorRepository, _tenantContext);

    private static CreateStorageObjectDto CreateCreateDto(Guid tenantId) =>
        new()
        {
            FileTypeId = 1,
            Uri = "/api/storageobject/018f0000-0000-7000-8000-000000000001/content",
            ObjectKey = "tenants/current/file.png",
            Provider = StorageProviders.Local,
            FullName = "file.png",
            SafeDisplayName = "file.png",
            Extension = "png",
            ContentType = "image/png",
            Sha256Checksum = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Size = 1024,
            Visibility = StorageObjectVisibilities.PublicImage,
            Purpose = StorageObjectPurposes.LegacyImage,
            LifecycleState = StorageObjectLifecycleStates.Active,
            TenantId = tenantId
        };

    private static UpdateStorageObjectDto CreateUpdateDto(
        Guid storageObjectId,
        Guid tenantId,
        Guid? actorId = null,
        string? owningResourceKind = null,
        Guid? owningResourceId = null,
        string? safeDisplayName = "updated-safe-name.pdf") =>
        new()
        {
            Id = storageObjectId,
            FileTypeId = 1,
            Uri = "/api/storageobject/018f0000-0000-7000-8000-000000000002/content",
            ObjectKey = "tenants/current/updated-file.pdf",
            Provider = StorageProviders.Local,
            FullName = "updated-file.pdf",
            SafeDisplayName = safeDisplayName,
            Extension = "pdf",
            ContentType = "application/pdf",
            Sha256Checksum = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            Size = 2048,
            Visibility = StorageObjectVisibilities.AuthenticatedTenant,
            Purpose = StorageObjectPurposes.Document,
            LifecycleState = StorageObjectLifecycleStates.Active,
            OwningResourceKind = owningResourceKind,
            OwningResourceId = owningResourceId,
            TenantId = tenantId,
            ActorId = actorId
        };

    private static StorageObject CreateStorageObject(Guid storageObjectId, Guid tenantId) =>
        new()
        {
            Id = storageObjectId,
            FileTypeId = 1,
            FileType = null!,
            Uri = "/api/storageobject/018f0000-0000-7000-8000-000000000001/content",
            ObjectKey = "tenants/current/file.png",
            Provider = StorageProviders.Local,
            FullName = "file.png",
            SafeDisplayName = "file.png",
            Extension = "png",
            ContentType = "image/png",
            Sha256Checksum = null,
            Size = 1024,
            Visibility = StorageObjectVisibilities.PublicImage,
            Purpose = StorageObjectPurposes.LegacyImage,
            LifecycleState = StorageObjectLifecycleStates.Active,
            TenantId = tenantId,
            Tenant = null!
        };
}
