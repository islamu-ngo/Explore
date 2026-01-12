using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.StorageObject;

namespace Explore.Application.Contracts.Infrastructure;

public interface IObjectStorageService
{
    /// <summary>
    /// Generates a pre-signed URL for uploading a file to S3-compatible storage
    /// </summary>
    /// <param name="fileName">The name of the file to upload</param>
    /// <param name="contentType">The MIME content type of the file</param>
    /// <returns>Response containing upload URL, object key, and view URL</returns>
    Task<UploadUrlResponseDto> GeneratePresignedUploadUrl(string fileName, string contentType);
}
