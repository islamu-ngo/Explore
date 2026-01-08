using Explore.Application.DTOs.ProgramRegistration;
using Explore.Application.Features.ProgramRegistration.Requests.Commands;
using Explore.Application.Features.ProgramRegistration.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventRegistrationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<EventRegistrationController> _logger;

        public EventRegistrationController(IMediator mediator, ILogger<EventRegistrationController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // POST: api/ProgramRegistration
        [HttpPost]
        [EndpointSummary("Register for a Program/Event")]
        [EndpointDescription("Register a user for a specific program (event or education)")]
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

        // GET: api/ProgramRegistration/program/{programId}
        [HttpGet("program/{programId}")]
        [EndpointSummary("Get all registrations for a Program/Event")]
        [EndpointDescription("Get a list of all users registered for a specific program")]
        public async Task<ActionResult<List<ProgramRegistrationListDto>>> GetRegistrationsForProgram(Guid programId)
        {
            try
            {
                var query = new GetProgramRegistrationsRequest { ProgramId = programId };
                var registrations = await _mediator.Send(query);
                return Ok(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching registrations for program {ProgramId}", programId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/ProgramRegistration/my
        [HttpGet("my")]
        [EndpointSummary("Get current user's registrations")]
        [EndpointDescription("Return all program registrations for the authenticated user")]
        public async Task<ActionResult<List<ProgramRegistrationListDto>>> GetMyRegistrations()
        {
            try
            {
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User?.FindFirstValue("sub")
                    ?? string.Empty;

                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var query = new GetMyProgramRegistrationsRequest { UserId = userGuid };
                var registrations = await _mediator.Send(query);
                return Ok(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching registrations for current user");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // DELETE: api/ProgramRegistration/{registrationId}
        [HttpDelete("{registrationId}")]
        [EndpointSummary("Unregister from a Program/Event")]
        [EndpointDescription("Remove the current user's registration for a program")]
        public async Task<ActionResult> DeleteRegistration(Guid registrationId)
        {
            try
            {
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User?.FindFirstValue("sub")
                    ?? string.Empty;

                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var command = new DeleteProgramRegistrationCommand
                {
                    RegistrationId = registrationId,
                    UserId = userGuid
                };

                var result = await _mediator.Send(command);

                if (result.Success)
                {
                    return Ok(new { message = result.Message });
                }

                return BadRequest(new { message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting registration {RegistrationId}", registrationId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}