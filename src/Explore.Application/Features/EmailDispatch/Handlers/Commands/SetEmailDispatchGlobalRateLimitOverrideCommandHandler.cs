// ABOUTME: Persists or clears the instance-wide SMTP rate-limit override.
// ABOUTME: Keeps the configured rate effective whenever no durable override is present.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Application.Features.EmailDispatch.Validators;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Handlers.Commands;

public sealed class SetEmailDispatchGlobalRateLimitOverrideCommandHandler(IEmailDispatchOutboxRepository repository)
    : IRequestHandler<SetEmailDispatchGlobalRateLimitOverrideCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        SetEmailDispatchGlobalRateLimitOverrideCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await new SetEmailDispatchGlobalRateLimitOverrideCommandValidator()
            .ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                FailureCode = EmailDispatchFailureCodes.ValidationFailed,
                Message = "Global SMTP rate-limit override failed validation.",
                Errors = validation.Errors.Select(error => error.ErrorMessage).ToList()
            };
        }

        var state = await repository.SetGlobalSmtpRateLimitOverride(
            request.RateLimitPerMinute,
            request.ChangedBy,
            DateTime.UtcNow,
            cancellationToken);
        return new BaseCommandResponse<Guid>
        {
            Id = state.Id,
            Success = true,
            Message = request.RateLimitPerMinute.HasValue
                ? "Global SMTP rate-limit override updated."
                : "Global SMTP rate-limit override cleared."
        };
    }
}
