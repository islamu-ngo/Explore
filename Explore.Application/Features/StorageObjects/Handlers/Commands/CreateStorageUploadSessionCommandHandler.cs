// ABOUTME: Handler for reserving tenant storage quota before bytes are uploaded.
// ABOUTME: Resolves storage policy, enforces byte limits, creates idempotent sessions, and persists counters atomically.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.StorageObject.Validators;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands;

public class CreateStorageUploadSessionCommandHandler
    : IRequestHandler<CreateStorageUploadSessionCommand, BaseCommandResponse<StorageUploadSessionDto>>
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(15);

    private readonly IStoragePolicyResolver _storagePolicyResolver;
    private readonly IStorageUploadSessionRepository _uploadSessionRepository;
    private readonly IStorageUsageCounterRepository _usageCounterRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BusinessMetrics _metrics;

    public CreateStorageUploadSessionCommandHandler(
        IStoragePolicyResolver storagePolicyResolver,
        IStorageUploadSessionRepository uploadSessionRepository,
        IStorageUsageCounterRepository usageCounterRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        BusinessMetrics metrics)
    {
        _storagePolicyResolver = storagePolicyResolver;
        _uploadSessionRepository = uploadSessionRepository;
        _usageCounterRepository = usageCounterRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
    }

    public async Task<BaseCommandResponse<StorageUploadSessionDto>> Handle(
        CreateStorageUploadSessionCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new CreateStorageUploadSessionDtoValidator();
        var validationResult = await validator.ValidateAsync(request.UploadSessionDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            _metrics.RecordStorageUploadSession(null, "create", "failed", "validation_failed");

            return Failure(
                "Upload session reservation failed.",
                validationResult.Errors.Select(error => error.ErrorMessage));
        }

        var tenantId = _tenantContext.TenantId;
        var policy = await _storagePolicyResolver.ResolveAsync(tenantId, cancellationToken);

        if (request.UploadSessionDto.ExpectedSizeBytes > policy.MaxUploadBytes)
        {
            _metrics.RecordStorageUploadSession(
                policy.Provider,
                "create",
                "failed",
                FailureCodes.StorageUploadTooLarge);

            return Failure(
                "Upload exceeds the configured per-file limit.",
                [
                    $"ExpectedSizeBytes must be less than or equal to {policy.MaxUploadBytes} bytes."
                ],
                FailureCodes.StorageUploadTooLarge);
        }

        var response = await _unitOfWork.ExecuteInTransactionAsync(
            async ct => await ReserveSessionAsync(request.UploadSessionDto, tenantId, policy, ct),
            cancellationToken);

        RecordCreateMetrics(response, policy.Provider, request.UploadSessionDto.ExpectedSizeBytes);

        return response;
    }

    private void RecordCreateMetrics(
        BaseCommandResponse<StorageUploadSessionDto> response,
        string provider,
        long expectedSizeBytes)
    {
        var idempotentReplay = IsIdempotentReplay(response);
        var outcome = response.Success
            ? (idempotentReplay ? "idempotent" : "succeeded")
            : "failed";

        _metrics.RecordStorageUploadSession(provider, "create", outcome, response.FailureCode);

        if (response.Success && !idempotentReplay)
        {
            _metrics.RecordStorageQuotaReservation(provider, "reserve", "succeeded");
            _metrics.RecordStorageQuotaBytes(expectedSizeBytes, provider, "reserve", "succeeded");
            return;
        }

        if (!response.Success && response.FailureCode == FailureCodes.QuotaExceeded)
        {
            _metrics.RecordStorageQuotaReservation(provider, "reserve", "failed", response.FailureCode);
        }
    }

    private async Task<BaseCommandResponse<StorageUploadSessionDto>> ReserveSessionAsync(
        CreateStorageUploadSessionDto dto,
        Guid tenantId,
        Models.Storage.ResolvedStoragePolicy policy,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = dto.IdempotencyKey.Trim();
        var existing = await _uploadSessionRepository.GetByTenantAndIdempotencyKeyForUpdateAsync(
            tenantId,
            idempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            var existingCounter = await _usageCounterRepository.GetByTenantAndProviderAsync(
                tenantId,
                existing.Provider,
                cancellationToken);

            return Success(
                existing,
                policy,
                existingCounter,
                "Upload session already exists for this idempotency key.");
        }

        var usageCounter = await _usageCounterRepository.GetOrCreateAsync(
            tenantId,
            policy.Provider,
            cancellationToken);

        if (!usageCounter.CanReserve(dto.ExpectedSizeBytes, policy.TenantQuotaBytes))
        {
            return Failure(
                "Upload would exceed the tenant storage quota.",
                [
                    $"Tenant storage quota is {policy.TenantQuotaBytes} bytes; used={usageCounter.UsedBytes}, reserved={usageCounter.ReservedBytes}, attempted={dto.ExpectedSizeBytes}."
                ],
                FailureCodes.QuotaExceeded);
        }

        usageCounter.Reserve(dto.ExpectedSizeBytes, policy.TenantQuotaBytes);
        await _usageCounterRepository.Update(usageCounter);

        var utcNow = DateTime.UtcNow;
        var session = new StorageUploadSession
        {
            TenantId = tenantId,
            UserId = _currentUserService.UserId,
            Provider = policy.Provider,
            ExpectedSizeBytes = dto.ExpectedSizeBytes,
            ReservedBytes = dto.ExpectedSizeBytes,
            ContentType = NormalizeContentType(dto.ContentType),
            OriginalFileName = NormalizeOptional(dto.OriginalFileName),
            SafeDisplayName = ResolveSafeDisplayName(dto),
            Extension = ResolveExtension(dto),
            Purpose = dto.Purpose,
            Visibility = dto.Visibility,
            Status = StorageUploadSessionStates.Reserved,
            IdempotencyKey = idempotencyKey,
            ExpiresAt = utcNow.Add(SessionLifetime)
        };

        session = await _uploadSessionRepository.Create(session);

        return Success(session, policy, usageCounter, "Upload session reserved successfully.");
    }

    private static BaseCommandResponse<StorageUploadSessionDto> Success(
        StorageUploadSession session,
        Models.Storage.ResolvedStoragePolicy policy,
        StorageUsageCounter? usageCounter,
        string message)
        => new()
        {
            Success = true,
            Id = Map(session, policy, usageCounter),
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

    internal static StorageUploadSessionDto Map(
        StorageUploadSession session,
        Models.Storage.ResolvedStoragePolicy policy,
        StorageUsageCounter? usageCounter)
        => new()
        {
            Id = session.Id,
            TenantId = session.TenantId,
            UserId = session.UserId,
            Provider = session.Provider,
            ExpectedSizeBytes = session.ExpectedSizeBytes,
            ReservedBytes = session.ReservedBytes,
            ContentType = session.ContentType,
            OriginalFileName = session.OriginalFileName,
            SafeDisplayName = session.SafeDisplayName,
            Extension = session.Extension,
            Purpose = session.Purpose,
            Visibility = session.Visibility,
            Status = session.Status,
            IdempotencyKey = session.IdempotencyKey,
            StorageObjectId = session.StorageObjectId,
            StoredSizeBytes = session.StorageObject?.Size,
            Sha256Checksum = session.Sha256Checksum,
            ExpiresAt = session.ExpiresAt,
            UploadStartedAt = session.UploadStartedAt,
            FinalizedAt = session.FinalizedAt,
            CanceledAt = session.CanceledAt,
            FailedAt = session.FailedAt,
            MaxUploadBytes = policy.MaxUploadBytes,
            TenantQuotaBytes = policy.TenantQuotaBytes,
            UsedBytes = usageCounter?.UsedBytes ?? 0,
            TotalReservedBytes = usageCounter?.ReservedBytes ?? 0
        };

    private static string NormalizeContentType(string contentType)
        => contentType.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsIdempotentReplay(BaseCommandResponse<StorageUploadSessionDto> response)
        => response.Message?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true;

    private static string ResolveSafeDisplayName(CreateStorageUploadSessionDto dto)
        => NormalizeOptional(dto.SafeDisplayName)
            ?? NormalizeOptional(dto.OriginalFileName)
            ?? "upload";

    private static string? ResolveExtension(CreateStorageUploadSessionDto dto)
    {
        var extension = NormalizeOptional(dto.Extension);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.TrimStart('.').ToLowerInvariant();
        }

        var fileName = NormalizeOptional(dto.OriginalFileName);
        var dotIndex = fileName?.LastIndexOf('.') ?? -1;
        if (dotIndex < 0 || dotIndex == fileName!.Length - 1)
        {
            return null;
        }

        return fileName[(dotIndex + 1)..].ToLowerInvariant();
    }
}
