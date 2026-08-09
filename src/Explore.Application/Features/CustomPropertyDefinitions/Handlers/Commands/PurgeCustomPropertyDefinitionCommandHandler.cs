// ABOUTME: Handles explicit audited hard purge for dependency-free shared custom-property definitions.
// ABOUTME: Keeps irreversible purge separate from normal retire + soft-delete lifecycle.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Features.CustomProperties;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.CustomPropertyDefinitions.Handlers.Commands;

public sealed class PurgeCustomPropertyDefinitionCommandHandler : IRequestHandler<PurgeCustomPropertyDefinitionCommand, BaseCommandResponse<CustomPropertyPurgeResultDto>>
{
    private readonly ICustomPropertyDefinitionRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly BusinessMetrics? _metrics;

    public PurgeCustomPropertyDefinitionCommandHandler(
        ICustomPropertyDefinitionRepository repository,
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        HybridCache cache,
        BusinessMetrics? metrics = null)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _metrics = metrics;
    }

    public async Task<BaseCommandResponse<CustomPropertyPurgeResultDto>> Handle(PurgeCustomPropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<CustomPropertyPurgeResultDto>();
        var reason = request.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            response.Success = false;
            response.Message = "Custom-property definition purge failed.";
            response.Errors = ["A purge reason is required."];
            return response;
        }

        var summary = await _repository.GetPurgeDependencies(request.Id, cancellationToken);
        if (summary is null)
        {
            response.Success = false;
            response.Message = "Custom-property definition not found.";
            response.FailureCode = FailureCodes.NotFound;
            return response;
        }

        if (summary.HasBlockingDependencies)
        {
            CustomPropertyPurgeResponseFactory.ApplyBlockedResponse(
                response,
                summary,
                reason,
                "Custom-property definition purge blocked.");
            RecordPurgeDecision(summary, "blocked");
            return response;
        }

        var auditLogId = Guid.CreateVersion7();
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

        if (!purged)
        {
            var latestSummary = await _repository.GetPurgeDependencies(request.Id, cancellationToken);
            if (latestSummary?.HasBlockingDependencies == true)
            {
                CustomPropertyPurgeResponseFactory.ApplyBlockedResponse(
                    response,
                    latestSummary,
                    reason,
                    "Custom-property definition purge blocked.");
                RecordPurgeDecision(latestSummary, "blocked_after_recheck");
                return response;
            }
        }

        response.Success = purged;
        response.Message = purged
            ? "Custom-property definition purged successfully."
            : "Custom-property definition purge failed.";
        response.Id = purged ? result : CustomPropertyPurgeResponseFactory.ToResult(summary, false, null, reason);
        RecordPurgeDecision(summary, purged ? "purged" : "failed");

        if (purged)
        {
            await _cache.RemoveAsync($"custom-property-definitions:detail:{request.Id}", cancellationToken);
        }

        return response;
    }

    private void RecordPurgeDecision(CustomPropertyPurgeDependencySummary summary, string outcome)
    {
        _metrics?.RecordCustomPropertyPurgeDecision(
            summary.TenantId.ToString(),
            summary.Scope,
            outcome,
            CustomPropertyPurgeResponseFactory.GetPrimaryBlockerCategory(summary));
    }
}
