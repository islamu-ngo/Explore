using Explore.Application.DTOs.Language;
using Explore.Application.Features.Languages.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LanguageController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LanguageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/language
        [HttpGet]
        [EndpointSummary("Get all Languages")]
        [EndpointDescription("Get a list of all available languages (lookup table)")]
        [AllowAnonymous]
        public async Task<ActionResult<List<LanguageListDto>>> GetAll()
        {
            var languages = await _mediator.Send(new GetLanguageListRequest());
            return Ok(languages);
        }

        // GET: api/v1/language/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Language Details")]
        [EndpointDescription("Get detailed information about a specific language")]
        [AllowAnonymous]
        public async Task<ActionResult<LanguageDto>> GetById(int id)
        {
            var language = await _mediator.Send(new GetLanguageDetailsRequest { Id = id });

            if (language == null)
            {
                return NotFound(new { error = "Language not found" });
            }

            return Ok(language);
        }
    }
}
