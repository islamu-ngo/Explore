// ABOUTME: Response contract for metadata-authorized presigned storage downloads.
// ABOUTME: Carries the short-lived URL plus safe presentation metadata without exposing provider keys.

namespace Explore.Application.DTOs.StorageObject;

/// <summary>
/// Response DTO for pre-signed download URL generation
/// </summary>
public class PresignedDownloadUrlResponseDto
{
    /// <summary>
    /// The pre-signed URL for downloading/viewing the file from S3
    /// </summary>
    public string PresignedUrl { get; set; } = string.Empty;

    /// <summary>
    /// The object key (path) of the file in storage
    /// </summary>
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>
    /// The expiration time of the pre-signed URL in minutes
    /// </summary>
    public int ExpiresInMinutes { get; set; }

    public string SafeDisplayName { get; set; } = "download";

    public bool ShouldDownloadAsAttachment { get; set; }
}
