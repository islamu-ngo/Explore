using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class StorageObjectController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StorageObjectController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/storageobject
        [HttpGet]
        [EndpointSummary("Get all Storage Objects")]
        [EndpointDescription("Retrieve a list of all storage objects (files, images, documents, etc.)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<StorageObjectListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<StorageObjectListDto>>> GetAll()
        {
            var storageObjects = await _mediator.Send(new GetStorageObjectListRequest());
            return Ok(storageObjects);
        }

        // GET: api/v1/storageobject/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Storage Object by ID")]
        [EndpointDescription("Retrieve details of a specific storage object")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(StorageObjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StorageObjectDto>> GetById(Guid id)
        {
            var storageObject = await _mediator.Send(new GetStorageObjectDetailsRequest { Id = id });
            return Ok(storageObject);
        }

        // POST: api/v1/storageobject
        [HttpPost]
        [EndpointSummary("Upload Storage Object")]
        [EndpointDescription("Upload a new storage object (file/image/document)")]
        [Authorize]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateStorageObjectDto dto)
        {
            var command = new CreateStorageObjectCommand { StorageObjectDto = dto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT: api/v1/storageobject/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Storage Object")]
        [EndpointDescription("Update an existing storage object")]
        [Authorize]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateStorageObjectDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "Storage Object ID mismatch" });
            }

            var command = new UpdateStorageObjectCommand { StorageObjectDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/storageobject/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Delete Storage Object")]
        [EndpointDescription("Delete a storage object")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var command = new DeleteStorageObjectCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "Storage Object not found" });
            }

            return NoContent();
        }
    }
}
