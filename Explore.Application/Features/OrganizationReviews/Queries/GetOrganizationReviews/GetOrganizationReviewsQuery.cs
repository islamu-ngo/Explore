using Explore.Application.DTOs.OrganizationReview;
using MediatR;

namespace Explore.Application.Features.OrganizationReviews.Queries.GetOrganizationReviews;

public class GetOrganizationReviewsQuery : IRequest<List<OrganizationReviewDto>>
{
    public Guid OrganizationId { get; set; }
}
