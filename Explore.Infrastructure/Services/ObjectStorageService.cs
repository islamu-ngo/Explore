using System;
using System.Collections.Generic;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject;
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

        var uploadUrl = _s3Client.GetPreSignedURL(request);

        // Construct the public view URL
        // For Hetzner S3, the format is: https://{bucket}.{endpoint}/{key}
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

    private string ConstructViewUrl(string objectKey)
    {
        // For Hetzner Object Storage, construct the public URL
        // Format: https://{bucket}.{endpoint}/{key}
        // Example: https://mybucket.fsn1.your-objectstorage.com/uploads/file.jpg

        if (string.IsNullOrEmpty(_s3Settings.Endpoint))
        {
            throw new InvalidOperationException("S3 Endpoint must be configured for Hetzner Object Storage");
        }

        // Parse the endpoint to construct the proper Hetzner URL
        var endpoint = _s3Settings.Endpoint.TrimEnd('/');

        // Remove protocol if present
        var endpointHost = endpoint
            .Replace("https://", "")
            .Replace("http://", "");

        // Hetzner format: https://{bucket}.{endpoint-host}/{key}
        return $"https://{_s3Settings.BucketName}.{endpointHost}/{objectKey}";
    }
}
