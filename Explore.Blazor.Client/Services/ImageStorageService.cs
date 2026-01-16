using System.Net.Http.Json;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for handling image storage operations with S3 pre-signed URLs.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Get a pre-signed URL for uploading an image.
    /// </summary>
    Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType);

    /// <summary>
    /// Upload an image file using a pre-signed URL.
    /// </summary>
    Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file);

    /// <summary>
    /// Upload an image and create a StorageObject record, returning the storage object ID.
    /// </summary>
    Task<ImageUploadResult?> UploadImageAndCreateRecordAsync(IBrowserFile file);

    /// <summary>
    /// Get a pre-signed URL for viewing an image.
    /// </summary>
    Task<string?> GetImageUrlAsync(string imageKey);

    /// <summary>
    /// Delete an image from storage.
    /// </summary>
    Task<bool> DeleteImageAsync(string imageKey);

    /// <summary>
    /// Generate a preview URL from a browser file (local preview before upload).
    /// </summary>
    Task<string> GenerateLocalPreviewAsync(IBrowserFile file, long maxFileSize = 5 * 1024 * 1024);
}

public class ImageUploadResponse
{
    public string UploadUrl { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public string ViewUrl { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; }
}

public class ImageUploadResult
{
    public Guid StorageObjectId { get; set; }
    public string ViewUrl { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Implementation of image storage service using S3 pre-signed URLs.
/// </summary>
public class ImageStorageService : IImageStorageService
{
    private readonly IEventApiClient _apiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(IEventApiClient apiClient, IHttpClientFactory httpClientFactory, ILogger<ImageStorageService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType)
    {
        try
        {
            _logger.LogInformation("Getting upload URL for: {FileName}, type: {ContentType}", fileName, contentType);

            var request = new UploadRequestDto
            {
                FileName = fileName,
                ContentType = contentType
            };

            var response = await _apiClient.GenerateUploadUrlAsync(request);

            // Defensive: server might return non-null DTO but with an empty UploadUrl.
            if (response == null)
            {
                _logger.LogWarning("GenerateUploadUrlAsync returned null response");
                return null;
            }

            if (string.IsNullOrWhiteSpace(response.UploadUrl))
            {
                _logger.LogWarning("UploadUrl is null or empty. Check server S3 configuration (bucket/endpoint/credentials)");
                return null;
            }

            _logger.LogDebug("Got upload URL: {UploadUrlPreview}...", response.UploadUrl?.Substring(0, Math.Min(50, response.UploadUrl?.Length ?? 0)));
            return new ImageUploadResponse
            {
                UploadUrl = response.UploadUrl ?? string.Empty,
                ObjectKey = response.ObjectKey ?? string.Empty,
                ViewUrl = response.ViewUrl ?? string.Empty,
                ExpiresInMinutes = response.ExpiresInMinutes
            };
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error getting upload URL: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload URL");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file)
    {
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            _logger.LogWarning("Invalid upload URL (empty/null) - aborting upload");
            return false;
        }

        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var validatedUri))
        {
            _logger.LogWarning("Invalid upload URL format - aborting upload");
            return false;
        }

        try
        {
            _logger.LogInformation("Uploading to S3: {FileName}", file.Name);

            // Use named HTTP client for S3 upload (configured with proper timeout)
            using var s3Client = _httpClientFactory.CreateClient("S3Upload");

            // Read the file content
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            // PUT request to the pre-signed URL
            var response = await s3Client.PutAsync(validatedUri, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("S3 upload failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            }
            else
            {
                _logger.LogInformation("S3 upload successful");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ImageUploadResult?> UploadImageAndCreateRecordAsync(IBrowserFile file)
    {
        try
        {
            _logger.LogInformation("Starting upload process for: {FileName}", file.Name);

            // Step 1: Get pre-signed upload URL
            var uploadResponse = await GetUploadUrlAsync(file.Name, file.ContentType);
            if (uploadResponse == null)
            {
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to get pre-signed URL"
                };
            }

            // Step 2: Upload to S3
            var uploadSuccess = await UploadImageAsync(uploadResponse.UploadUrl, file);
            if (!uploadSuccess)
            {
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to upload image to storage"
                };
            }

            // Step 3: Create StorageObject record
            var createDto = new CreateStorageObjectDto
            {
                FileTypeId = GetFileTypeId(file.ContentType),
                Uri = uploadResponse.ViewUrl,
                FullName = file.Name,
                Extension = Path.GetExtension(file.Name),
                Size = file.Size
            };

            _logger.LogInformation("Creating StorageObject record");
            var createResponse = await _apiClient.StorageObjectPOSTAsync(createDto);

            if (createResponse?.Success == true)
            {
                _logger.LogInformation("StorageObject created with ID: {StorageObjectId}", createResponse.Id);
                return new ImageUploadResult
                {
                    Success = true,
                    StorageObjectId = createResponse.Id,
                    ViewUrl = uploadResponse.ViewUrl,
                    ObjectKey = uploadResponse.ObjectKey
                };
            }
            else
            {
                _logger.LogWarning("StorageObject creation failed: {Message}", createResponse?.Message);
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = createResponse?.Message ?? "Failed to create storage object record"
                };
            }
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error: {StatusCode}", ex.StatusCode);
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = $"API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in upload process");
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public Task<string?> GetImageUrlAsync(string imageKey)
    {
        if (string.IsNullOrEmpty(imageKey))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            if (Uri.TryCreate(imageKey, UriKind.Absolute, out var uri))
            {
                // The object key is the path part of the URI, without the leading slash.
                var objectKey = uri.AbsolutePath.TrimStart('/');

                // Construct the relative URL to our proxy controller
                var proxyUrl = $"/api/v1/ImageProxy/{objectKey}";
                return Task.FromResult<string?>(proxyUrl);
            }

            // If it's not a full URI, it might already be just the key.
            // This provides a fallback.
            return Task.FromResult<string?>($"/api/v1/ImageProxy/{imageKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error constructing image proxy URL");
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteImageAsync(string imageKey)
    {
        try
        {
            // TODO: Implement delete via API
            _logger.LogWarning("Delete not implemented for: {ImageKey}", imageKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> GenerateLocalPreviewAsync(IBrowserFile file, long maxFileSize = 5 * 1024 * 1024)
    {
        try
        {
            var resizedImage = await file.RequestImageFileAsync(file.ContentType, 400, 400);
            using var stream = resizedImage.OpenReadStream(maxFileSize);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            return $"data:{file.ContentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview");
            return string.Empty;
        }
    }

    private int GetFileTypeId(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/jpeg" or "image/jpg" => 1,
            "image/png" => 2,
            "image/gif" => 3,
            "image/webp" => 4,
            "image/svg+xml" => 5,
            _ => 1 // Default to JPEG
        };
    }
}

// Local DTO for parsing the response (matches BaseCommandResponse<Guid>)
internal class BaseCommandResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid Id { get; set; }
    public List<string>? Errors { get; set; }
}
