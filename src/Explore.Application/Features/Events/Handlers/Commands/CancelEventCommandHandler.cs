// ABOUTME: Handler that transitions an event to the Cancelled lifecycle state.
// ABOUTME: Tolerant path: skips publish readiness and emits no public outbox events.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public sealed class CancelEventCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<CancelEventCommand, BaseCommandResponse<Guid>>
{
    private const string ConcurrencyConflictCode = "event_cancel_concurrency_conflict";
    private const string AlreadyCancelledCode = "event_cancel_already_cancelled";

    public async Task<BaseCommandResponse<Guid>> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        var validator = new CancelEventRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(request.Id, "Event cancel request is invalid.", validationResult.Errors.Select(e => e.ErrorMessage));
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var @event = await eventRepository.GetById(request.Id);
            if (@event is null)
            {
                return Failure(request.Id, "Event was not found.", new[] { "Event was not found." });
            }

            if (@event.ConcurrencyStamp != request.Request.ExpectedConcurrencyStamp)
            {
                return Failure(request.Id, "Event was modified by another user.", new[] { "The event was modified by another user. Refresh and try again." }, ConcurrencyConflictCode);
            }

            if (@event.EventStatusId == (int)EventStatusEnum.Cancelled)
            {
                return Failure(request.Id, "Event is already cancelled.", new[] { "The event is already cancelled." }, AlreadyCancelledCode);
            }

            @event.EventStatusId = (int)EventStatusEnum.Cancelled;
            @event.UpdatedAt = DateTime.UtcNow;

            await eventRepository.Update(@event);
            await cache.RemoveAsync($"event:detail:{@event.Id}", token);
            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), token);

            return Success(@event.Id, "Event cancelled successfully.");
        }, cancellationToken);
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(Guid id, string message, IEnumerable<string> errors, string? failureCode = null) => new()
    {
        Success = false,
        Id = id,
        Message = message,
        Errors = errors.ToList(),
        FailureCode = failureCode
    };
}
