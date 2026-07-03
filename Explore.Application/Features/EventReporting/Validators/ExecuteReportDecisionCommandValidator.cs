// ABOUTME: FluentValidation rules for executing captured report decisions.
// ABOUTME: Ensures route identifiers, case concurrency, and optional correlation metadata are bounded.

using Explore.Application.Features.EventReporting.Policies;
using Explore.Application.Features.EventReporting.Requests.Commands;
using FluentValidation;

namespace Explore.Application.Features.EventReporting.Validators;

public sealed class ExecuteReportDecisionCommandValidator : AbstractValidator<ExecuteReportDecisionCommand>
{
    public ExecuteReportDecisionCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.ReportId).NotEmpty();
        RuleFor(command => command.CaseId).NotEmpty();
        RuleFor(command => command.DecisionId).NotEmpty();
        RuleFor(command => command.ExpectedCaseConcurrencyStamp).NotEmpty();

        RuleFor(command => command.CorrelationId)
            .MaximumLength(EventReportReasonCodePolicy.MaxCorrelationIdLength)
            .When(command => !string.IsNullOrWhiteSpace(command.CorrelationId));
    }
}
