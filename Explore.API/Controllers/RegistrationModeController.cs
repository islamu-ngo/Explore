using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.RegistrationMode;
using Explore.Application.Features.RegistrationModes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RegistrationModeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RegistrationModeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/registrationmode
        [HttpGet]
        [EndpointSummary("Get all Registration Modes")]
        [EndpointDescription("Retrieve a list of all registration modes (Open, ApprovalRequired, InvitationOnly)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<RegistrationModeListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<RegistrationModeListDto>>> GetAll()
        {
            var registrationModes = await _mediator.Send(new GetRegistrationModeListRequest());
            return Ok(registrationModes);
        }

        // GET: api/v1/registrationmode/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Registration Mode by ID")]
        [EndpointDescription("Retrieve details of a specific registration mode")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegistrationModeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RegistrationModeDto>> GetById(int id)
        {
            var registrationMode = await _mediator.Send(new GetRegistrationModeDetailsRequest { Id = id });
            return Ok(registrationMode);
        }
    }
}
