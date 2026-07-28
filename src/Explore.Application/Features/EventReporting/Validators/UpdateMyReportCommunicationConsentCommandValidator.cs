// ABOUTME: FluentValidation rules for reporter-owned communication-consent updates.
// ABOUTME: Rejects an empty route report id before ownership and tenant checks run.

using Explore.Application.Features.EventReporting.Requests.Commands;
using FluentValidation;

namespace Explore.Application.Features.EventReporting.Validators;

public sealed class UpdateMyReportCommunicationConsentCommandValidator
    : AbstractValidator<UpdateMyReportCommunicationConsentCommand>
{
    public UpdateMyReportCommunicationConsentCommandValidator()
    {
        RuleFor(command => command.ReportId).NotEmpty();
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.Consent)
            .NotNull()
            .When(command => command.Request is not null);
    }
}
