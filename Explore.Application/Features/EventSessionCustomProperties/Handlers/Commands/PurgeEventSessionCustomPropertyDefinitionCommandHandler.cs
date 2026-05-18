// ABOUTME: Handles explicit audited hard purge for dependency-free session custom-property definitions.
// ABOUTME: Blocks irreversible purge when values, projections, audit, or template provenance exist.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Features.CustomProperties;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessionCustomProperties.Handlers.Commands;

public sealed class PurgeEventSessionCustomPropertyDefinitionCommandHandler : IRequestHandler<PurgeEventSessionCustomPropertyDefinitionCommand, BaseCommandResponse<CustomPropertyPurgeResultDto>>
{
    private readonly IEventSessionCustomPropertyRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;

    public PurgeEventSessionCustomPropertyDefinitionCommandHandler(
        IEventSessionCustomPropertyRepository repository,
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        HybridCache cache)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<CustomPropertyPurgeResultDto>> Handle(PurgeEventSessionCustomPropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<CustomPropertyPurgeResultDto>();
        var reason = request.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            response.Success = false;
            response.Message = "Session custom-property definition purge failed.";
            response.Errors = ["A purge reason is required."];
            return response;
        }

        var summary = await _repository.GetPurgeDependencies(request.Id, cancellationToken);
        if (summary is null)
        {
            response.Success = false;
            response.Message = "Session custom-property definition not found.";
            return response;
        }

        if (summary.HasBlockingDependencies)
        {
            response.Success = false;
            response.Message = "Session custom-property definition purge blocked.";
            response.Id = CustomPropertyPurgeResponseFactory.ToResult(summary, false, null, reason);
            response.Errors = CustomPropertyPurgeResponseFactory.ToBlockingErrors(summary).ToList();
            return response;
        }

        var auditLogId = Guid.NewGuid();
        var result = CustomPropertyPurgeResponseFactory.ToResult(summary, true, auditLogId, reason);
        var audit = CustomPropertyPurgeResponseFactory.CreateAudit(summary, result, _currentUserService.UserId);

        var purged = await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var deleted = await _repository.PurgeDefinition(request.Id, ct);
                if (!deleted)
                {
                    return false;
                }

                await _auditLogRepository.Create(audit);
                return true;
            },
            cancellationToken);

        response.Success = purged;
        response.Message = purged
            ? "Session custom-property definition purged successfully."
            : "Session custom-property definition purge failed.";
        response.Id = purged ? result : CustomPropertyPurgeResponseFactory.ToResult(summary, false, null, reason);

        if (purged)
        {
            await _cache.RemoveAsync($"session-custom-properties:detail:{request.Id}", cancellationToken);
        }

        return response;
    }
}
