using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.AudienceAge;
using Explore.Application.Features.AudienceAges.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AudienceAgeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AudienceAgeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/audienceage
        [HttpGet]
        [EndpointSummary("Get all Audience Age groups")]
        [EndpointDescription("Retrieve a list of all audience age groups (Children, Youth, Adults, Seniors, All Ages)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<AudienceAgeListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AudienceAgeListDto>>> GetAll()
        {
            var audienceAges = await _mediator.Send(new GetAudienceAgeListRequest());
            return Ok(audienceAges);
        }

        // GET: api/v1/audienceage/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Audience Age group by ID")]
        [EndpointDescription("Retrieve details of a specific audience age group")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AudienceAgeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AudienceAgeDto>> GetById(int id)
        {
            var audienceAge = await _mediator.Send(new GetAudienceAgeDetailsRequest { Id = id });
            if (audienceAge == null)
            {
                return NotFound();
            }

            return Ok(audienceAge);
        }
    }
}
