// ABOUTME: Persists idempotent instance-wide SMTP processor pause and resume commands.
// ABOUTME: Leaves queued outbox work untouched so a later resume continues normal admission.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Application.Features.EmailDispatch.Validators;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Handlers.Commands;

public sealed class SetEmailDispatchProcessorPauseStateCommandHandler(IEmailDispatchOutboxRepository repository)
    : IRequestHandler<SetEmailDispatchProcessorPauseStateCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        SetEmailDispatchProcessorPauseStateCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await new SetEmailDispatchProcessorPauseStateCommandValidator()
            .ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                FailureCode = EmailDispatchFailureCodes.ValidationFailed,
                Message = "Email dispatch processor pause update failed validation.",
                Errors = validation.Errors.Select(error => error.ErrorMessage).ToList()
            };
        }

        var state = await repository.SetProcessorPauseState(
            request.IsPaused,
            request.IsPaused ? request.PauseReason?.Trim() : null,
            request.ChangedBy,
            DateTime.UtcNow,
            cancellationToken);
        return new BaseCommandResponse<Guid>
        {
            Id = state.Id,
            Success = true,
            Message = request.IsPaused
                ? "Email dispatch processor paused."
                : "Email dispatch processor resumed."
        };
    }
}
