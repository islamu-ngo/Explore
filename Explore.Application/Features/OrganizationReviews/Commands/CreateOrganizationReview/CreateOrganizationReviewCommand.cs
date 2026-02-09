using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationReviews.Commands.CreateOrganizationReview;

public class CreateOrganizationReviewCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateOrganizationReviewDto CreateOrganizationReviewDto { get; set; }
}
