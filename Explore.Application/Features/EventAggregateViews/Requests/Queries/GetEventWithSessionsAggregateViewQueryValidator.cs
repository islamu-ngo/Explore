// ABOUTME: Manual FluentValidation validator for the single-event aggregate view query.
// ABOUTME: Prevents empty identifiers before the handler hits the read-model repository.

using FluentValidation;

namespace Explore.Application.Features.EventAggregateViews.Requests.Queries;

public sealed class GetEventWithSessionsAggregateViewQueryValidator : AbstractValidator<GetEventWithSessionsAggregateViewQuery>
{
    public GetEventWithSessionsAggregateViewQueryValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty();
    }
}
