using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services;

public class ObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _s3Settings;
    private const int UploadUrlExpirationMinutes = 40; // DEVELOPMENT MODE! Reduce in production

    public ObjectStorageService(IAmazonS3 s3Client, IOptions<S3Settings> s3Settings)
    {
        _s3Client = s3Client;
        _s3Settings = s3Settings.Value;
    }

    public Task<UploadUrlResponseDto> GeneratePresignedUploadUrl(string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("fileName must be provided", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("contentType must be provided", nameof(contentType));

        if (_s3Settings == null)
            throw new InvalidOperationException("S3 settings are not configured.");

        if (string.IsNullOrWhiteSpace(_s3Settings.BucketName))
            throw new InvalidOperationException("S3 bucket name is not configured (S3Settings:BucketName).");

        // Generate a unique object key with timestamp to prevent collisions
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);
        var sanitizedName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "-")
            .ToLowerInvariant();

        var objectKey = $"uploads/{timestamp}/{uniqueId}-{sanitizedName}{extension}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(UploadUrlExpirationMinutes),
            ContentType = contentType
        };

        string? uploadUrl;
        try
        {
            uploadUrl = _s3Client.GetPreSignedURL(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate pre-signed upload URL. Verify S3 configuration and credentials.", ex);
        }

        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            throw new InvalidOperationException("Failed to generate pre-signed upload URL: the S3 client returned an empty URL. Verify bucket, endpoint and credentials.");
        }

        // Construct the public view URL
        var viewUrl = ConstructViewUrl(objectKey);

        var response = new UploadUrlResponseDto
        {
            UploadUrl = uploadUrl,
            ObjectKey = objectKey,
            ViewUrl = viewUrl,
            ExpiresInMinutes = UploadUrlExpirationMinutes
        };

        return Task.FromResult(response);
    }

    public async Task<(Stream FileStream, string ContentType)> GetFileStream(string fileKey)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            throw new ArgumentException("fileKey must be provided", nameof(fileKey));

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = fileKey
            };

            var response = await _s3Client.GetObjectAsync(request);
            return (response.ResponseStream, response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"S3 object not found. Key: {fileKey}", ex);
        }
    }

    Task<(Stream FileStream, string ContentType)> IObjectStorageService.GetFileStream(string fileKey)
        => GetFileStream(fileKey);

    public string GeneratePresignedDownloadUrl(string objectKey, int expirationMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("objectKey must be provided", nameof(objectKey));

        if (_s3Settings == null)
            throw new InvalidOperationException("S3 settings are not configured.");

        if (string.IsNullOrWhiteSpace(_s3Settings.BucketName))
            throw new InvalidOperationException("S3 bucket name is not configured (S3Settings:BucketName).");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        string? downloadUrl;
        try
        {
            downloadUrl = _s3Client.GetPreSignedURL(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate pre-signed download URL. Verify S3 configuration and credentials.", ex);
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            throw new InvalidOperationException("Failed to generate pre-signed download URL: the S3 client returned an empty URL.");
        }

        return downloadUrl;
    }

    private string ConstructViewUrl(string objectKey)
    {
        // Return the object key as a relative API path
        // The Blazor app will use this to fetch images through the StorageObject/file endpoint
        // This ensures images are always accessible (authenticated if needed) and not dependent on S3 public access
        // Format: /api/v1/StorageObject/file/{objectKey}
        // NOTE: We store just the object key. The full URL will be constructed at display time
        // by either generating a presigned S3 URL or routing through the API proxy.
        return objectKey;
    }
}
