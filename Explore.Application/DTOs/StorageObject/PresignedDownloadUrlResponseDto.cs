using System;
using System.Collections.Generic;
using System.Text;

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
}
