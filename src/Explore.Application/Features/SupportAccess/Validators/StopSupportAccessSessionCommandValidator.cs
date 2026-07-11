// ABOUTME: FluentValidation rules for actor-owned support-access stop requests.
// ABOUTME: Keeps optional stop reason text bounded before Domain transition methods run.

using Explore.Application.Features.SupportAccess.Requests.Commands;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.Features.SupportAccess.Validators;

public sealed class StopSupportAccessSessionCommandValidator : AbstractValidator<StopSupportAccessSessionCommand>
{
    public StopSupportAccessSessionCommandValidator()
    {
        RuleFor(command => command.SessionId)
            .NotEmpty();

        RuleFor(command => command.EndReasonText)
            .MaximumLength(SupportAccessSession.MaxEndReasonTextLength);
    }
}
