using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.AudienceGender;
using Explore.Application.Features.AudienceGenders.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AudienceGenderController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AudienceGenderController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/audiencegender
        [HttpGet]
        [EndpointSummary("Get all Audience Gender types")]
        [EndpointDescription("Retrieve a list of all audience gender types (Men-only, Women-only, Mixed, Family)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<AudienceGenderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AudienceGenderListDto>>> GetAll()
        {
            var audienceGenders = await _mediator.Send(new GetAudienceGenderListRequest());
            return Ok(audienceGenders);
        }

        // GET: api/v1/audiencegender/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Audience Gender type by ID")]
        [EndpointDescription("Retrieve details of a specific audience gender type")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AudienceGenderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AudienceGenderDto>> GetById(int id)
        {
            var audienceGender = await _mediator.Send(new GetAudienceGenderDetailsRequest { Id = id });
            if (audienceGender == null)
            {
                return NotFound();
            }

            return Ok(audienceGender);
        }
    }
}
