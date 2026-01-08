using Explore.Application.DTOs.AudienceGender;
using Explore.Application.Features.AudienceGenders.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AudienceGenderController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AudienceGenderController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/<AudienceGenderController>
        [HttpGet]
        [EndpointSummary("Get all Audience Gender Options")]
        [EndpointDescription("Get A List of all the Audience Gender Options")]
        [AllowAnonymous]
        public async Task<ActionResult<List<AudienceGenderListDto>>> GetAll()
        {
            var audienceGenders = await _mediator.Send(new GetAudienceGenderListRequest());
            return Ok(audienceGenders);
        }

        // GET api/<AudienceGenderController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<AudienceGenderController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<AudienceGenderController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AudienceGenderController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
