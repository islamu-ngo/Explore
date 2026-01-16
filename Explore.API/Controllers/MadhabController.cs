using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.Madhab;
using Explore.Application.Features.Madhabs.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class MadhabController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MadhabController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/madhab
        [HttpGet]
        [EndpointSummary("Get all Madhabs")]
        [EndpointDescription("Retrieve a list of all Islamic jurisprudence schools (Hanafi, Maliki, Shafi'i, Hanbali)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MadhabListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MadhabListDto>>> GetAll()
        {
            var madhabs = await _mediator.Send(new GetMadhabListRequest());
            return Ok(madhabs);
        }

        // GET: api/v1/madhab/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Madhab by ID")]
        [EndpointDescription("Retrieve details of a specific Islamic jurisprudence school")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MadhabDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MadhabDto>> GetById(int id)
        {
            var madhab = await _mediator.Send(new GetMadhabDetailsRequest { Id = id });
            return Ok(madhab);
        }
    }
}
