// ABOUTME: Handler for canceling pending storage upload sessions and releasing reserved quota.
// ABOUTME: Keeps cancellation idempotent and marks stale sessions expired before returning state.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Handlers.Commands;

public class CancelStorageUploadSessionCommandHandler
    : IRequestHandler<CancelStorageUploadSessionCommand, BaseCommandResponse<StorageUploadSessionDto>>
{
    private readonly IStoragePolicyResolver _storagePolicyResolver;
    private readonly IStorageUploadSessionRepository _uploadSessionRepository;
    private readonly IStorageUsageCounterRepository _usageCounterRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CancelStorageUploadSessionCommandHandler(
        IStoragePolicyResolver storagePolicyResolver,
        IStorageUploadSessionRepository uploadSessionRepository,
        IStorageUsageCounterRepository usageCounterRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _storagePolicyResolver = storagePolicyResolver;
        _uploadSessionRepository = uploadSessionRepository;
        _usageCounterRepository = usageCounterRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<StorageUploadSessionDto>> Handle(
        CancelStorageUploadSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UploadSessionId == Guid.Empty)
        {
            return Failure("Upload session cancellation failed.", ["UploadSessionId is required."]);
        }

        var tenantId = _tenantContext.TenantId;
        var policy = await _storagePolicyResolver.ResolveAsync(tenantId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(
            async ct => await CancelSessionAsync(request.UploadSessionId, tenantId, policy, ct),
            cancellationToken);
    }

    private async Task<BaseCommandResponse<StorageUploadSessionDto>> CancelSessionAsync(
        Guid uploadSessionId,
        Guid tenantId,
        Models.Storage.ResolvedStoragePolicy policy,
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

        var counter = await _usageCounterRepository.GetByTenantAndProviderAsync(
            tenantId,
            session.Provider,
            cancellationToken);

        if (session.Status == StorageUploadSessionStates.Finalized)
        {
            return Failure(
                "Finalized upload sessions cannot be canceled.",
                ["Finalized upload sessions cannot be canceled."],
                FailureCodes.StorageUploadSessionFinalized);
        }

        if (session.Status is StorageUploadSessionStates.Canceled
            or StorageUploadSessionStates.Expired
            or StorageUploadSessionStates.Failed)
        {
            return Success(session, policy, counter, "Upload session is already closed.");
        }

        if (counter is not null)
        {
            counter.ReleaseReservation(session.ReservedBytes);
            await _usageCounterRepository.Update(counter);
        }

        var utcNow = DateTime.UtcNow;
        if (session.ExpiresAt <= utcNow)
        {
            session.MarkExpired(utcNow);
        }
        else
        {
            session.Cancel(utcNow);
        }

        await _uploadSessionRepository.Update(session);

        return Success(session, policy, counter, "Upload session canceled successfully.");
    }

    private static BaseCommandResponse<StorageUploadSessionDto> Success(
        StorageUploadSession session,
        Models.Storage.ResolvedStoragePolicy policy,
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
}
