using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.Features.OrganizationReviews.Commands.CreateOrganizationReview;
using Explore.Application.Features.OrganizationReviews.Queries.GetOrganizationReviews;
using Explore.Application.Features.OrganizationReviews.Queries.GetMyReviews;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class OrganizationReviewController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrganizationReviewController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{organizationId}")]
        public async Task<ActionResult<List<OrganizationReviewDto>>> Get(Guid organizationId)
        {
            var reviews = await _mediator.Send(new GetOrganizationReviewsQuery { OrganizationId = organizationId });
            return Ok(reviews);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<OrganizationReviewDto>>> GetByUserId(Guid userId)
        {
            var reviews = await _mediator.Send(new GetMyReviewsQuery { UserId = userId });
            return Ok(reviews);
        }

        [HttpPost]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Post([FromBody] CreateOrganizationReviewDto createOrganizationReviewDto)
        {
            var command = new CreateOrganizationReviewCommand { CreateOrganizationReviewDto = createOrganizationReviewDto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
