using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.VisibilityType;
using Explore.Application.Features.VisibilityTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class VisibilityTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VisibilityTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/visibilitytype
        [HttpGet]
        [EndpointSummary("Get all Visibility Types")]
        [EndpointDescription("Retrieve a list of all event visibility types (Public, Private, Unlisted)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<VisibilityTypeListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<VisibilityTypeListDto>>> GetAll()
        {
            var visibilityTypes = await _mediator.Send(new GetVisibilityTypeListRequest());
            return Ok(visibilityTypes);
        }

        // GET: api/v1/visibilitytype/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Visibility Type by ID")]
        [EndpointDescription("Retrieve details of a specific event visibility type")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VisibilityTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VisibilityTypeDto>> GetById(int id)
        {
            var visibilityType = await _mediator.Send(new GetVisibilityTypeDetailsRequest { Id = id });
            return Ok(visibilityType);
        }
    }
}
