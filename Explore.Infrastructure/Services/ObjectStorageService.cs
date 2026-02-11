// ABOUTME: S3-compatible object storage service that resolves config per-tenant via cascading settings.
// Supports any S3-compatible provider (Hetzner, MinIO, AWS, Backblaze B2, Wasabi, R2, etc.).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Models;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services;

public class ObjectStorageService : IObjectStorageService
{
    private readonly IS3ConfigResolver _configResolver;
    private readonly ILogger<ObjectStorageService> _logger;

    public ObjectStorageService(IS3ConfigResolver configResolver, ILogger<ObjectStorageService> logger)
    {
        _configResolver = configResolver;
        _logger = logger;
    }

    public async Task<UploadUrlResponseDto> GeneratePresignedUploadUrl(string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("fileName must be provided", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("contentType must be provided", nameof(contentType));

        var config = await _configResolver.ResolveAsync();
        if (config is null)
            throw new InvalidOperationException("S3 storage is not configured. Configure S3 settings in the admin panel.");

        // Generate a unique object key with timestamp to prevent collisions
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);
        var sanitizedName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "-")
            .ToLowerInvariant();

        var objectKey = $"uploads/{timestamp}/{uniqueId}-{sanitizedName}{extension}";

        var presignClient = CreatePresignClient(config);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = config.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(config.UploadUrlExpirationMinutes),
            ContentType = contentType
        };

        string? uploadUrl;
        try
        {
            uploadUrl = presignClient.GetPreSignedURL(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate pre-signed upload URL. Verify S3 configuration and credentials.", ex);
        }

        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            throw new InvalidOperationException("Failed to generate pre-signed upload URL: the S3 client returned an empty URL. Verify bucket, endpoint and credentials.");
        }

        // Return the object key as view URL — full URL constructed at display time
        var viewUrl = objectKey;

        var response = new UploadUrlResponseDto
        {
            UploadUrl = uploadUrl,
            ObjectKey = objectKey,
            ViewUrl = viewUrl,
            ExpiresInMinutes = config.UploadUrlExpirationMinutes
        };

        return response;
    }

    public async Task<(Stream FileStream, string ContentType)> GetFileStream(string fileKey)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            throw new ArgumentException("fileKey must be provided", nameof(fileKey));

        var config = await _configResolver.ResolveAsync();
        if (config is null)
            throw new InvalidOperationException("S3 storage is not configured.");

        var client = CreateS3Client(config);

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = config.BucketName,
                Key = fileKey
            };

            var response = await client.GetObjectAsync(request);
            return (response.ResponseStream, response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"S3 object not found. Key: {fileKey}", ex);
        }
    }

    Task<(Stream FileStream, string ContentType)> IObjectStorageService.GetFileStream(string fileKey)
        => GetFileStream(fileKey);

    public async Task<string> GeneratePresignedDownloadUrl(string objectKey, int expirationMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("objectKey must be provided", nameof(objectKey));

        var config = await _configResolver.ResolveAsync();
        if (config is null)
            throw new InvalidOperationException("S3 storage is not configured.");

        var presignClient = CreatePresignClient(config);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = config.BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        string? downloadUrl;
        try
        {
            downloadUrl = presignClient.GetPreSignedURL(request);
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

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configResolver.ResolveAsync(cancellationToken);
        if (config is null)
            return false;

        try
        {
            var client = CreateS3Client(config);
            await client.ListBucketsAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "S3 connection test failed for endpoint {Endpoint}", config.Endpoint);
            return false;
        }
    }

    private static IAmazonS3 CreateS3Client(S3Configuration config)
    {
        var s3Config = new AmazonS3Config
        {
            ForcePathStyle = config.ForcePathStyle
        };

        var endpoint = config.Endpoint.Trim();
        if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = $"https://{endpoint}";
        }

        s3Config.ServiceURL = endpoint;
        s3Config.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        s3Config.AuthenticationRegion = string.IsNullOrWhiteSpace(config.Region) ? "us-east-1" : config.Region;

        return new AmazonS3Client(config.AccessKeyId, config.SecretAccessKey, s3Config);
    }

    private static IAmazonS3 CreatePresignClient(S3Configuration config)
    {
        // If no separate public endpoint, use the regular client for presigning
        if (string.IsNullOrWhiteSpace(config.PublicEndpoint))
        {
            return CreateS3Client(config);
        }

        if (string.Equals(config.PublicEndpoint.Trim(), config.Endpoint.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return CreateS3Client(config);
        }

        var s3Config = new AmazonS3Config
        {
            ForcePathStyle = config.ForcePathStyle
        };

        var endpoint = config.PublicEndpoint.Trim();
        if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = $"https://{endpoint}";
        }

        s3Config.ServiceURL = endpoint;
        s3Config.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        s3Config.AuthenticationRegion = string.IsNullOrWhiteSpace(config.Region) ? "us-east-1" : config.Region;

        return new AmazonS3Client(config.AccessKeyId, config.SecretAccessKey, s3Config);
    }
}
