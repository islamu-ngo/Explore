// ABOUTME: MediatR query for fetching the current user's submitted organization reviews.
// ABOUTME: Returns IEnumerable<OrganizationReviewDto>.
using Explore.Application.DTOs.OrganizationReview;
using MediatR;

namespace Explore.Application.Features.OrganizationReviews.Queries.GetMyReviews;

public class GetMyReviewsQuery : IRequest<List<OrganizationReviewDto>>
{
    public Guid UserId { get; set; }
}
