// ABOUTME: Unit tests for metadata-driven storage object content reads.
// ABOUTME: Verifies visibility gates and provider-neutral stream opening without caller-supplied keys.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Models.Storage;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.StorageObjects.Queries;

public sealed class StorageObjectContentReaderTests : IDisposable
{
    private readonly Guid _storageObjectId = Guid.CreateVersion7();
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _ownerId = Guid.CreateVersion7();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly IFileStorageProviderResolver _providerResolver = Substitute.For<IFileStorageProviderResolver>();
    private readonly IFileStorageProvider _provider = Substitute.For<IFileStorageProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly BusinessMetrics _metrics = CreateMetrics();

    public StorageObjectContentReaderTests()
    {
        _providerResolver.GetRequired(StorageProviders.Local).Returns(_provider);
        _provider.OpenReadAsync(Arg.Any<FileStorageReadInput>(), Arg.Any<CancellationToken>())
            .Returns(new FileStorageReadResult(
                new MemoryStream([1, 2, 3]),
                "image/png",
                3,
                DateTimeOffset.UtcNow));
    }

    public void Dispose()
    {
        _metrics.Dispose();
    }

    [Test]
    public async Task OpenAsync_WithPublicActiveObject_OpensProviderByMetadataObjectKey()
    {
        var storageObject = CreateStorageObject(StorageObjectVisibilities.PublicImage, createdBy: null);
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        var reader = CreateReader();

        var result = await reader.OpenAsync(_storageObjectId, publicImagesOnly: false, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ContentType).IsEqualTo("image/png");
        await Assert.That(result.SafeDisplayName).IsEqualTo("object.png");
        await Assert.That(result.ShouldDownloadAsAttachment).IsFalse();
        await _provider.Received(1).OpenReadAsync(
            Arg.Is<FileStorageReadInput>(input =>
                input.ObjectKey == storageObject.ObjectKey &&
                input.ContentType == storageObject.ContentType),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OpenAsync_WithAuthenticatedDocument_ReturnsSanitizedAttachmentMetadata()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        var storageObject = CreateStorageObject(StorageObjectVisibilities.AuthenticatedTenant, _ownerId);
        storageObject.ContentType = "application/pdf";
        storageObject.Extension = "pdf";
        storageObject.Purpose = StorageObjectPurposes.Document;
        storageObject.SafeDisplayName = "../unsafe.pdf";
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        var reader = CreateReader();

        var result = await reader.OpenAsync(_storageObjectId, publicImagesOnly: false, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.SafeDisplayName).IsEqualTo("download");
        await Assert.That(result.ShouldDownloadAsAttachment).IsTrue();
        await _provider.Received(1).OpenReadAsync(
            Arg.Any<FileStorageReadInput>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OpenAsync_WhenProviderReturnsDifferentContentType_UsesPersistedMetadataContentType()
    {
        var storageObject = CreateStorageObject(StorageObjectVisibilities.PublicImage, createdBy: null);
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        _provider.OpenReadAsync(Arg.Any<FileStorageReadInput>(), Arg.Any<CancellationToken>())
            .Returns(new FileStorageReadResult(
                new MemoryStream([1, 2, 3]),
                "image/svg+xml",
                3,
                DateTimeOffset.UtcNow));
        var reader = CreateReader();

        var result = await reader.OpenAsync(_storageObjectId, publicImagesOnly: false, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ContentType).IsEqualTo(storageObject.ContentType);
        await Assert.That(result.ContentType).IsNotEqualTo("image/svg+xml");
    }

    [Test]
    public async Task OpenAsync_WithPrivateOwnerObjectAndDifferentUser_ReturnsNullWithoutProviderRead()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(Guid.CreateVersion7());
        _storageObjectRepository.GetById(_storageObjectId)
            .Returns(CreateStorageObject(StorageObjectVisibilities.PrivateOwner, _ownerId));
        var reader = CreateReader();

        var result = await reader.OpenAsync(_storageObjectId, publicImagesOnly: false, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _provider.DidNotReceive().OpenReadAsync(
            Arg.Any<FileStorageReadInput>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OpenAsync_WithPublicImagesOnlyRejectsAuthenticatedTenantObject()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _storageObjectRepository.GetById(_storageObjectId)
            .Returns(CreateStorageObject(StorageObjectVisibilities.AuthenticatedTenant, _ownerId));
        var reader = CreateReader();

        var result = await reader.OpenAsync(_storageObjectId, publicImagesOnly: true, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _provider.DidNotReceive().OpenReadAsync(
            Arg.Any<FileStorageReadInput>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task OpenAsync_WithUnsafePublicImageMetadata_ReturnsNullWithoutProviderRead(bool publicImagesOnly)
    {
        var storageObject = CreateStorageObject(StorageObjectVisibilities.PublicImage, createdBy: null);
        storageObject.ContentType = "text/html";
        storageObject.Extension = "html";
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        var reader = CreateReader();

        var result = await reader.OpenAsync(_storageObjectId, publicImagesOnly, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _provider.DidNotReceive().OpenReadAsync(
            Arg.Any<FileStorageReadInput>(),
            Arg.Any<CancellationToken>());
    }

    private StorageObjectContentReader CreateReader()
        => new(
            _storageObjectRepository,
            _providerResolver,
            _currentUserService,
            NullLogger<StorageObjectContentReader>.Instance,
            _metrics);

    private StorageObject CreateStorageObject(string visibility, Guid? createdBy)
        => new()
        {
            Id = _storageObjectId,
            TenantId = _tenantId,
            FileTypeId = 1,
            FileType = null!,
            Tenant = null!,
            Uri = $"/api/storageobject/{_storageObjectId}/content",
            ObjectKey = "tenants/example/object.png",
            Provider = StorageProviders.Local,
            FullName = "object.png",
            SafeDisplayName = "object.png",
            Extension = "png",
            ContentType = "image/png",
            Sha256Checksum = "abc123",
            Size = 3,
            Visibility = visibility,
            Purpose = StorageObjectPurposes.EventImage,
            LifecycleState = StorageObjectLifecycleStates.Active,
            CreatedBy = createdBy
        };

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }
}
