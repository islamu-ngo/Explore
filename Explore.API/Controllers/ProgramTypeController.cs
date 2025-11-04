using Explore.Application.DTOs.ProgramType;
using Explore.Application.Features.ProgramTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProgramTypeController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProgramTypeController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/<ProgramTypeController>
        [HttpGet]
        [EndpointSummary("Get all Program Types")]
        [EndpointDescription("Get A List of all the Program Type Options")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProgramTypeListDto>>> GetAll()
        {
            var programTypes = await _mediator.Send(new GetProgramTypeListRequest());
            return Ok(programTypes);
        }

        // GET api/<ProgramTypeController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ProgramTypeController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ProgramTypeController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ProgramTypeController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
