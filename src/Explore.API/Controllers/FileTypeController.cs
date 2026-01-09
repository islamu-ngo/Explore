using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.FileType;
using Explore.Application.Features.FileTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FileTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FileTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/filetype
        [HttpGet]
        [EndpointSummary("Get all File Types")]
        [EndpointDescription("Retrieve a list of all file types (Image, Document, Video, Audio)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<FileTypeListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<FileTypeListDto>>> GetAll()
        {
            var fileTypes = await _mediator.Send(new GetFileTypeListRequest());
            return Ok(fileTypes);
        }

        // GET: api/v1/filetype/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get File Type by ID")]
        [EndpointDescription("Retrieve details of a specific file type")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FileTypeDto>> GetById(int id)
        {
            var fileType = await _mediator.Send(new GetFileTypeDetailsRequest { Id = id });
            return Ok(fileType);
        }
    }
}
