// ABOUTME: Legacy request DTO for generating a direct presigned upload URL.
// ABOUTME: Carries only browser-supplied file name and content type, both validated before storage use.

namespace Explore.Application.DTOs.StorageObject;

public class UploadRequestDto
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
}
