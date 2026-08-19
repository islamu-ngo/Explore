// ABOUTME: Unit tests for storage object metadata update authorization and handling.
// ABOUTME: Verifies route-owned identity, tenant isolation, and provider-owned field preservation.

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
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public StorageObjectCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task UpdateCommand_UsesRouteOwnedIdentityAsAuthorizationContext()
    {
        var storageObjectId = Guid.CreateVersion7();

        ISecureRequest command = new UpdateStorageObjectCommand
        {
            StorageObjectId = storageObjectId,
            StorageObjectDto = CreateUpdateDto()
        };

        await Assert.That(command.ResourceId).IsEqualTo(storageObjectId.ToString("D"));
        await Assert.That(command.AuthorizationFacts).IsNull();
    }

    [Test]
    public async Task UpdateHandle_AppliesEditableGroupsAndPreservesProviderOwnedFields()
    {
        var storageObjectId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var owningResourceId = Guid.CreateVersion7();
        var entity = CreateStorageObject(storageObjectId, _tenantId);
        var dto = CreateUpdateDto(
            actorId: actorId,
            owningResourceKind: "islamuevent_event",
            owningResourceId: owningResourceId,
            safeDisplayName: null);

        _actorRepository.Exists(actorId).Returns(true);
        _storageObjectRepository.GetById(storageObjectId).Returns(entity);

        var result = await CreateUpdateHandler().Handle(
            new UpdateStorageObjectCommand
            {
                StorageObjectId = storageObjectId,
                StorageObjectDto = dto
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(storageObjectId);
        await Assert.That(entity.TenantId).IsEqualTo(_tenantId);
        await Assert.That(entity.Uri).IsEqualTo("/api/storageobject/018f0000-0000-7000-8000-000000000001/content");
        await Assert.That(entity.ObjectKey).IsEqualTo("tenants/current/file.png");
        await Assert.That(entity.Provider).IsEqualTo(StorageProviders.Local);
        await Assert.That(entity.FullName).IsEqualTo(dto.Metadata!.FullName);
        await Assert.That(entity.SafeDisplayName).IsEqualTo(dto.Metadata.FullName);
        await Assert.That(entity.Extension).IsEqualTo("png");
        await Assert.That(entity.ContentType).IsEqualTo("image/png");
        await Assert.That(entity.Size).IsEqualTo(1024);
        await Assert.That(entity.Visibility).IsEqualTo(dto.Access!.Visibility);
        await Assert.That(entity.Purpose).IsEqualTo(dto.Access.Purpose);
        await Assert.That(entity.LifecycleState).IsEqualTo(StorageObjectLifecycleStates.Active);
        await Assert.That(entity.OwningResourceKind).IsEqualTo(dto.Ownership!.OwningResourceKind);
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
                StorageObjectId = storageObjectId,
                StorageObjectDto = CreateUpdateDto()
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Storage object not found.");
        await _storageObjectRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task UpdateContract_DoesNotExposeByteIdentityFields()
    {
        string[] forbiddenProperties = ["FileTypeId", "Extension", "ContentType", "Size", "Sha256Checksum", "ObjectKey", "Provider"];
        var properties = typeof(StorageObjectMetadataUpdateDto).GetProperties().Select(property => property.Name);

        foreach (string property in forbiddenProperties)
        {
            await Assert.That(properties).DoesNotContain(property);
        }
    }

    [Test]
    public async Task UpdateHandle_WhenMergedAccessWouldPromoteUnsafeBytes_ReturnsFailureWithoutUpdate()
    {
        var storageObjectId = Guid.CreateVersion7();
        var entity = CreateStorageObject(storageObjectId, _tenantId);
        entity.ContentType = "application/pdf";
        entity.Extension = "pdf";
        entity.Purpose = StorageObjectPurposes.Document;
        entity.Visibility = StorageObjectVisibilities.PrivateOwner;
        _storageObjectRepository.GetById(storageObjectId).Returns(entity);

        var result = await CreateUpdateHandler().Handle(
            new UpdateStorageObjectCommand
            {
                StorageObjectId = storageObjectId,
                StorageObjectDto = new UpdateStorageObjectDto
                {
                    Access = new StorageObjectAccessUpdateDto
                    {
                        Visibility = StorageObjectVisibilities.PublicImage,
                        Purpose = StorageObjectPurposes.EventImage
                    }
                }
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _storageObjectRepository.DidNotReceive().Update(Arg.Any<StorageObject>());
    }

    private UpdateStorageObjectCommandHandler CreateUpdateHandler()
        => new(_storageObjectRepository, _actorRepository, _tenantContext);

    private static UpdateStorageObjectDto CreateUpdateDto(
        Guid? actorId = null,
        string? owningResourceKind = null,
        Guid? owningResourceId = null,
        string? safeDisplayName = "updated-safe-name.pdf") =>
        new()
        {
            Metadata = new StorageObjectMetadataUpdateDto
            {
                FullName = "updated-file.pdf",
                SafeDisplayName = safeDisplayName
            },
            Access = new StorageObjectAccessUpdateDto
            {
                Visibility = StorageObjectVisibilities.AuthenticatedTenant,
                Purpose = StorageObjectPurposes.Document
            },
            Ownership = new StorageObjectOwnershipUpdateDto
            {
                OwningResourceKind = owningResourceKind,
                OwningResourceId = owningResourceId,
                ActorId = actorId
            }
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
