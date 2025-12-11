using Explore.Application.DTOs.OrganizationReview;
using MediatR;

namespace Explore.Application.Features.OrganizationReviews.Queries.GetMyReviews
{
    public class GetMyReviewsQuery : IRequest<List<OrganizationReviewDto>>
    {
        public Guid UserId { get; set; }
    }
}
