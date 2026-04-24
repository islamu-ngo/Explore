// ABOUTME: Handles transactional event-session-template sync applies using the explicit sync service and manual validator instantiation.
// ABOUTME: Returns structured outcome data instead of throwing for stale-base or concurrent-update conflict results.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Exceptions;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplateSync.Commands.ApplyEventSessionTemplateSync;

public sealed class ApplyEventSessionTemplateSyncCommandHandler
    : IRequestHandler<ApplyEventSessionTemplateSyncCommand, BaseCommandResponse<TemplateSyncOutcomeDto>>
{
    private readonly IEventSessionTemplateSyncService _syncService;

    public ApplyEventSessionTemplateSyncCommandHandler(IEventSessionTemplateSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<BaseCommandResponse<TemplateSyncOutcomeDto>> Handle(
        ApplyEventSessionTemplateSyncCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new TemplateSyncPlanDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Plan, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult);

        var outcome = await _syncService.ApplySyncAsync(
            request.EventSessionId,
            request.Plan,
            request.BaseProvenanceVersion,
            cancellationToken);

        string? failureCode = outcome.Conflicts.FirstOrDefault()?.Reason;
        if (failureCode is ConcurrencyConflictException.StaleSyncBase or ConcurrencyConflictException.ConcurrentUpdate)
        {
            throw new ConcurrencyConflictException(
                failureCode,
                failureCode == ConcurrencyConflictException.StaleSyncBase
                    ? "The template sync base is stale. Recompute the diff and try again."
                    : "The event session was updated concurrently. Recompute the diff and try again.",
                nameof(EventSession),
                request.EventSessionId.ToString());
        }

        return new BaseCommandResponse<TemplateSyncOutcomeDto>
        {
            Success = outcome.Conflicts.Count == 0,
            Id = outcome,
            Message = outcome.Conflicts.Count == 0
                ? "Event session template sync applied successfully."
                : "Event session template sync completed with conflicts.",
            Errors = outcome.Conflicts.Select(x => $"{x.Key}:{x.Reason}").ToList(),
            FailureCode = outcome.Conflicts.Count == 0 ? null : outcome.Conflicts[0].Reason
        };
    }
}
