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

namespace Explore.API.Controllers;

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
    [EndpointDescription("Retrieve a paginated list of all storage objects (files, images, documents, etc.). Default page size is 20, max is 100.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<StorageObjectListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<StorageObjectListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var storageObjects = await _mediator.Send(new GetStorageObjectListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });
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

        if (storageObject == null)
        {
            return NotFound(new { error = "Storage object not found" });
        }

        return Ok(storageObject);
    }

    // GET: api/v1/storageobject/file/{*fileKey}
    [HttpGet("file/{*fileKey}")]
    [EndpointSummary("Get File Content")]
    [EndpointDescription("Retrieve the content of a file from storage by its key")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)] // Cache for 1 day
    public async Task<IActionResult> GetFile(string fileKey)
    {
        if (string.IsNullOrEmpty(fileKey))
        {
            return BadRequest("File key cannot be empty.");
        }

        try
        {
            var result = await _mediator.Send(new GetStorageObjectFileRequest { FileKey = fileKey });
            return File(result.FileStream, result.ContentType, enableRangeProcessing: true);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("File not found.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An internal error occurred.");
        }
    }

    // GET: api/v1/storageobject/{id}/presigned-url
    [HttpGet("{id}/presigned-url")]
    [EndpointSummary("Get Presigned Download URL")]
    [EndpointDescription("Generate a time-limited presigned URL for viewing/downloading a file from S3-compatible storage")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PresignedDownloadUrlResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PresignedDownloadUrlResponseDto>> GetPresignedDownloadUrl(Guid id, [FromQuery] int expirationMinutes = 60)
    {
        var result = await _mediator.Send(new GetPresignedDownloadUrlRequest
        {
            Id = id,
            ExpirationMinutes = expirationMinutes
        });

        if (result == null)
        {
            return NotFound(new { error = "Storage object not found or could not generate presigned URL" });
        }

        return Ok(result);
    }

    // GET: api/v1/storageobject/presigned-url-by-key/{*objectKey}
    [HttpGet("presigned-url-by-key/{*objectKey}")]
    [EndpointSummary("Get Presigned Download URL by Key")]
    [EndpointDescription("Generate a time-limited presigned URL for viewing/downloading a file using its object key")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PresignedDownloadUrlResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PresignedDownloadUrlResponseDto>> GetPresignedDownloadUrlByKey(string objectKey, [FromQuery] int expirationMinutes = 60)
    {
        if (string.IsNullOrEmpty(objectKey))
        {
            return BadRequest(new { error = "Object key cannot be empty" });
        }

        var result = await _mediator.Send(new GetPresignedDownloadUrlByKeyRequest
        {
            ObjectKey = objectKey,
            ExpirationMinutes = expirationMinutes
        });

        if (result == null)
        {
            return StatusCode(500, new { error = "Failed to generate presigned URL" });
        }

        return Ok(result);
    }

    // POST: api/v1/storageobject/generate-upload-url
    [HttpPost("generate-upload-url")]
    [EndpointSummary("Generate Pre-signed Upload URL")]
    [EndpointDescription("Generate a pre-signed URL for uploading a file directly to S3-compatible storage (Hetzner Object Storage)")]
    [Authorize]
    [ProducesResponseType(typeof(UploadUrlResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UploadUrlResponseDto>> GenerateUploadUrl([FromBody] UploadRequestDto request)
    {
        var command = new GenerateUploadUrlCommand
        {
            FileName = request.FileName,
            ContentType = request.ContentType
        };
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // POST: api/v1/storageobject
    [HttpPost]
    [EndpointSummary("Create Storage Object Record")]
    [EndpointDescription("Create a storage object record after successful file upload to S3")]
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
