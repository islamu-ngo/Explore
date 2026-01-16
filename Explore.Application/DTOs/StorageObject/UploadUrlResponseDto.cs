using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.StorageObject;

/// <summary>
/// Response DTO for pre-signed upload URL generation
/// </summary>
public class UploadUrlResponseDto
{
    /// <summary>
    /// The pre-signed URL for uploading the file directly to S3
    /// </summary>
    public string UploadUrl { get; set; } = string.Empty;

    /// <summary>
    /// The object key (path) where the file will be stored
    /// </summary>
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>
    /// The public view URL for the uploaded file
    /// </summary>
    public string ViewUrl { get; set; } = string.Empty;

    /// <summary>
    /// The expiration time of the pre-signed URL in minutes
    /// </summary>
    public int ExpiresInMinutes { get; set; }
}
