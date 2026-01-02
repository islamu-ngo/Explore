using Explore.Application.DTOs.StatusType;
using Explore.Application.Features.StatusTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalStatusController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApprovalStatusController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/<StatusTypeController>
        [HttpGet]
        [EndpointSummary("Get all Status Types")]
        [EndpointDescription("Get A List of all the Status Type Options")]
        [AllowAnonymous] //allow anonymous in case i want in beginning to let unverified org publish programs en ban them if necessery, then when there is lot's then change this business logic
        public async Task<ActionResult<List<StatusTypeListDto>>> GetAll()
        {
            var statusTypes = await _mediator.Send(new GetStatusTypeListRequest());
            return Ok(statusTypes);
        }

        // GET api/<StatusTypeController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<StatusTypeController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<StatusTypeController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<StatusTypeController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
