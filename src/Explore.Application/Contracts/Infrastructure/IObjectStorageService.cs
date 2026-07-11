// ABOUTME: Contract for S3-compatible object storage operations.
// Supports presigned URLs for browser-direct upload/download and server-side file retrieval.

using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.StorageObject;

namespace Explore.Application.Contracts.Infrastructure;

public interface IObjectStorageService
{
    /// <summary>
    /// Generates a pre-signed URL for uploading a file to S3-compatible storage.
    /// </summary>
    /// <param name="fileName">The name of the file to upload.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <returns>Response containing upload URL, object key, and view URL.</returns>
    Task<UploadUrlResponseDto> GeneratePresignedUploadUrl(string fileName, string contentType);

    /// <summary>
    /// Generates a pre-signed URL for downloading/viewing a file from S3-compatible storage.
    /// </summary>
    /// <param name="objectKey">The key of the object to retrieve.</param>
    /// <param name="expirationMinutes">URL expiration time in minutes (default: 60).</param>
    /// <returns>The presigned download URL.</returns>
    Task<string> GeneratePresignedDownloadUrl(string objectKey, int expirationMinutes = 60);

    /// <summary>
    /// Retrieves a file stream from S3-compatible storage.
    /// </summary>
    /// <param name="fileKey">The key of the file to retrieve.</param>
    /// <returns>A tuple containing the file stream and content type.</returns>
    Task<(Stream FileStream, string ContentType)> GetFileStream(string fileKey);

    /// <summary>
    /// Tests connectivity to the configured S3-compatible storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
