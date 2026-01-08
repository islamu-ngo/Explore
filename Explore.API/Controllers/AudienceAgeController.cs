using Explore.Application.DTOs.AudienceAge;
using Explore.Application.Features.AudienceAges.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AudienceAgeController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AudienceAgeController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/<AudienceAgeController>
        [HttpGet]
        [EndpointSummary("Get all Audience Ages Options")]
        [EndpointDescription("Get A List of all the Audience Ages Options")]
        [AllowAnonymous]
        public async Task<ActionResult<List<AudienceAgeListDto>>> GetAll()
        {
            var audienceAges = await _mediator.Send(new GetAudienceAgeListRequest());
            return Ok(audienceAges);
        }

        // GET api/<AudienceAgeController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<AudienceAgeController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<AudienceAgeController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AudienceAgeController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
