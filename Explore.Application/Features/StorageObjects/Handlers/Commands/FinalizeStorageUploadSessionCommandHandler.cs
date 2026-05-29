// ABOUTME: Handler for streaming reserved upload-session bytes into provider storage and finalizing metadata.
// ABOUTME: Separates long-running provider IO from short database transactions while preserving quota consistency.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Models.Storage;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands;

public class FinalizeStorageUploadSessionCommandHandler
    : IRequestHandler<FinalizeStorageUploadSessionCommand, BaseCommandResponse<StorageUploadSessionDto>>
{
    private readonly IFileStorageProviderResolver _providerResolver;
    private readonly IStoragePolicyResolver _storagePolicyResolver;
    private readonly IStorageUploadSessionRepository _uploadSessionRepository;
    private readonly IStorageUsageCounterRepository _usageCounterRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public FinalizeStorageUploadSessionCommandHandler(
        IFileStorageProviderResolver providerResolver,
        IStoragePolicyResolver storagePolicyResolver,
        IStorageUploadSessionRepository uploadSessionRepository,
        IStorageUsageCounterRepository usageCounterRepository,
        IStorageObjectRepository storageObjectRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _providerResolver = providerResolver;
        _storagePolicyResolver = storagePolicyResolver;
        _uploadSessionRepository = uploadSessionRepository;
        _usageCounterRepository = usageCounterRepository;
        _storageObjectRepository = storageObjectRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<StorageUploadSessionDto>> Handle(
        FinalizeStorageUploadSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UploadSessionId == Guid.Empty)
        {
            return Failure("Upload finalization failed.", ["UploadSessionId is required."]);
        }

        if (request.Content is null || !request.Content.CanRead)
        {
            return Failure("Upload finalization failed.", ["A readable upload content stream is required."]);
        }

        var tenantId = _tenantContext.TenantId;
        var policy = await _storagePolicyResolver.ResolveAsync(tenantId, cancellationToken);

        var sessionResponse = await _unitOfWork.ExecuteInTransactionAsync(
            async ct => await MarkUploadingAsync(request, tenantId, policy, ct),
            cancellationToken);

        if (!sessionResponse.Success || sessionResponse.Id is null)
        {
            return sessionResponse;
        }

        var session = await _uploadSessionRepository.GetByIdForUpdateAsync(request.UploadSessionId, cancellationToken);
        if (session is null || session.TenantId != tenantId)
        {
            return Failure(
                "Upload session was not found.",
                ["Upload session was not found."],
                FailureCodes.StorageUploadSessionNotFound);
        }

        try
        {
            var provider = _providerResolver.GetRequired(session.Provider);
            var writeResult = await provider.WriteAsync(
                new FileStorageWriteInput(
                    tenantId,
                    request.Content,
                    session.ContentType,
                    session.SafeDisplayName,
                    session.Extension,
                    session.ExpectedSizeBytes,
                    session.ExpectedSizeBytes),
                cancellationToken);

            return await _unitOfWork.ExecuteInTransactionAsync(
                async ct => await FinalizeAsync(session.Id, tenantId, policy, writeResult, ct),
                cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException or ArgumentException)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async ct => await FailSessionAsync(
                    session.Id,
                    tenantId,
                    policy,
                    FailureCodes.StorageUploadWriteFailed,
                    "Storage provider could not accept the upload.",
                    ct),
                cancellationToken);
        }
    }

    private async Task<BaseCommandResponse<StorageUploadSessionDto>> MarkUploadingAsync(
        FinalizeStorageUploadSessionCommand request,
        Guid tenantId,
        ResolvedStoragePolicy policy,
        CancellationToken cancellationToken)
    {
        var session = await _uploadSessionRepository.GetByIdForUpdateAsync(request.UploadSessionId, cancellationToken);
        if (session is null || session.TenantId != tenantId)
        {
            return Failure(
                "Upload session was not found.",
                ["Upload session was not found."],
                FailureCodes.StorageUploadSessionNotFound);
        }

        var counter = await _usageCounterRepository.GetByTenantAndProviderAsync(
            tenantId,
            session.Provider,
            cancellationToken);

        if (session.Status == StorageUploadSessionStates.Finalized)
        {
            return Success(session, policy, counter, "Upload session is already finalized.");
        }

        if (session.Status != StorageUploadSessionStates.Reserved)
        {
            return Failure(
                "Upload session cannot accept bytes in its current state.",
                [$"Upload session status is {session.Status}."],
                FailureCodes.StorageUploadSessionInvalidState);
        }

        var utcNow = DateTime.UtcNow;
        if (session.ExpiresAt <= utcNow)
        {
            if (counter is not null)
            {
                counter.ReleaseReservation(session.ReservedBytes);
                await _usageCounterRepository.Update(counter);
            }

            session.MarkExpired(utcNow);
            await _uploadSessionRepository.Update(session);

            return Failure(
                "Upload session has expired.",
                ["Upload session has expired."],
                FailureCodes.StorageUploadSessionExpired);
        }

        if (request.ContentLength is { } contentLength && contentLength != session.ExpectedSizeBytes)
        {
            return Failure(
                "Upload content length does not match the reserved byte count.",
                [$"ContentLength must equal {session.ExpectedSizeBytes} bytes."],
                FailureCodes.StorageUploadSizeMismatch);
        }

        if (!string.IsNullOrWhiteSpace(request.ContentType) &&
            !string.Equals(request.ContentType.Trim(), session.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                "Upload content type does not match the reserved content type.",
                [$"ContentType must equal {session.ContentType}."],
                FailureCodes.StorageUploadContentTypeMismatch);
        }

        session.MarkUploading(utcNow);
        await _uploadSessionRepository.Update(session);

        return Success(session, policy, counter, "Upload session is ready to accept bytes.");
    }

    private async Task<BaseCommandResponse<StorageUploadSessionDto>> FinalizeAsync(
        Guid uploadSessionId,
        Guid tenantId,
        ResolvedStoragePolicy policy,
        FileStorageWriteResult writeResult,
        CancellationToken cancellationToken)
    {
        var session = await _uploadSessionRepository.GetByIdForUpdateAsync(uploadSessionId, cancellationToken);
        if (session is null || session.TenantId != tenantId)
        {
            return Failure(
                "Upload session was not found.",
                ["Upload session was not found."],
                FailureCodes.StorageUploadSessionNotFound);
        }

        var counter = await _usageCounterRepository.GetOrCreateAsync(tenantId, writeResult.Provider, cancellationToken);
        if (session.Status == StorageUploadSessionStates.Finalized)
        {
            return Success(session, policy, counter, "Upload session is already finalized.");
        }

        if (session.Status != StorageUploadSessionStates.Uploading)
        {
            return Failure(
                "Upload session cannot be finalized in its current state.",
                [$"Upload session status is {session.Status}."],
                FailureCodes.StorageUploadSessionInvalidState);
        }

        var storageObject = new StorageObject
        {
            Id = Guid.CreateVersion7(),
            FileTypeId = ResolveFileTypeId(session.ContentType, session.Extension),
            FileType = null!,
            Uri = $"/api/storageobject/{session.Id}/content",
            ObjectKey = writeResult.ObjectKey,
            Provider = writeResult.Provider,
            FullName = session.SafeDisplayName,
            SafeDisplayName = session.SafeDisplayName,
            Extension = ResolveRequiredExtension(session.Extension, session.SafeDisplayName),
            ContentType = writeResult.ContentType,
            Sha256Checksum = writeResult.Sha256Checksum,
            Size = writeResult.SizeBytes,
            Visibility = session.Visibility,
            Purpose = session.Purpose,
            LifecycleState = StorageObjectLifecycleStates.Active,
            TenantId = tenantId,
            Tenant = null!,
            ActorId = null
        };
        storageObject.Uri = $"/api/storageobject/{storageObject.Id}/content";

        storageObject = await _storageObjectRepository.Create(storageObject);
        counter.FinalizeReservation(writeResult.SizeBytes);
        await _usageCounterRepository.Update(counter);

        session.Finalize(storageObject.Id, writeResult.ObjectKey, writeResult.Sha256Checksum, DateTime.UtcNow);
        session.StorageObject = storageObject;
        await _uploadSessionRepository.Update(session);

        return Success(session, policy, counter, "Upload session finalized successfully.");
    }

    private async Task<BaseCommandResponse<StorageUploadSessionDto>> FailSessionAsync(
        Guid uploadSessionId,
        Guid tenantId,
        ResolvedStoragePolicy policy,
        string failureCode,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        var session = await _uploadSessionRepository.GetByIdForUpdateAsync(uploadSessionId, cancellationToken);
        if (session is null || session.TenantId != tenantId)
        {
            return Failure("Upload session was not found.", ["Upload session was not found."], FailureCodes.StorageUploadSessionNotFound);
        }

        var counter = await _usageCounterRepository.GetByTenantAndProviderAsync(
            tenantId,
            session.Provider,
            cancellationToken);

        if (session.Status != StorageUploadSessionStates.Finalized)
        {
            if (counter is not null)
            {
                counter.ReleaseReservation(session.ReservedBytes);
                await _usageCounterRepository.Update(counter);
            }

            session.Fail(failureCode, failureMessage, DateTime.UtcNow);
            await _uploadSessionRepository.Update(session);
        }

        return new BaseCommandResponse<StorageUploadSessionDto>
        {
            Success = false,
            Id = CreateStorageUploadSessionCommandHandler.Map(session, policy, counter),
            Message = failureMessage,
            Errors = [failureMessage],
            FailureCode = failureCode
        };
    }

    private static BaseCommandResponse<StorageUploadSessionDto> Success(
        StorageUploadSession session,
        ResolvedStoragePolicy policy,
        StorageUsageCounter? usageCounter,
        string message)
        => new()
        {
            Success = true,
            Id = CreateStorageUploadSessionCommandHandler.Map(session, policy, usageCounter),
            Message = message
        };

    private static BaseCommandResponse<StorageUploadSessionDto> Failure(
        string message,
        IEnumerable<string> errors,
        string? failureCode = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors.ToList(),
            FailureCode = failureCode
        };

    private static int ResolveFileTypeId(string contentType, string? extension)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return (int)FileTypeEnum.Image;
        }

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return (int)FileTypeEnum.Video;
        }

        if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return (int)FileTypeEnum.Audio;
        }

        var normalizedExtension = extension?.TrimStart('.').ToLowerInvariant();
        if (normalizedExtension is "pdf" or "doc" or "docx" or "txt" or "rtf" or "odt")
        {
            return (int)FileTypeEnum.Document;
        }

        return (int)FileTypeEnum.Other;
    }

    private static string ResolveRequiredExtension(string? extension, string safeDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.TrimStart('.').ToLowerInvariant();
        }

        var dotIndex = safeDisplayName.LastIndexOf('.');
        return dotIndex >= 0 && dotIndex < safeDisplayName.Length - 1
            ? safeDisplayName[(dotIndex + 1)..].ToLowerInvariant()
            : "bin";
    }
}
