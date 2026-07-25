// ABOUTME: Unit tests for privacy-fenced local-first storage upload session command handlers.
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
    private const string ValidSha256Checksum = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly IStoragePolicyResolver _storagePolicyResolver = Substitute.For<IStoragePolicyResolver>();
    private readonly IFileStorageProviderResolver _providerResolver = Substitute.For<IFileStorageProviderResolver>();
    private readonly IFileStorageProvider _provider = Substitute.For<IFileStorageProvider>();
    private readonly IStorageUploadSessionRepository _uploadSessionRepository = Substitute.For<IStorageUploadSessionRepository>();
    private readonly IStorageUsageCounterRepository _usageCounterRepository = Substitute.For<IStorageUsageCounterRepository>();
    private readonly IPrivacyErasureStateRepository _privacyErasureStateRepository =
        Substitute.For<IPrivacyErasureStateRepository>();
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
        _unitOfWork
            .ExecuteSerializableAsync(
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
    public async Task CreateHandle_WhenFenceAppearsBeforeReservationDoesNotReserveQuotaOrCreateSession()
    {
        DateTime nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            _userId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        PrivacyErasureSaga saga = PrivacyErasureSaga.Start(
            intent,
            1,
            new byte[32],
            nowUtc.AddMinutes(5),
            nowUtc);
        _privacyErasureStateRepository
            .GetBySubjectAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, saga);

        BaseCommandResponse<StorageUploadSessionDto> result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(expectedSizeBytes: 42, originalFileName: "Report.PDF")
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Upload session reservation failed.");
        await Assert.That(result.Errors).IsEmpty();
        await _privacyErasureStateRepository.Received(2)
            .GetBySubjectAsync(_userId, Arg.Any<CancellationToken>());
        await _usageCounterRepository.DidNotReceive().Update(Arg.Any<StorageUsageCounter>());
        await _uploadSessionRepository.DidNotReceive().Create(Arg.Any<StorageUploadSession>());
    }

    [Test]
    public async Task CreateHandle_WhenFenceAppearsDuringValidationMasksDetailedErrors()
    {
        DateTime nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            _userId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        PrivacyErasureSaga saga = PrivacyErasureSaga.Start(
            intent,
            1,
            new byte[32],
            nowUtc.AddMinutes(5),
            nowUtc);
        _privacyErasureStateRepository
            .GetBySubjectAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, saga);

        BaseCommandResponse<StorageUploadSessionDto> result = await CreateCreateHandler().Handle(
            new CreateStorageUploadSessionCommand
            {
                UploadSessionDto = CreateUploadDto(idempotencyKey: "")
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Upload session reservation failed.");
        await Assert.That(result.Errors).IsEmpty();
        await _privacyErasureStateRepository.Received(2)
            .GetBySubjectAsync(_userId, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<StorageUploadSessionDto>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateHandle_NormalizesContentTypeBeforePolicyResolutionAndPersistence()
    {
        var policy = CreatePolicy(
            maxUploadBytes: 80,
            quotaBytes: 1_000,
            provider: StorageProviders.S3Compatible,
            routeKey: StorageRouteKeys.Documents,
            policyVersion: 7);
        _storagePolicyResolver
            .ResolveAsync(_tenantId, Arg.Is<StoragePolicyIntent>(request =>
                request.ContentType == "application/pdf"), Arg.Any<CancellationToken>())
            .Returns(policy);
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.S3Compatible };
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
                    originalFileName: "Report.PDF",
                    purpose: StorageObjectPurposes.Document,
                    contentType: " Application/PDF ")
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.Provider).IsEqualTo(StorageProviders.S3Compatible);
        await Assert.That(result.Id.ContentType).IsEqualTo("application/pdf");

        await _storagePolicyResolver.Received(1).ResolveAsync(
            _tenantId,
            Arg.Is<StoragePolicyIntent>(request => request.ContentType == "application/pdf"),
            Arg.Any<CancellationToken>());
        await _uploadSessionRepository.Received(1).Create(Arg.Is<StorageUploadSession>(session =>
            session.ContentType == "application/pdf"));
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
        var objectKey = ReservedObjectKey(session);
        var writeResult = CreateWriteResult(objectKey: objectKey);
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
        await Assert.That(result.Id.Sha256Checksum).IsEqualTo(ValidSha256Checksum);
        await Assert.That(counter.ReservedBytes).IsEqualTo(0);
        await Assert.That(counter.UsedBytes).IsEqualTo(11);
        await Assert.That(counter.ObjectCount).IsEqualTo(1);

        await _provider.Received(1).WriteAsync(
            Arg.Is<FileStorageWriteInput>(input =>
                input.TenantId == _tenantId &&
                input.ExpectedSizeBytes == 11 &&
                input.MaxSizeBytes == 11 &&
                input.ObjectKey == objectKey),
            Arg.Any<CancellationToken>());
        await _storageObjectRepository.Received(1).Create(Arg.Is<StorageObject>(storageObject =>
            storageObject.Provider == StorageProviders.Local &&
            storageObject.ObjectKey == objectKey &&
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
        var writeResult = CreateWriteResult(StorageProviders.S3Compatible, ReservedObjectKey(session));
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
    public async Task FinalizeHandle_WhenSessionIsAlreadyFinalized_ReturnsIdempotentSuccessWithoutProviderReplay()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 11);
        var storageObjectId = Guid.CreateVersion7();
        session.Finalize(storageObjectId, ValidObjectKey("existing.txt"), ValidSha256Checksum, DateTime.UtcNow.AddMinutes(-1));
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository
            .GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>())
            .Returns(new StorageUsageCounter { TenantId = _tenantId, Provider = session.Provider, UsedBytes = 11, ObjectCount = 1 });

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
        await Assert.That(result.Message).Contains("already finalized");
        await Assert.That(result.Id!.Status).IsEqualTo(StorageUploadSessionStates.Finalized);
        await Assert.That(result.Id.StorageObjectId).IsEqualTo(storageObjectId);
        await _provider.DidNotReceive().WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>());
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
        await _usageCounterRepository.DidNotReceive().GetOrCreateAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _uploadSessionRepository.DidNotReceive().Update(Arg.Any<StorageUploadSession>());
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
    public async Task FinalizeHandle_WhenContentSignatureMismatchesReservation_FailsClosedAndReleasesReservation()
    {
        var bytes = "%PDF-1.7"u8.ToArray();
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: bytes.Length);
        session.ContentType = "image/png";
        session.OriginalFileName = "image.png";
        session.SafeDisplayName = "image.png";
        session.Extension = "png";
        var counter = new StorageUsageCounter
        {
            TenantId = _tenantId,
            Provider = StorageProviders.Local,
            ReservedBytes = bytes.Length
        };
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);

        var result = await CreateFinalizeHandler().Handle(
            new FinalizeStorageUploadSessionCommand
            {
                UploadSessionId = session.Id,
                Content = new MemoryStream(bytes),
                ContentType = "image/png",
                ContentLength = bytes.Length
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadContentSignatureMismatch);
        await Assert.That(result.Errors).Contains("Upload bytes did not match the reserved content type signature.");
        await Assert.That(session.Status).IsEqualTo(StorageUploadSessionStates.Failed);
        await Assert.That(counter.ReservedBytes).IsEqualTo(0);
        await _provider.DidNotReceive().WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>());
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
    }

    [Test]
    public async Task FinalizeHandle_WhenKnownContentExtensionMismatchesReservation_FailsClosedWithoutProviderWrite()
    {
        var bytes = ValidPngBytes();
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: bytes.Length);
        session.ContentType = "image/png";
        session.OriginalFileName = "image.jpg";
        session.SafeDisplayName = "image.jpg";
        session.Extension = "jpg";
        var counter = new StorageUsageCounter
        {
            TenantId = _tenantId,
            Provider = StorageProviders.Local,
            ReservedBytes = bytes.Length
        };
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);

        var result = await CreateFinalizeHandler().Handle(
            new FinalizeStorageUploadSessionCommand
            {
                UploadSessionId = session.Id,
                Content = new MemoryStream(bytes),
                ContentType = "image/png",
                ContentLength = bytes.Length
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadContentSignatureMismatch);
        await Assert.That(result.Errors).Contains("File extension did not match the reserved content type.");
        await Assert.That(session.Status).IsEqualTo(StorageUploadSessionStates.Failed);
        await Assert.That(counter.ReservedBytes).IsEqualTo(0);
        await _provider.DidNotReceive().WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>());
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
    }

    [Test]
    public async Task FinalizeHandle_WithNonSeekableKnownContent_ReplaysInspectedPrefixToProvider()
    {
        var bytes = ValidPngBytes();
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: bytes.Length);
        session.ContentType = "image/png";
        session.OriginalFileName = "image.png";
        session.SafeDisplayName = "image.png";
        session.Extension = "png";
        var counter = new StorageUsageCounter
        {
            TenantId = _tenantId,
            Provider = StorageProviders.Local,
            ReservedBytes = bytes.Length
        };
        var objectKey = ReservedObjectKey(session);
        var writeResult = CreateWriteResult(objectKey: objectKey, sizeBytes: bytes.Length, contentType: "image/png");
        byte[]? capturedBytes = null;
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _usageCounterRepository.GetOrCreateAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _provider.WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var input = call.Arg<FileStorageWriteInput>();
            using var captured = new MemoryStream();
            input.Content.CopyTo(captured);
            capturedBytes = captured.ToArray();
            return writeResult;
        });
        _storageObjectRepository.Create(Arg.Any<StorageObject>()).Returns(call => call.Arg<StorageObject>());

        var result = await CreateFinalizeHandler().Handle(
            new FinalizeStorageUploadSessionCommand
            {
                UploadSessionId = session.Id,
                Content = new NonSeekableReadStream(bytes),
                ContentType = "image/png",
                ContentLength = bytes.Length
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedBytes).IsEquivalentTo(bytes);
        await _storageObjectRepository.Received(1).Create(Arg.Is<StorageObject>(storageObject =>
            storageObject.ObjectKey == objectKey &&
            storageObject.ContentType == "image/png" &&
            storageObject.Size == bytes.Length));
    }

    [Test]
    public async Task FinalizeHandle_WhenSessionBelongsToDifferentTenant_ReturnsNotFoundWithoutProviderWrite()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 11);
        session.TenantId = Guid.CreateVersion7();
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

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
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadSessionNotFound);
        await _usageCounterRepository.DidNotReceive().GetByTenantAndProviderAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _provider.DidNotReceive().WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>());
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
        await _uploadSessionRepository.DidNotReceive().Update(Arg.Any<StorageUploadSession>());
    }

    [Test]
    public async Task FinalizeHandle_WhenSessionBelongsToDifferentUser_ReturnsNotFoundWithoutProviderWrite()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 11);
        session.UserId = Guid.CreateVersion7();
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

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
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadSessionNotFound);
        await _usageCounterRepository.DidNotReceive().GetByTenantAndProviderAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _provider.DidNotReceive().WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>());
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
        await _uploadSessionRepository.DidNotReceive().Update(Arg.Any<StorageUploadSession>());
    }

    [Test]
    public async Task FinalizeHandle_WhenSessionIsCanceled_ReturnsInvalidStateWithoutProviderWrite()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Canceled, reservedBytes: 11);
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository
            .GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>())
            .Returns(new StorageUsageCounter { TenantId = _tenantId, Provider = session.Provider, ReservedBytes = 11 });

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
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadSessionInvalidState);
        await Assert.That(result.Errors).Contains($"Upload session status is {StorageUploadSessionStates.Canceled}.");
        await _provider.DidNotReceive().WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>());
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
        await _usageCounterRepository.DidNotReceive().Update(Arg.Any<StorageUsageCounter>());
        await _uploadSessionRepository.DidNotReceive().Update(Arg.Any<StorageUploadSession>());
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
    public async Task FinalizeHandle_WhenProviderReturnsInvalidMetadata_FailsClosedAndReleasesReservation()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 11);
        var counter = new StorageUsageCounter { TenantId = _tenantId, Provider = StorageProviders.Local, ReservedBytes = 11 };
        var wrongTenantObjectKey = $"tenants/{Guid.CreateVersion7():N}/object.txt";
        var writeResult = CreateWriteResult(
            provider: StorageProviders.S3Compatible,
            objectKey: wrongTenantObjectKey,
            sizeBytes: 10,
            contentType: "application/json",
            checksum: "not-a-sha256");
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _provider.WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>()).Returns(writeResult);

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
        await Assert.That(result.Message).IsEqualTo("Storage provider returned invalid upload metadata.");
        await Assert.That(result.Errors).Contains("Storage provider result did not match the reserved provider.");
        await Assert.That(result.Errors).Contains("Storage provider returned an invalid object key.");
        await Assert.That(result.Errors).Contains("Storage provider byte count did not match the reserved byte count.");
        await Assert.That(result.Errors).Contains("Storage provider content type did not match the reserved content type.");
        await Assert.That(result.Errors).Contains("Storage provider checksum was missing or invalid.");
        await Assert.That(session.Status).IsEqualTo(StorageUploadSessionStates.Failed);
        await Assert.That(counter.ReservedBytes).IsEqualTo(0);
        await Assert.That(counter.UsedBytes).IsEqualTo(0);
        await Assert.That(counter.ObjectCount).IsEqualTo(0);
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
        await _uploadSessionRepository.Received().Update(Arg.Is<StorageUploadSession>(value =>
            value.Id == session.Id &&
            value.Status == StorageUploadSessionStates.Failed));
    }

    [Test]
    public async Task FinalizeHandle_WhenErasureFenceAppearsAfterProviderWrite_DoesNotFinalizeMetadata()
    {
        var session = CreateSession(StorageUploadSessionStates.Reserved, 11);
        var counter = new StorageUsageCounter
        {
            TenantId = _tenantId,
            Provider = StorageProviders.Local,
            ReservedBytes = 11
        };
        DateTime nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            _userId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        PrivacyErasureSaga saga = PrivacyErasureSaga.Start(
            intent,
            1,
            new byte[32],
            nowUtc.AddMinutes(5),
            nowUtc);

        _privacyErasureStateRepository
            .GetBySubjectAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, (PrivacyErasureSaga?)null, saga);
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _usageCounterRepository.GetByTenantAndProviderAsync(_tenantId, session.Provider, Arg.Any<CancellationToken>()).Returns(counter);
        _provider.WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>())
            .Returns(call => CreateWriteResult(objectKey: call.Arg<FileStorageWriteInput>().ObjectKey));

        BaseCommandResponse<StorageUploadSessionDto> result = await CreateFinalizeHandler().Handle(
            new FinalizeStorageUploadSessionCommand
            {
                UploadSessionId = session.Id,
                Content = new MemoryStream(new byte[11]),
                ContentType = "text/plain",
                ContentLength = 11
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Upload finalization failed.");
        await Assert.That(result.Errors).IsEmpty();
        await Assert.That(session.Status).IsEqualTo(StorageUploadSessionStates.Failed);
        await _provider.Received(1).WriteAsync(Arg.Any<FileStorageWriteInput>(), Arg.Any<CancellationToken>());
        await _storageObjectRepository.DidNotReceive().Create(Arg.Any<StorageObject>());
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
    public async Task CancelHandle_WhenSessionBelongsToDifferentUser_ReturnsNotFoundWithoutRelease()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 40);
        session.UserId = Guid.CreateVersion7();
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateCancelHandler().Handle(
            new CancelStorageUploadSessionCommand { UploadSessionId = session.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadSessionNotFound);
        await _usageCounterRepository.DidNotReceive().GetByTenantAndProviderAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _usageCounterRepository.DidNotReceive().Update(Arg.Any<StorageUsageCounter>());
        await _uploadSessionRepository.DidNotReceive().Update(Arg.Any<StorageUploadSession>());
    }

    [Test]
    public async Task CancelHandle_WhenSessionBelongsToDifferentTenant_ReturnsNotFoundWithoutRelease()
    {
        var session = CreateSession(status: StorageUploadSessionStates.Reserved, reservedBytes: 40);
        session.TenantId = Guid.CreateVersion7();
        _uploadSessionRepository.GetByIdForUpdateAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateCancelHandler().Handle(
            new CancelStorageUploadSessionCommand { UploadSessionId = session.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.StorageUploadSessionNotFound);
        await _usageCounterRepository.DidNotReceive().GetByTenantAndProviderAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
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
            _privacyErasureStateRepository,
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
            _currentUserService,
            _unitOfWork,
            _metrics);

    private FinalizeStorageUploadSessionCommandHandler CreateFinalizeHandler()
        => new(
            _providerResolver,
            _storagePolicyResolver,
            _uploadSessionRepository,
            _usageCounterRepository,
            _storageObjectRepository,
            _privacyErasureStateRepository,
            _tenantContext,
            _currentUserService,
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

    private FileStorageWriteResult CreateWriteResult(
        string provider = StorageProviders.Local,
        string? objectKey = null,
        long sizeBytes = 11,
        string contentType = "text/plain",
        string? checksum = ValidSha256Checksum)
        => new(provider, objectKey ?? ValidObjectKey(), sizeBytes, contentType, checksum);

    private string ValidObjectKey(string fileName = "object.txt")
        => $"tenants/{_tenantId:N}/2026/07/04/{fileName}";

    private static string ReservedObjectKey(StorageUploadSession session)
        => $"tenants/{session.TenantId:N}/uploads/{session.Id:N}.{session.Extension}";

    private static byte[] ValidPngBytes()
        => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableReadStream(byte[] bytes)
        {
            _inner = new MemoryStream(bytes);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _inner.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer)
            => _inner.Read(buffer);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

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
