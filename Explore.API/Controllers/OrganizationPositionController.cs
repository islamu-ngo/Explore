using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.OrganizationPosition;
using Explore.Application.Features.OrganizationPositions.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class OrganizationPositionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrganizationPositionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/organizationposition
        [HttpGet]
        [EndpointSummary("Get all Organization Positions")]
        [EndpointDescription("Retrieve a list of all organization positions (President, Secretary, Member)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<OrganizationPositionListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<OrganizationPositionListDto>>> GetAll()
        {
            var organizationPositions = await _mediator.Send(new GetOrganizationPositionListRequest());
            return Ok(organizationPositions);
        }

        // GET: api/v1/organizationposition/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Organization Position by ID")]
        [EndpointDescription("Retrieve details of a specific organization position")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrganizationPositionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrganizationPositionDto>> GetById(int id)
        {
            var organizationPosition = await _mediator.Send(new GetOrganizationPositionDetailsRequest { Id = id });
            return Ok(organizationPosition);
        }
    }
}
