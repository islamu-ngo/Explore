// ABOUTME: Handles tenant-wide rebuild of event custom-property projection rows.
// ABOUTME: Delegates to the projection updater which coordinates advisory locks, status tracking, and dirty-scope drain.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.DTOs.CustomPropertyProjection.Validators;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain.Settings.Definitions;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Handlers.Commands;

public class RebuildEventCustomPropertyProjectionCommandHandler
    : IRequestHandler<RebuildEventCustomPropertyProjectionCommand, BaseCommandResponse<RebuildProjectionResponseDto>>
{
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ProjectionMetrics _metrics;

    public RebuildEventCustomPropertyProjectionCommandHandler(
        IEventCustomPropertyProjectionUpdater projectionUpdater,
        ICustomPropertyQuotaResolver quotaResolver,
        ProjectionMetrics metrics)
    {
        _projectionUpdater = projectionUpdater;
        _quotaResolver = quotaResolver;
        _metrics = metrics;
    }

    public async Task<BaseCommandResponse<RebuildProjectionResponseDto>> Handle(
        RebuildEventCustomPropertyProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<RebuildProjectionResponseDto>();

        var validator = new RebuildProjectionRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(request.RequestDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Projection rebuild request validation failed.";
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
                response.SetQuotaExceeded(
                    "Projection rebuild request validation failed.",
                    new QuotaExceededDetails(
                        CustomPropertyQuotaSettingDefinitions.ProjectionRebuildBatchSize.Key,
                        maxBatchSize,
                        null,
                        request.RequestDto.BatchSize.Value,
                        "event_custom_property_projection_rebuild",
                        request.RequestDto.TenantId));
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
            IEventCustomPropertyProjectionUpdater.ProjectionName,
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
            ? $"Projection rebuild completed. {result.RowsProcessed} rows processed, {result.DrainedDirtyScopes} dirty scopes drained."
            : "Projection rebuild skipped — another rebuild is already in progress.";

        return response;
    }
}
