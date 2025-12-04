using Explore.Application.DTOs.ProgramRegistration;
using Explore.Application.Features.ProgramRegistration.Requests.Commands;
using Explore.Application.Features.ProgramRegistration.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Explore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProgramRegistrationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProgramRegistrationController> _logger;

        public ProgramRegistrationController(IMediator mediator, ILogger<ProgramRegistrationController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // POST: api/ProgramRegistration
        [HttpPost]
        [EndpointSummary("Register for a Program/Event")]
        [EndpointDescription("Register a user for a specific program (event or education)")]
        [AllowAnonymous] // Temporary for testing - should be [Authorize] in production
        public async Task<ActionResult<Guid>> CreateRegistration([FromBody] CreateProgramRegistrationDto registrationDto)
        {
            try
            {
                // Get user ID from claims (when authentication is enabled)
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                    ?? User?.FindFirstValue("sub") 
                    ?? "00000000-0000-0000-0000-000000000001"; // Fixed test user ID

                var command = new CreateProgramRegistrationCommand
                {
                    ProgramRegistrationDto = registrationDto,
                    UserId = userId
                };

                var result = await _mediator.Send(command);

                if (result.Success)
                {
                    return Ok(new { id = result.Id, message = result.Message });
                }

                return BadRequest(new { message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating program registration");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/ProgramRegistration/check/{programId}
        [HttpGet("check/{programId}")]
        [EndpointSummary("Check if user is registered for a Program/Event")]
        [EndpointDescription("Check if the current user is already registered for a specific program")]
        [AllowAnonymous] // Temporary for testing - should be [Authorize] in production
        public async Task<ActionResult<bool>> CheckRegistrationStatus(Guid programId)
        {
            try
            {
                // Get user ID from claims (when authentication is enabled)
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                    ?? User?.FindFirstValue("sub") 
                    ?? "00000000-0000-0000-0000-000000000001"; // Fixed test user ID

                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var query = new CheckUserRegistrationStatusRequest
                {
                    UserId = userGuid,
                    ProgramId = programId
                };

                var isRegistered = await _mediator.Send(query);
                return Ok(new { isRegistered });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking registration status for program {ProgramId}", programId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}