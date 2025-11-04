using Explore.Application.DTOs.Program;
using Explore.Application.Features.Programs.Requests.Commands;
using Explore.Application.Features.Programs.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProgramController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProgramController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/<ProgramController>
        [HttpGet]
        [EndpointSummary("Get all Programs (Events, Education ...)")]
        [EndpointDescription("Get A List of all the Programs (pagination!)")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProgramListDto>>> GetAll()
        {
            var programs = await _mediator.Send(new GetProgramListRequest());
            return Ok(programs);
        }

        // GET api/<ProgramController>/5
        [HttpGet("{id}")]
        [EndpointSummary("Get Program (Event, Education...) Details")]
        [EndpointDescription("Get Details of the Program!")]
        [AllowAnonymous]
        public async Task<ActionResult<ProgramDto>> GetById(Guid id)
        {
            var program = await _mediator.Send(new GetProgramDetailsRequest());
            return Ok(program);
        }

        // POST api/<ProgramController>
        [HttpPost]
        [EndpointSummary("")]
        [EndpointDescription("")]
        [Authorize]
        //[Authorize(Roles = "OrgAdmin")] // not yet implemented when creating org to give admin role in keycloak with keycloak api! for later TODO
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateProgramDto program)
        {
            var command = new CreateProgramCommand { ProgramDto = program };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        //// PUT api/<ProgramController>/5
        //[HttpPut("{id}")]
        //[EndpointSummary("")]
        //[EndpointDescription("")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<ProgramController>/5
        //[HttpDelete("{id}")]
        //[EndpointSummary("")]
        //[EndpointDescription("")]
        //public void Delete(int id)
        //{
        //}
    }
}
