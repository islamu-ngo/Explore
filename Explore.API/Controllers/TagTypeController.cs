using Explore.Application.DTOs.TagType;
using Explore.Application.Features.TagTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TagTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TagTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/tagtype
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<TagTypeListDto>>> GetAll()
        {
            var tagTypes = await _mediator.Send(new GetTagTypeListRequest());
            return Ok(tagTypes);
        }

        // GET: api/v1/tagtype/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<TagTypeDto>> GetById(int id)
        {
            var tagType = await _mediator.Send(new GetTagTypeDetailsRequest { Id = id });
            return Ok(tagType);
        }
    }
}
