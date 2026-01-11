using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.DidCustodyType;
using Explore.Application.Features.DidCustodyTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class DidCustodyTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DidCustodyTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/didcustodytype
        [HttpGet]
        [EndpointSummary("Get all DID Custody Types")]
        [EndpointDescription("Retrieve a list of all DID custody types (Self-Custodied, Custodial, Managed)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<DidCustodyTypeListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DidCustodyTypeListDto>>> GetAll()
        {
            var didCustodyTypes = await _mediator.Send(new GetDidCustodyTypeListRequest());
            return Ok(didCustodyTypes);
        }

        // GET: api/v1/didcustodytype/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get DID Custody Type by ID")]
        [EndpointDescription("Retrieve details of a specific DID custody type")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DidCustodyTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DidCustodyTypeDto>> GetById(int id)
        {
            var didCustodyType = await _mediator.Send(new GetDidCustodyTypeDetailsRequest { Id = id });
            return Ok(didCustodyType);
        }
    }
}
