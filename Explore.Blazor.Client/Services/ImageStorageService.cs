using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Http;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Holds file data read from IBrowserFile for stable transfer across async operations.
/// Reading file bytes immediately prevents stream exhaustion issues in Blazor WASM.
/// </summary>
public sealed record FileUploadData
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Size => Content.Length;
}

/// <summary>
/// Service for handling image storage operations with S3 pre-signed URLs.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Reads a browser file into memory as FileUploadData.
    /// IMPORTANT: Call this immediately after file selection to prevent stream exhaustion.
    /// </summary>
    Task<FileUploadData?> ReadFileAsync(IBrowserFile file, long maxFileSize = 10 * 1024 * 1024);

    /// <summary>
    /// Get a pre-signed URL for uploading an image.
    /// </summary>
    Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType);

    /// <summary>
    /// Upload an image file using a pre-signed URL (legacy - uses IBrowserFile).
    /// Prefer UploadImageFromBytesAsync for reliable WASM uploads.
    /// </summary>
    Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file);

    /// <summary>
    /// Upload image bytes using a pre-signed URL.
    /// Uses ByteArrayContent for reliable WASM uploads (avoids stream timing issues).
    /// </summary>
    Task<bool> UploadImageFromBytesAsync(string uploadUrl, FileUploadData fileData);

    /// <summary>
    /// Upload an image and create a StorageObject record, returning the storage object ID.
    /// Legacy method using IBrowserFile - prefer UploadAndCreateRecordFromBytesAsync.
    /// </summary>
    Task<ImageUploadResult?> UploadImageAndCreateRecordAsync(IBrowserFile file);

    /// <summary>
    /// Upload image bytes and create a StorageObject record.
    /// Preferred method for reliable WASM uploads.
    /// </summary>
    Task<ImageUploadResult?> UploadAndCreateRecordFromBytesAsync(FileUploadData fileData);

    /// <summary>
    /// Get a pre-signed URL for viewing an image by its object key or full URI.
    /// </summary>
    Task<string?> GetImageUrlAsync(string imageKey);

    /// <summary>
    /// Get a pre-signed URL for viewing an image by its storage object ID.
    /// </summary>
    Task<string?> GetPresignedUrlByIdAsync(Guid storageObjectId);

    /// <summary>
    /// Delete an image from storage.
    /// </summary>
    Task<bool> DeleteImageAsync(string imageKey);

    /// <summary>
    /// Generate a preview URL from a browser file (local preview before upload).
    /// </summary>
    Task<string> GenerateLocalPreviewAsync(IBrowserFile file, long maxFileSize = 5 * 1024 * 1024);

    /// <summary>
    /// Generate a preview URL from file bytes.
    /// </summary>
    string GenerateLocalPreviewFromBytes(FileUploadData fileData);
}

public class ImageUploadResponse
{
    public string UploadUrl { get; set; } = string.Empty;
    public string UploadSessionId { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public string ViewUrl { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; }
}

public sealed class BffStorageUploadSessionResponse
{
    public string UploadSessionId { get; set; } = string.Empty;
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

public class PresignedDownloadUrlResponse
{
    public string PresignedUrl { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; }
}

/// <summary>
/// Implementation of image storage service using S3 pre-signed URLs.
/// </summary>
public class ImageStorageService : IImageStorageService
{
    private readonly IEventApiClient _apiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageStorageService> _logger;
    private readonly BffClient? _bffClient;
    private readonly IApiClientExecutor _apiClientExecutor;
    private readonly IImageFileReaderService _fileReader;
    private readonly IImagePreviewService _previewService;
    private readonly IImageUploadClient _uploadClient;
    private readonly IImageStorageRecordClient _storageRecordClient;
    private const long DefaultMaxFileSize = 10 * 1024 * 1024; // 10MB

    public ImageStorageService(
        IEventApiClient apiClient,
        IHttpClientFactory httpClientFactory,
        ILogger<ImageStorageService> logger,
        BffClient? bffClient = null,
        IApiClientExecutor? apiClientExecutor = null,
        IImageFileReaderService? fileReader = null,
        IImagePreviewService? previewService = null,
        IImageContentClassifier? contentClassifier = null,
        IImageUploadClient? uploadClient = null,
        IImageStorageRecordClient? storageRecordClient = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bffClient = bffClient;
        _apiClientExecutor = apiClientExecutor ?? new ApiClientExecutor();
        _fileReader = fileReader ?? new ImageFileReaderService(NullLogger<ImageFileReaderService>.Instance);
        _previewService = previewService ?? new ImagePreviewService(NullLogger<ImagePreviewService>.Instance);
        var classifier = contentClassifier ?? new ImageContentClassifier();
        _uploadClient = uploadClient ?? new ImageUploadClient(
            _apiClient,
            _httpClientFactory,
            NullLogger<ImageUploadClient>.Instance,
            _bffClient,
            _apiClientExecutor);
        _storageRecordClient = storageRecordClient ?? new ImageStorageRecordClient(
            _apiClient,
            classifier,
            NullLogger<ImageStorageRecordClient>.Instance);
    }

    /// <inheritdoc />
    public async Task<FileUploadData?> ReadFileAsync(IBrowserFile file, long maxFileSize = DefaultMaxFileSize)
    {
        return await _fileReader.ReadFileAsync(file, maxFileSize);
    }

    /// <inheritdoc />
    public async Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType)
    {
        return await _uploadClient.GetUploadUrlAsync(fileName, contentType);
    }

    /// <inheritdoc />
    public async Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file)
    {
        return await _uploadClient.UploadImageAsync(uploadUrl, file);
    }

    /// <inheritdoc />
    public async Task<bool> UploadImageFromBytesAsync(string uploadUrl, FileUploadData fileData)
    {
        return await _uploadClient.UploadImageFromBytesAsync(uploadUrl, fileData);
    }

    /// <inheritdoc />
    public async Task<ImageUploadResult?> UploadAndCreateRecordFromBytesAsync(FileUploadData fileData)
    {
        if (fileData == null || fileData.Content.Length == 0)
        {
            _logger.LogWarning("UploadAndCreateRecordFromBytesAsync called with null or empty file data");
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = "No file data provided"
            };
        }

        try
        {
            _logger.LogInformation("Starting byte-based upload process for: {FileName} ({Size} bytes)",
                fileData.FileName, fileData.Size);

            // Step 1: Get pre-signed upload URL
            var uploadResponse = await GetUploadUrlAsync(fileData.FileName, fileData.ContentType);
            if (uploadResponse == null)
            {
                _logger.LogError("Failed to get pre-signed URL for file {FileName}", fileData.FileName);
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to get pre-signed URL. Please check your authentication and try again."
                };
            }

            _logger.LogDebug("Got pre-signed URL. ObjectKey: {ObjectKey}", uploadResponse.ObjectKey);

            // Step 2: Upload to S3 using bytes
            var uploadSuccess = OperatingSystem.IsBrowser() && !string.IsNullOrWhiteSpace(uploadResponse.UploadSessionId)
                ? await _uploadClient.UploadViaBffProxyAsync(uploadResponse.UploadSessionId, fileData)
                : await UploadImageFromBytesAsync(uploadResponse.UploadUrl, fileData);
            if (!uploadSuccess)
            {
                _logger.LogError("S3 upload failed for file {FileName}", fileData.FileName);
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to upload image to storage. Please check your connection and try again."
                };
            }

            _logger.LogDebug("S3 upload completed successfully for {FileName}", fileData.FileName);

            // Step 3: Create StorageObject record
            return await _storageRecordClient.CreateRecordFromBytesAsync(uploadResponse, fileData);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error during upload process: {StatusCode}", ex.StatusCode);
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = $"API error ({ex.StatusCode}): {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in upload process for file {FileName}", fileData.FileName);
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
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
            var uploadSuccess = OperatingSystem.IsBrowser() && !string.IsNullOrWhiteSpace(uploadResponse.UploadSessionId)
                ? await _uploadClient.UploadViaBffProxyAsync(uploadResponse.UploadSessionId, file)
                : await UploadImageAsync(uploadResponse.UploadUrl, file);
            if (!uploadSuccess)
            {
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to upload image to storage"
                };
            }

            // Step 3: Create StorageObject record
            return await _storageRecordClient.CreateRecordFromFileAsync(uploadResponse, file);
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
    public async Task<string?> GetImageUrlAsync(string imageKey)
    {
        if (string.IsNullOrEmpty(imageKey))
        {
            return null;
        }

        try
        {
            string objectKey;

            if (Uri.TryCreate(imageKey, UriKind.Absolute, out var uri))
            {
                // The object key is the path part of the URI, without the leading slash.
                objectKey = uri.AbsolutePath.TrimStart('/');
            }
            else
            {
                // If it's not a full URI, it might already be just the key.
                objectKey = imageKey;
            }

            // Get presigned URL from the API via BFF
            var result = await _apiClientExecutor.ReadJsonAsync<PresignedDownloadUrlResponse>(
                ct => _httpClientFactory.CreateClient("BffClient").GetAsync($"/api/StorageObject/presigned-url-by-key/{objectKey}?expirationMinutes=60", ct),
                "BFF presigned URL by key");

            if (result.IsSuccess)
            {
                var presignedResponse = result.Value;
                if (presignedResponse != null && !string.IsNullOrEmpty(presignedResponse.PresignedUrl))
                {
                    return presignedResponse.PresignedUrl;
                }
            }

            _logger.LogWarning("Failed to get presigned URL for object key: {ObjectKey}, Status: {StatusCode}", objectKey, result.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting presigned URL");
            return null;
        }
    }

    /// <summary>
    /// Get a presigned URL for a storage object by its ID.
    /// </summary>
    public async Task<string?> GetPresignedUrlByIdAsync(Guid storageObjectId)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<PresignedDownloadUrlResponse>(
                ct => _httpClientFactory.CreateClient("BffClient").GetAsync($"/api/StorageObject/{storageObjectId}/presigned-url?expirationMinutes=60", ct),
                "BFF presigned URL by id");

            if (result.IsSuccess)
            {
                var presignedResponse = result.Value;
                if (presignedResponse != null && !string.IsNullOrEmpty(presignedResponse.PresignedUrl))
                {
                    return presignedResponse.PresignedUrl;
                }
            }

            _logger.LogWarning("Failed to get presigned URL for storage object ID: {Id}, Status: {StatusCode}", storageObjectId, result.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting presigned URL by ID");
            return null;
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
        return await _previewService.GenerateLocalPreviewAsync(file, maxFileSize);
    }

    /// <inheritdoc />
    public string GenerateLocalPreviewFromBytes(FileUploadData fileData)
    {
        return _previewService.GenerateLocalPreviewFromBytes(fileData);
    }

}
