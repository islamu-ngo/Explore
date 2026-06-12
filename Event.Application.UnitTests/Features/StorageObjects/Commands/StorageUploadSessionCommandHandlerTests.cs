// ABOUTME: Unit tests for local-first storage upload session command handlers.
// ABOUTME: Verifies quota reservation, idempotency, validation, cancellation, and expiry behavior.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Handlers.Commands;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Models.Storage;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.StorageObjects.Commands;

public sealed class StorageUploadSessionCommandHandlerTests : IDisposable
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly IStoragePolicyResolver _storagePolicyResolver = Substitute.For<IStoragePolicyResolver>();
    private readonly IFileStorageProviderResolver _providerResolver = Substitute.For<IFileStorageProviderResolver>();
    private readonly IFileStorageProvider _provider = Substitute.For<IFileStorageProvider>();
    private readonly IStorageUploadSessionRepository _uploadSessionRepository = Substitute.For<IStorageUploadSessionRepository>();
    private readonly IStorageUsageCounterRepository _usageCounterRepository = Substitute.For<IStorageUsageCounterRepository>();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BusinessMetrics _metrics = CreateMetrics();

    public StorageUploadSessionCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserService.UserId.Returns(_userId);
        _storagePolicyResolver
            .ResolveAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(CreatePolicy(maxUploadBytes: 100, quotaBytes: 1_000));
        _storagePolicyResolver
            .ResolveAsync(_tenantId, Arg.Any<StoragePolicyIntent>(), Arg.Any<CancellationToken>())
            .Returns(CreatePolicy(maxUploadBytes: 100, quotaBytes: 1_000));
        _usageCounterRepository
            .GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StorageUsageCounter>());
        _providerResolver.GetRequired(StorageProviders.Local).Returns(_provider);

        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<StorageUploadSessionDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<StorageUploadSessionDto>>>>()(CancellationToken.None));
    }

    public void Dispose()
    {
        _metrics.Dispose();
    }

    [Test]
    public async Task CreateHandle_WithValidRequest_ReservesQuotaAndCreatesSession()
    {
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.Local, UsedBytes = 100 };
        _usageCounterRepository.GetOrCreateAsync(_tenantId, StorageProviders.Local, Arg.Any<CancellationToken>()).Returns(counter);
        _uploadSessionRepository.Create(Arg.Any<StorageUploadSession>()).Returns(call =>
        {
            var session = call.Arg<StorageUploadSession>();
            session.Id = Guid.CreateVersion7();
            return session;
        });

        var result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(expectedSizeBytes: 42, originalFileName: "Report.PDF")
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.Provider).IsEqualTo(StorageProviders.Local);
        await Assert.That(result.Id.Status).IsEqualTo(StorageUploadSessionStates.Reserved);
        await Assert.That(result.Id.UserId).IsEqualTo(_userId);
        await Assert.That(result.Id.ReservedBytes).IsEqualTo(42);
        await Assert.That(result.Id.TotalReservedBytes).IsEqualTo(42);
        await Assert.That(result.Id.Extension).IsEqualTo("pdf");

        await _usageCounterRepository.Received(1).Update(Arg.Is<StorageUsageCounter>(usage => usage.ReservedBytes == 42));
        await _uploadSessionRepository.Received(1).Create(Arg.Is<StorageUploadSession>(session =>
            session.TenantId == _tenantId &&
            session.UserId == _userId &&
            session.ContentType == "application/pdf" &&
            session.SafeDisplayName == "Report.PDF" &&
            session.ExpectedSizeBytes == 42 &&
            session.ReservedBytes == 42));
    }

    [Test]
    public async Task CreateHandle_WhenExpectedSizeExceedsPolicy_ReturnsUploadTooLargeFailure()
    {
        var result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(expectedSizeBytes: 101)
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadTooLarge);
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<StorageUploadSessionDto>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateHandle_ForDocumentPurpose_StoresSelectedRoutePolicyOnSession()
    {
        var policy = CreatePolicy(
            maxUploadBytes: 80,
            quotaBytes: 1_000,
            provider: StorageProviders.S3Compatible,
            routeKey: StorageRouteKeys.Documents,
            policyVersion: 7);
        _storagePolicyResolver
            .ResolveAsync(_tenantId, Arg.Is<StoragePolicyIntent>(request =>
                request.Purpose == StorageObjectPurposes.Document &&
                request.ContentType == "application/pdf"), Arg.Any<CancellationToken>())
            .Returns(policy);
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.S3Compatible, UsedBytes = 100 };
        _usageCounterRepository.GetOrCreateAsync(_tenantId, StorageProviders.S3Compatible, Arg.Any<CancellationToken>()).Returns(counter);
        _uploadSessionRepository.Create(Arg.Any<StorageUploadSession>()).Returns(call =>
        {
            var session = call.Arg<StorageUploadSession>();
            session.Id = Guid.CreateVersion7();
            return session;
        });

        var result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(
                    expectedSizeBytes: 42,
                    originalFileName: "Policy.PDF",
                    purpose: StorageObjectPurposes.Document,
                    contentType: "application/pdf")
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.Provider).IsEqualTo(StorageProviders.S3Compatible);
        await Assert.That(result.Id.RouteKey).IsEqualTo(StorageRouteKeys.Documents);
        await Assert.That(result.Id.PolicyMaxUploadBytes).IsEqualTo(80);
        await Assert.That(result.Id.PolicyVersion).IsEqualTo("7");
        await Assert.That(result.Id.MaxUploadBytes).IsEqualTo(80);

        await _uploadSessionRepository.Received(1).Create(Arg.Is<StorageUploadSession>(session =>
            session.Provider == StorageProviders.S3Compatible &&
            session.RouteKey == StorageRouteKeys.Documents &&
            session.PolicyMaxUploadBytes == 80 &&
            session.PolicyVersion == "7"));
    }

    [Test]
    public async Task FinalizeHandle_WithReservedSession_WritesProviderCreatesMetadataAndFinalizesUsage()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 11);
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.Local, ReservedBytes = 11 };
        var writeResult = new FileStorageWriteResult(StorageProviders.Local, "tenant/object.txt", 11, "text/plain", "hash");
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _usageCounterRepository.GetOrCreateAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _provider.WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>()).Returns(writeResult);
        _storageObjectRepository.Create(Arg.Any<StorageObject>()).Returns(call => call.Arg<StorageObject>());

        var result = await CreateFinalizeHandler().Handle(
            new FinalizeStorageUploadSessionCommand
            {
                UploadSessionId = session.Id,
                Content = new MemoryStream(new byte[11]),
                ContentType = "text/plain",
                ContentLength = 11
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.Status).IsEqualTo(StorageUploadSessionStates.Finalized);
        await Assert.That(result.Id.StorageObjectId).IsNotNull();
        await Assert.That(result.Id.Sha256Checksum).IsEqualTo("hash");
        await Assert.That(counter.ReservedBytes).IsEqualTo(0);
        await Assert.That(counter.UsedBytes).IsEqualTo(11);
        await Assert.That(counter.ObjectCount).IsEqualTo(1);

        await _provider.Received(1).WriteAsync(
            Arg.Is<FileStorageWriteInput>(input =>
                input.TenantId == _tenantId &&
                input.ExpectedSizeBytes == 11 &&
                input.MaxSizeBytes == 11),
            Arg.Any<CancellationToken>());
        await _storageObjectRepository.Received(1).Create(Arg.Is<StorageObject>(storageObject =>
            storageObject.Provider == StorageProviders.Local &&
            storageObject.ObjectKey == "tenant/object.txt" &&
            storageObject.Size == 11 &&
            storageObject.ContentType == "text/plain"));
    }

    [Test]
    public async Task FinalizeHandle_UsesPersistedSessionPolicyAfterSettingsChange()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 11);
        session.Provider = StorageProviders.S3Compatible;
        session.RouteKey = StorageRouteKeys.Documents;
        session.PolicyMaxUploadBytes = 80;
        session.PolicyVersion = "7";
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.S3Compatible, ReservedBytes = 11 };
        var writeResult = new FileStorageWriteResult(StorageProviders.S3Compatible, "tenant/document.txt", 11, "text/plain", "hash");
        _providerResolver.GetRequired(StorageProviders.S3Compatible).Returns(_provider);
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _usageCounterRepository.GetOrCreateAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _provider.WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>()).Returns(writeResult);
        _storageObjectRepository.Create(Arg.Any<StorageObject>()).Returns(call => call.Arg<StorageObject>());

        var result = await CreateFinalizeHandler().Handle(
            new FinalizeStorageUploadSessionCommand
            {
                UploadSessionId = session.Id,
                Content = new MemoryStream(new byte[11]),
                ContentType = "text/plain",
                ContentLength = 11
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.Provider).IsEqualTo(StorageProviders.S3Compatible);
        await Assert.That(result.Id.RouteKey).IsEqualTo(StorageRouteKeys.Documents);
        await Assert.That(result.Id.PolicyMaxUploadBytes).IsEqualTo(80);
        await Assert.That(result.Id.PolicyVersion).IsEqualTo("7");
        await Assert.That(result.Id.MaxUploadBytes).IsEqualTo(80);
        await _storagePolicyResolver.DidNotReceive().ResolveAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _storagePolicyResolver.DidNotReceive().ResolveAsync(Arg.Any<Guid?>(), Arg.Any<StoragePolicyIntent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FinalizeHandle_WhenContentLengthMismatchesReservation_ReturnsFailureWithoutProviderWrite()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 11);
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository
            .GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>())
            .Returns(new StorageUsageCounter { TenantId = _tenantId, Provider = session.Provider, ReservedBytes = 11 });

        var result = await CreateFinalizeHandler().Handle(
            new FinalizeStorageUploadSessionCommand
            {
                UploadSessionId = session.Id,
                Content = new MemoryStream(new byte[10]),
                ContentType = "text/plain",
                ContentLength = 10
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadSizeMismatch);
        await _provider.DidNotReceive().WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>());
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
    }

    [Test]
    public async Task FinalizeHandle_WhenProviderWriteFails_MarksSessionFailedAndReleasesReservation()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 11);
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.Local, ReservedBytes = 11 };
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _provider.WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>())
            .Returns<Task<FileStorageWriteResult>>(_ => throw new IOException("disk full"));

        var result = await CreateFinalizeHandler().Handle(
            new FinalizeStorageUploadSessionCommand
            {
                UploadSessionId = session.Id,
                Content = new MemoryStream(new byte[11]),
                ContentType = "text/plain",
                ContentLength = 11
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadWriteFailed);
        await Assert.That(session.Status).IsEqualTo(StorageUploadSessionStates.Failed);
        await Assert.That(counter.ReservedBytes).IsEqualTo(0);
        await _uploadSessionRepository.Received().Update(Arg.Is<StorageUploadSession>(value =>
            value.Id == session.Id &&
            value.Status == StorageUploadSessionStates.Failed));
    }

    [Test]
    public async Task CreateHandle_WhenQuotaWouldBeExceeded_ReturnsQuotaFailureWithoutCreatingSession()
    {
        var counter = new StorageUsageCounter
        {
            TenantId = _tenantId,
            Provider = StorageProviders.Local,
            UsedBytes = 980,
            ReservedBytes = 10
        };
        _usageCounterRepository.GetOrCreateAsync(_tenantId, StorageProviders.Local, Arg.Any<CancellationToken>()).Returns(counter);

        var result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(expectedSizeBytes: 20)
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await _uploadSessionRepository.DidNotReceive().Create(Arg.Any<StorageUploadSession>());
        await _usageCounterRepository.DidNotReceive().Update(Arg.Any<StorageUsageCounter>());
    }

    [Test]
    public async Task CreateHandle_WhenAggregateQuotaAcrossProvidersWouldBeExceeded_ReturnsQuotaFailure()
    {
        var policy = CreatePolicy(
            maxUploadBytes: 100,
            quotaBytes: 1_000,
            provider: StorageProviders.S3Compatible,
            routeKey: StorageRouteKeys.Documents);
        _storagePolicyResolver
            .ResolveAsync(_tenantId, Arg.Any<StoragePolicyIntent>(), Arg.Any<CancellationToken>())
            .Returns(policy);
        var s3Counter = new StorageUsageCounter
        {
            TenantId = _tenantId,
            Provider = StorageProviders.S3Compatible,
            UsedBytes = 100
        };
        var localCounter = new StorageUsageCounter
        {
            TenantId = _tenantId,
            Provider = StorageProviders.Local,
            UsedBytes = 850,
            ReservedBytes = 40
        };
        _usageCounterRepository.GetOrCreateAsync(_tenantId, StorageProviders.S3Compatible, Arg.Any<CancellationToken>()).Returns(s3Counter);
        _usageCounterRepository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns([localCounter]);

        var result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(expectedSizeBytes: 20, purpose: StorageObjectPurposes.Document, contentType: "application/pdf")
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await _uploadSessionRepository.DidNotReceive().Create(Arg.Any<StorageUploadSession>());
        await _usageCounterRepository.DidNotReceive().Update(Arg.Any<StorageUsageCounter>());
    }

    [Test]
    public async Task CreateHandle_WithExistingIdempotencyKey_ReturnsExistingSessionWithoutDoubleReserve()
    {
        var existing = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 50);
        _uploadSessionRepository
            .GetByTenantAndIdempotencyKeyForUpdateAsync(_tenantId, "upload-1", Arg.Any<CancellationToken>())
            .Returns(existing);
        _usageCounterRepository
            .GetByTenantAndProviderAsync(_tenantId, existing.Provider, Arg.Any<CancellationToken>())
            .Returns(new StorageUsageCounter { TenantId = _tenantId, Provider = existing.Provider, ReservedBytes = 50 });

        var result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(idempotencyKey: "upload-1")
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.Id).IsEqualTo(existing.Id);
        await Assert.That(result.Message).Contains("already exists");
        await _usageCounterRepository.DidNotReceive().GetOrCreateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uploadSessionRepository.DidNotReceive().Create(Arg.Any<StorageUploadSession>());
    }

    [Test]
    public async Task CreateHandle_WhenIdempotencyKeyMissing_ReturnsValidationFailure()
    {
        var result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(idempotencyKey: "")
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).Contains("Idempotency Key is required");
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<StorageUploadSessionDto>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelHandle_WithReservedSession_ReleasesQuotaAndCancels()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 40);
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.Local, ReservedBytes = 70 };
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);

        var result = await CreateCancelHandler().Handle(
            new CancelStorageUploadSessionCommand { UploadSessionId = session.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.Status).IsEqualTo(StorageUploadSessionStates.Canceled);
        await Assert.That(counter.ReservedBytes).IsEqualTo(30);
        await _usageCounterRepository.Received(1).Update(counter);
        await _uploadSessionRepository.Received(1).Update(Arg.Is<StorageUploadSession>(value =>
            value.Id == session.Id &&
            value.Status == StorageUploadSessionStates.Canceled));
    }

    [Test]
    public async Task CancelHandle_WhenSessionIsExpired_MarksExpiredAndReleasesQuota()
    {
        var session = CreateSession(
            status: StorageUploadSessionStates.Reserved,
            reservedBytes: 25,
            expiresAt: DateTime.UtcNow.AddMinutes(-1));
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.Local, ReservedBytes = 25 };
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);

        var result = await CreateCancelHandler().Handle(
            new CancelStorageUploadSessionCommand { UploadSessionId = session.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.Status).IsEqualTo(StorageUploadSessionStates.Expired);
        await Assert.That(result.Id.FailedAt).IsNotNull();
        await Assert.That(counter.ReservedBytes).IsEqualTo(0);
    }

    [Test]
    public async Task CancelHandle_WhenSessionFinalized_ReturnsFailureWithoutRelease()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Finalized, reservedBytes: 0);
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.Local, ReservedBytes = 10 };
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);

        var result = await CreateCancelHandler().Handle(
            new CancelStorageUploadSessionCommand { UploadSessionId = session.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadSessionFinalized);
        await Assert.That(counter.ReservedBytes).IsEqualTo(10);
        await _usageCounterRepository.DidNotReceive().Update(Arg.Any<StorageUsageCounter>());
        await _uploadSessionRepository.DidNotReceive().Update(Arg.Any<StorageUploadSession>());
    }

    [Test]
    public async Task CancelHandle_WhenSessionIdMissing_ReturnsValidationFailure()
    {
        var result = await CreateCancelHandler().Handle(
            new CancelStorageUploadSessionCommand { UploadSessionId = Guid.Empty },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors![0]).IsEqualTo("UploadSessionId is required.");
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<StorageUploadSessionDto>>>>(),
            Arg.Any<CancellationToken>());
    }

    private CreateStorageUploadSessionCommandHandler CreateCreateHandler()
        => new(
            _storagePolicyResolver,
            _uploadSessionRepository,
            _usageCounterRepository,
            _tenantContext,
            _currentUserService,
            _unitOfWork,
            _metrics);

    private CancelStorageUploadSessionCommandHandler CreateCancelHandler()
        => new(
            _storagePolicyResolver,
            _uploadSessionRepository,
            _usageCounterRepository,
            _tenantContext,
            _unitOfWork,
            _metrics);

    private FinalizeStorageUploadSessionCommandHandler CreateFinalizeHandler()
        => new(
            _providerResolver,
            _storagePolicyResolver,
            _uploadSessionRepository,
            _usageCounterRepository,
            _storageObjectRepository,
            _tenantContext,
            _unitOfWork,
            _metrics);

    private static CreateStorageUploadSessionDto CreateUploadDto(
        long expectedSizeBytes = 10,
        string idempotencyKey = "upload-1",
        string originalFileName = "file.txt",
        string purpose = StorageObjectPurposes.Attachment,
        string visibility = StorageObjectVisibilities.PrivateOwner,
        string? contentType = null)
        => new()
        {
            ExpectedSizeBytes = expectedSizeBytes,
            ContentType = contentType
                ?? (originalFileName.EndsWith(".PDF", StringComparison.Ordinal) ? "Application/PDF" : "text/plain"),
            OriginalFileName = originalFileName,
            Purpose = purpose,
            Visibility = visibility,
            IdempotencyKey = idempotencyKey
        };

    private StorageUploadSession CreateSession(
        string status,
        long reservedBytes,
        DateTime? expiresAt = null)
        => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantId,
            UserId = _userId,
            Provider = StorageProviders.Local,
            RouteKey = StorageRouteKeys.General,
            PolicyMaxUploadBytes = reservedBytes,
            PolicyVersion = "1",
            ExpectedSizeBytes = reservedBytes,
            ReservedBytes = reservedBytes,
            ContentType = "text/plain",
            OriginalFileName = "file.txt",
            SafeDisplayName = "file.txt",
            Extension = "txt",
            Purpose = StorageObjectPurposes.Attachment,
            Visibility = StorageObjectVisibilities.PrivateOwner,
            Status = status,
            IdempotencyKey = "upload-1",
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10)
        };

    private ResolvedStoragePolicy CreatePolicy(
        long maxUploadBytes,
        long quotaBytes,
        string provider = StorageProviders.Local,
        string routeKey = StorageRouteKeys.General,
        int policyVersion = 1)
    {
        var route = new ResolvedStorageRoutePolicy(
            routeKey,
            provider,
            maxUploadBytes,
            SettingSource.TenantOverride,
            SettingSource.TenantOverride);

        return new ResolvedStoragePolicy(
            _tenantId,
            provider,
            maxUploadBytes,
            quotaBytes,
            maxUploadBytes,
            TenantOverridesAllowed: true,
            TenantStorageLocked: false,
            ProviderSource: SettingSource.TenantOverride,
            MaxUploadSource: SettingSource.TenantOverride,
            QuotaSource: SettingSource.TenantOverride,
            routeKey,
            policyVersion,
            [route],
            route);
    }

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }
}
