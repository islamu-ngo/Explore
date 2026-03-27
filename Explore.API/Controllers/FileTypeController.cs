// ABOUTME: API controller for file type lookup table (read-only enumeration).
// ABOUTME: Provides allowed file types for storage object uploads and validation.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.FileType;
using Explore.Application.Features.FileTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class FileTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public FileTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/filetype
    [HttpGet]
    [EndpointSummary("Get all File Types")]
    [EndpointDescription("Retrieve a list of all file types (Image, Document, Video, Audio)")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<FileTypeListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<FileTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var fileTypes = await _mediator.Send(new GetFileTypeListRequest(), cancellationToken);
        return Ok(fileTypes);
    }

    // GET: api/filetype/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get File Type by ID")]
    [EndpointDescription("Retrieve details of a specific file type")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<FileTypeDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var fileType = await _mediator.Send(new GetFileTypeDetailsRequest { Id = id }, cancellationToken);
        return Ok(fileType);
    }
}
