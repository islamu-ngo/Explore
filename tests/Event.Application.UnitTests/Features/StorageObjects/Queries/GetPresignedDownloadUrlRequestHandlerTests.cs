// ABOUTME: Unit tests for metadata-authorized storage presigned download URL queries.
// ABOUTME: Verifies visibility, expiration, and object-key leakage guards before signing provider URLs.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.StorageObjects.Handlers.Queries;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using Explore.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.StorageObjects.Queries;

public sealed class GetPresignedDownloadUrlRequestHandlerTests
{
    private readonly Guid _storageObjectId = Guid.CreateVersion7();
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _ownerId = Guid.CreateVersion7();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly IObjectStorageService _objectStorageService = Substitute.For<IObjectStorageService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public GetPresignedDownloadUrlRequestHandlerTests()
    {
        _objectStorageService
            .GeneratePresignedDownloadUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns("https://storage.example.test/presigned");
    }

    [Test]
    public async Task Handle_WithPublicActiveObject_SignsPersistedObjectKeyAndDoesNotReturnObjectKey()
    {
        var storageObject = CreateStorageObject(StorageObjectVisibilities.PublicImage, createdBy: null);
        storageObject.ObjectKey = "tenants/example/object.png";
        storageObject.Uri = "https://storage.example.test/attacker-controlled-uri.png";
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.PresignedUrl).IsEqualTo("https://storage.example.test/presigned");
        await Assert.That(result.ObjectKey).IsEqualTo(string.Empty);
        await Assert.That(result.ExpiresInMinutes).IsEqualTo(15);
        await Assert.That(result.SafeDisplayName).IsEqualTo("object.png");
        await Assert.That(result.ShouldDownloadAsAttachment).IsFalse();
        await _objectStorageService.Received(1).GeneratePresignedDownloadUrl(
            "tenants/example/object.png",
            "object.png",
            15);
    }

    [Test]
    public async Task Handle_WithAuthenticatedDocument_ReturnsSanitizedAttachmentMetadata()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        var storageObject = CreateStorageObject(StorageObjectVisibilities.AuthenticatedTenant, _ownerId);
        storageObject.ContentType = "application/pdf";
        storageObject.Extension = "pdf";
        storageObject.Purpose = StorageObjectPurposes.Document;
        storageObject.SafeDisplayName = "../unsafe.pdf";
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.SafeDisplayName).IsEqualTo("download");
        await Assert.That(result.ShouldDownloadAsAttachment).IsTrue();
        await _objectStorageService.Received(1).GeneratePresignedDownloadUrl(
            storageObject.ObjectKey!,
            "download",
            15);
    }

    [Test]
    public async Task Handle_WithPrivateOwnerObjectAndDifferentUser_ReturnsNullWithoutSigning()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(Guid.CreateVersion7());
        _storageObjectRepository.GetById(_storageObjectId)
            .Returns(CreateStorageObject(StorageObjectVisibilities.PrivateOwner, _ownerId));
        var handler = CreateHandler();

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _objectStorageService.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task Handle_WithPrivateOwnerObjectAndOwnerUser_SignsProviderObjectKey()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_ownerId);
        var storageObject = CreateStorageObject(StorageObjectVisibilities.PrivateOwner, _ownerId);
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await _objectStorageService.Received(1).GeneratePresignedDownloadUrl(
            storageObject.ObjectKey!,
            storageObject.SafeDisplayName,
            15);
    }

    [Test]
    public async Task Handle_WithMissingObjectKey_ReturnsNullWithoutSigning()
    {
        var storageObject = CreateStorageObject(StorageObjectVisibilities.PublicImage, createdBy: null);
        storageObject.ObjectKey = null;
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _objectStorageService.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task Handle_WithUnsafePublicImageMetadata_ReturnsNullWithoutSigning()
    {
        var storageObject = CreateStorageObject(StorageObjectVisibilities.PublicImage, createdBy: null);
        storageObject.ContentType = "text/html";
        storageObject.Extension = "html";
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _objectStorageService.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task Handle_WithOutOfRangeExpiration_ReturnsNullWithoutMetadataLookup()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(CreateRequest(expirationMinutes: 240), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _storageObjectRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _objectStorageService.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task Handle_WhenSigningFails_LogsFailureTypeWithoutRawExceptionPayload()
    {
        var storageObject = CreateStorageObject(StorageObjectVisibilities.PublicImage, createdBy: null);
        storageObject.ObjectKey = "tenants/example/sensitive-object-key.png";
        _storageObjectRepository.GetById(_storageObjectId).Returns(storageObject);
        _objectStorageService
            .GeneratePresignedDownloadUrl(storageObject.ObjectKey, storageObject.SafeDisplayName, 15)
            .Returns<Task<string>>(_ => throw new InvalidOperationException(
                $"provider leaked raw storage reference {storageObject.ObjectKey}"));
        var logger = new ListLogger<GetPresignedDownloadUrlRequestHandler>();
        var handler = CreateHandler(logger);

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        await Assert.That(result).IsNull();
        var log = logger.Entries.Single(entry => entry.Level == LogLevel.Error);
        await Assert.That(log.Exception).IsNull();
        await Assert.That(log.Message).Contains("FailureType=provider_unavailable");
        await Assert.That(log.Message).DoesNotContain("provider leaked raw storage reference");
        await Assert.That(log.Message).DoesNotContain(storageObject.ObjectKey);
    }

    private GetPresignedDownloadUrlRequestHandler CreateHandler(
        ILogger<GetPresignedDownloadUrlRequestHandler>? logger = null)
        => new(
            _storageObjectRepository,
            _objectStorageService,
            _currentUserService,
            logger ?? NullLogger<GetPresignedDownloadUrlRequestHandler>.Instance);

    private GetPresignedDownloadUrlRequest CreateRequest(int expirationMinutes = 15)
        => new()
        {
            Id = _storageObjectId,
            ExpirationMinutes = expirationMinutes
        };

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

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();

        public void Dispose()
        {
        }
    }
}
