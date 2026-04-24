// ABOUTME: Manual FluentValidation validator for the aggregate list query request.
// ABOUTME: Enforces non-null filters and sane pagination before view access.

using Explore.Application.DTOs.EventAggregateView;
using Explore.Application.Responses;
using FluentValidation;

namespace Explore.Application.Features.EventAggregateViews.Requests.Queries;

public sealed class GetEventListAggregateViewQueryValidator : AbstractValidator<GetEventListAggregateViewQuery>
{
    public GetEventListAggregateViewQueryValidator()
    {
        RuleFor(x => x.Filter)
            .NotNull();

        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginatedResult<EventListAggregateViewDto>.MaxPageSize);
    }
}
