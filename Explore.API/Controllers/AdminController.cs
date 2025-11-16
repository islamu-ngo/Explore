using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Explore.Application.DTOs.Admin;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Application.Features.Organizations.Requests.Commands;

namespace Explore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Voor nu, later authentificatie toevoegen
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all organization requests voor admin dashboard
    /// </summary>
    [HttpGet("organizations")]
    public async Task<ActionResult<List<AdminOrganizationListDto>>> GetOrganizationRequests()
    {
        try
        {
            var request = new GetOrganizationListRequest();
            var organizations = await _mediator.Send(request);
            
            // Map naar admin DTO met StatusName
            // Manual mapping since we need StatusType.FullName
            var adminOrgs = organizations.Select(org => new AdminOrganizationListDto
            {
                Id = org.Id,
                FullName = org.FullName,
                Email = org.Email,
                WebsiteUrl = org.WebsiteUrl,
                Country = org.Country,
                City = org.City,
                Postcode = org.Postcode,
                Address = org.Address,
                StatusTypeId = org.StatusTypeId,
                StatusName = GetStatusName(org.StatusTypeId) // Mapping via helper
            }).ToList();

            return Ok(adminOrgs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Fout bij ophalen organisaties: {ex.Message}");
        }
    }

    /// <summary>
    /// Get details van een specifieke organisatie voor admin
    /// </summary>
    [HttpGet("organizations/{id:guid}")]
    public async Task<IActionResult> GetOrganizationDetails(Guid id)
    {
        try
        {
            var organizations = await _mediator.Send(new GetOrganizationListRequest());
            var organization = organizations.FirstOrDefault(o => o.Id == id);
            
            if (organization == null)
            {
                return NotFound();
            }

            var adminOrg = new AdminOrganizationListDto
            {
                Id = organization.Id,
                FullName = organization.FullName,
                Email = organization.Email,
                WebsiteUrl = organization.WebsiteUrl,
                Country = organization.Country,
                City = organization.City,
                Postcode = organization.Postcode,
                Address = organization.Address,
                StatusTypeId = organization.StatusTypeId,
                StatusName = GetStatusName(organization.StatusTypeId)
            };

            return Ok(adminOrg);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Fout bij ophalen organisatie details: {ex.Message}");
        }
    }

    /// <summary>
    /// Update organization status (approve/reject/pending)
    /// </summary>
    [HttpPut("organizations/{id}/status")]
    public async Task<ActionResult> UpdateOrganizationStatus(Guid id, [FromBody] UpdateOrganizationStatusDto updateDto)
    {
        try
        {
            var command = new UpdateOrganizationCommand
            {
                Id = id,
                OrganizationStatusTypeDto = new UpdateOrganizationStatusTypeDto 
                { 
                    StatusTypeId = updateDto.StatusTypeId 
                }
            };

            await _mediator.Send(command);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Fout bij updaten organisatie status: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper om status ID naar naam te mappen
    /// </summary>
    private static string GetStatusName(int statusTypeId)
    {
        return statusTypeId switch
        {
            1 => "Pending",
            2 => "Approved", 
            3 => "Rejected",
            _ => "Unknown"
        };
    }
}