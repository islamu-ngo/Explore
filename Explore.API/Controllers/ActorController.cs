using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Application.Responses;

namespace Explore.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class ActorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ActorController> _logger;

    public ActorController(
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ActorController> logger)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // GET: api/v1/actor
    [HttpGet]
    [EndpointSummary("Get all Actors")]
    [EndpointDescription("Retrieve a paginated list of all actors. Default page size is 20, max is 100.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<ActorListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ActorListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var actors = await _mediator.Send(new GetActorListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        return Ok(actors);
    }

    // GET: api/v1/actor/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ActorDto>> GetById(Guid id)
    {
        var actor = await _mediator.Send(new GetActorDetailsRequest { Id = id });
        return Ok(actor);
    }

    // GET: api/v1/actor/by-did/{did}
    [HttpGet("by-did/{did}")]
    [AllowAnonymous]
    public async Task<ActionResult<ActorDto>> GetByDid(string did)
    {
        var actor = await _mediator.Send(new GetActorByDidRequest { Did = did });
        return Ok(actor);
    }

    // GET: api/v1/actor/by-tenant/{tenantId}
    [HttpGet("by-tenant/{tenantId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ActorListDto>>> GetByTenant(Guid tenantId)
    {
        var actors = await _mediator.Send(new GetActorsByTenantRequest { TenantId = tenantId });
        return Ok(actors);
    }

    // POST: api/v1/actor
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateActorDto dto)
    {
        var command = new CreateActorCommand { ActorDto = dto };
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // PUT: api/v1/actor/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateActorDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "Actor ID mismatch" });
        }

        var command = new UpdateActorCommand { ActorDto = dto };
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/v1/actor/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id)
    {
        var command = new DeleteActorCommand { Id = id };
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound(new { error = "Actor not found or you don't have permission to delete it" });
        }

        return NoContent();
    }
}
