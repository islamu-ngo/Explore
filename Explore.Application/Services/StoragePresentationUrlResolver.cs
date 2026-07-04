// ABOUTME: Shared helper for turning stored image references into browser-safe presentation URLs.
// ABOUTME: Avoids signing arbitrary URI paths and keeps raw storage references out of logs.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject.Validators;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Services;

public static class StoragePresentationUrlResolver
{
    private const int ImageUrlExpirationMinutes = 60;
    private const string StorageObjectApiPathPrefix = "/api/storageobject/";

    public static async Task<string?> ResolveImageUrlAsync(
        string? objectKeyOrUri,
        IObjectStorageService objectStorageService,
        ILogger logger,
        string imageContext)
    {
        var candidate = objectKeyOrUri?.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (candidate.StartsWith('/'))
        {
            return IsStorageObjectApiPath(candidate) ? candidate : null;
        }

        if (candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return candidate;
        }

        if (!StorageObjectMetadataValidation.BeValidObjectKey(candidate))
        {
            logger.LogWarning("Rejected unsafe storage image reference for {ImageContext}.", imageContext);
            return null;
        }

        try
        {
            return await objectStorageService.GeneratePresignedDownloadUrl(candidate, ImageUrlExpirationMinutes);
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Failed to generate presigned URL for {ImageContext}. FailureType={FailureType}",
                imageContext,
                CategorizeSigningFailure(ex));
            return null;
        }
    }

    private static bool IsStorageObjectApiPath(string path)
        => path.StartsWith(StorageObjectApiPathPrefix, StringComparison.OrdinalIgnoreCase);

    private static string CategorizeSigningFailure(Exception exception) =>
        exception switch
        {
            TimeoutException => "timeout",
            InvalidOperationException => "provider_unavailable",
            IOException => "provider_io",
            ArgumentException => "invalid_provider_request",
            _ => "unknown"
        };
}
