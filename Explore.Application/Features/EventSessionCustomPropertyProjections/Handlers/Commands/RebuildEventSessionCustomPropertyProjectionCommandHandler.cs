// ABOUTME: Handles tenant-wide rebuild of event session custom-property projection rows.
// ABOUTME: Mirrors event projection rebuild handler for session scope.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.DTOs.CustomPropertyProjection.Validators;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain.Settings.Definitions;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Handlers.Commands;

public class RebuildEventSessionCustomPropertyProjectionCommandHandler
    : IRequestHandler<RebuildEventSessionCustomPropertyProjectionCommand, BaseCommandResponse<RebuildProjectionResponseDto>>
{
    private readonly IEventSessionCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ProjectionMetrics _metrics;

    public RebuildEventSessionCustomPropertyProjectionCommandHandler(
        IEventSessionCustomPropertyProjectionUpdater projectionUpdater,
        ICustomPropertyQuotaResolver quotaResolver,
        ProjectionMetrics metrics)
    {
        _projectionUpdater = projectionUpdater;
        _quotaResolver = quotaResolver;
        _metrics = metrics;
    }

    public async Task<BaseCommandResponse<RebuildProjectionResponseDto>> Handle(
        RebuildEventSessionCustomPropertyProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<RebuildProjectionResponseDto>();

        var validator = new RebuildProjectionRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(request.RequestDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Session projection rebuild request validation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        if (request.RequestDto.BatchSize.HasValue)
        {
            var maxBatchSize = await _quotaResolver.GetIntAsync(
                CustomPropertyQuotaSettingDefinitions.ProjectionRebuildBatchSize.Key,
                request.RequestDto.TenantId,
                cancellationToken);

            if (request.RequestDto.BatchSize.Value > maxBatchSize)
            {
                response.Success = false;
                response.Message = "Session projection rebuild request validation failed.";
                response.Errors = [$"quota_exceeded: Projection rebuild batch size limit of {maxBatchSize} has been exceeded."];
                return response;
            }
        }

        var startedAt = DateTimeOffset.UtcNow;
        var result = await _projectionUpdater.RebuildForTenantAsync(
            request.RequestDto.TenantId,
            request.RequestDto.BatchSize,
            cancellationToken);
        var completedAt = DateTimeOffset.UtcNow;
        var durationSeconds = (completedAt - startedAt).TotalSeconds;

        _metrics.RecordRebuild(
            request.RequestDto.TenantId.ToString(),
            IEventSessionCustomPropertyProjectionUpdater.ProjectionName,
            result.RowsProcessed,
            result.RowsFailed,
            durationSeconds,
            result.LockAcquired);

        response.Success = true;
        response.Id = new RebuildProjectionResponseDto
        {
            LockAcquired = result.LockAcquired,
            RowsProcessed = result.RowsProcessed,
            RowsFailed = result.RowsFailed,
            DrainedDirtyScopes = result.DrainedDirtyScopes,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };
        response.Message = result.LockAcquired
            ? $"Session projection rebuild completed. {result.RowsProcessed} rows processed, {result.DrainedDirtyScopes} dirty scopes drained."
            : "Session projection rebuild skipped — another rebuild is already in progress.";

        return response;
    }
}
