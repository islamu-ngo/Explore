using System.Net.Http.Json;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

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
    private const long DefaultMaxFileSize = 10 * 1024 * 1024; // 10MB

    public ImageStorageService(IEventApiClient apiClient, IHttpClientFactory httpClientFactory, ILogger<ImageStorageService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<FileUploadData?> ReadFileAsync(IBrowserFile file, long maxFileSize = DefaultMaxFileSize)
    {
        if (file == null)
        {
            _logger.LogWarning("ReadFileAsync called with null file");
            return null;
        }

        try
        {
            _logger.LogInformation("Reading file into memory: {FileName}, Size: {Size} bytes, ContentType: {ContentType}",
                file.Name, file.Size, file.ContentType);

            if (file.Size > maxFileSize)
            {
                _logger.LogWarning("File {FileName} exceeds max size ({Size} > {MaxSize})",
                    file.Name, file.Size, maxFileSize);
                return null;
            }

            // Read file into memory immediately - this prevents stream exhaustion in WASM
            await using var stream = file.OpenReadStream(maxAllowedSize: maxFileSize);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            _logger.LogDebug("Successfully read {ByteCount} bytes from {FileName}", bytes.Length, file.Name);

            return new FileUploadData
            {
                Content = bytes,
                FileName = file.Name,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {FileName} into memory", file.Name);
            return null;
        }
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
                ExpiresInMinutes = response.ExpiresInMinutes ?? 60
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
            _logger.LogWarning("Invalid upload URL format: {UploadUrl} - aborting upload", uploadUrl);
            return false;
        }

        try
        {
            _logger.LogInformation("Uploading to S3: {FileName}, Size: {Size} bytes, ContentType: {ContentType}, Host: {Host}",
                file.Name, file.Size, file.ContentType, validatedUri.Host);

            // Use named HTTP client for S3 upload (configured with CORS mode for cross-origin requests)
            using var s3Client = _httpClientFactory.CreateClient("S3Upload");

            // Read the file content - stream directly without buffering to avoid memory issues
            const long maxFileSize = 10 * 1024 * 1024; // 10MB max
            await using var stream = file.OpenReadStream(maxAllowedSize: maxFileSize);
            using var content = new StreamContent(stream);
            
            // CRITICAL: Content-Type MUST match what was used to generate the pre-signed URL
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            _logger.LogDebug("Sending PUT request to S3...");

            // PUT request to the pre-signed URL
            var response = await s3Client.PutAsync(validatedUri, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("S3 upload failed: {StatusCode} - {ReasonPhrase} - {ErrorContent}",
                    (int)response.StatusCode, response.ReasonPhrase, errorContent);
                
                // Log additional debug info for common error codes
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogError("S3 403 Forbidden - Check: 1) CORS config on bucket 2) Pre-signed URL expired 3) Content-Type mismatch");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogError("S3 400 Bad Request - Check: 1) Content-Type header matches pre-signed URL 2) Request headers are correct");
                }
            }
            else
            {
                var etag = response.Headers.ETag?.Tag ?? "no-etag";
                _logger.LogInformation("S3 upload successful! ETag: {ETag}", etag);
            }

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error uploading to S3. This may be a CORS issue - check bucket CORS configuration.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to S3");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UploadImageFromBytesAsync(string uploadUrl, FileUploadData fileData)
    {
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            _logger.LogWarning("Invalid upload URL (empty/null) - aborting upload");
            return false;
        }

        if (fileData == null || fileData.Content.Length == 0)
        {
            _logger.LogWarning("Invalid file data (null or empty) - aborting upload");
            return false;
        }

        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var validatedUri))
        {
            _logger.LogWarning("Invalid upload URL format: {UploadUrl} - aborting upload", uploadUrl);
            return false;
        }

        try
        {
            _logger.LogInformation("Uploading to S3: {FileName}, Size: {Size} bytes, ContentType: {ContentType}, Host: {Host}",
                fileData.FileName, fileData.Size, fileData.ContentType, validatedUri.Host);

            // Use named HTTP client for S3 upload (configured with CORS mode)
            using var s3Client = _httpClientFactory.CreateClient("S3Upload");

            // Use ByteArrayContent instead of StreamContent for reliable WASM uploads
            // ByteArrayContent avoids stream timing/disposal issues in browser fetch API
            using var content = new ByteArrayContent(fileData.Content);

            // CRITICAL: Content-Type MUST match what was used to generate the pre-signed URL
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fileData.ContentType);

            _logger.LogDebug("Sending PUT request to S3 with ByteArrayContent ({Size} bytes)...", fileData.Size);

            // Use a cancellation token with timeout to prevent indefinite hangs
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var response = await s3Client.PutAsync(validatedUri, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("S3 upload failed: {StatusCode} - {ReasonPhrase} - {ErrorContent}",
                    (int)response.StatusCode, response.ReasonPhrase, errorContent);

                // Log additional debug info for common error codes
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogError("S3 403 Forbidden - Check: 1) CORS config on bucket 2) Pre-signed URL expired 3) Content-Type mismatch");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogError("S3 400 Bad Request - Check: 1) Content-Type header matches pre-signed URL 2) Request headers are correct");
                }

                return false;
            }

            var etag = response.Headers.ETag?.Tag ?? "no-etag";
            _logger.LogInformation("S3 upload successful! ETag: {ETag}, Size: {Size} bytes", etag, fileData.Size);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("S3 upload timed out after 3 minutes for file {FileName}", fileData.FileName);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error uploading to S3. This may be a CORS issue - check bucket CORS configuration. File: {FileName}", fileData.FileName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading image to S3. File: {FileName}", fileData.FileName);
            return false;
        }
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
            var uploadSuccess = await UploadImageFromBytesAsync(uploadResponse.UploadUrl, fileData);
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
            var createDto = new CreateStorageObjectDto
            {
                FileTypeId = GetFileTypeId(fileData.ContentType),
                Uri = uploadResponse.ViewUrl,
                FullName = fileData.FileName,
                Extension = Path.GetExtension(fileData.FileName),
                Size = fileData.Size
            };

            _logger.LogInformation("Creating StorageObject record for {FileName}", fileData.FileName);

            BaseCommandResponseOfGuid? createResponse = null;
            try
            {
                createResponse = await _apiClient.StorageObjectPOSTAsync(createDto);
                _logger.LogInformation("StorageObject API response received: Success={Success}, Id={Id}, Message={Message}",
                    createResponse?.Success, createResponse?.Id, createResponse?.Message);
            }
            catch (Exception apiEx)
            {
                _logger.LogError(apiEx, "Exception calling StorageObjectPOSTAsync for {FileName}", fileData.FileName);
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = $"API call failed: {apiEx.Message}"
                };
            }

            if (createResponse?.Success == true)
            {
                _logger.LogInformation("StorageObject created successfully! ID: {StorageObjectId}", createResponse.Id);
                var result = new ImageUploadResult
                {
                    Success = true,
                    StorageObjectId = createResponse.Id ?? Guid.Empty,
                    ViewUrl = uploadResponse.ViewUrl,
                    ObjectKey = uploadResponse.ObjectKey
                };
                _logger.LogInformation("Returning successful ImageUploadResult for {FileName}", fileData.FileName);
                return result;
            }
            else
            {
                var errorMsg = createResponse?.Message ?? "Failed to create storage object record";
                _logger.LogWarning("StorageObject creation failed: {Message}", errorMsg);
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = errorMsg
                };
            }
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
                    StorageObjectId = createResponse.Id ?? Guid.Empty,
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
            using var httpClient = _httpClientFactory.CreateClient("BffClient");
            var response = await httpClient.GetAsync($"/api/v1/StorageObject/presigned-url-by-key/{objectKey}?expirationMinutes=60");

            if (response.IsSuccessStatusCode)
            {
                var presignedResponse = await response.Content.ReadFromJsonAsync<PresignedDownloadUrlResponse>();
                if (presignedResponse != null && !string.IsNullOrEmpty(presignedResponse.PresignedUrl))
                {
                    return presignedResponse.PresignedUrl;
                }
            }

            _logger.LogWarning("Failed to get presigned URL for object key: {ObjectKey}, Status: {StatusCode}", objectKey, response.StatusCode);
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
            using var httpClient = _httpClientFactory.CreateClient("BffClient");
            var response = await httpClient.GetAsync($"/api/v1/StorageObject/{storageObjectId}/presigned-url?expirationMinutes=60");

            if (response.IsSuccessStatusCode)
            {
                var presignedResponse = await response.Content.ReadFromJsonAsync<PresignedDownloadUrlResponse>();
                if (presignedResponse != null && !string.IsNullOrEmpty(presignedResponse.PresignedUrl))
                {
                    return presignedResponse.PresignedUrl;
                }
            }

            _logger.LogWarning("Failed to get presigned URL for storage object ID: {Id}, Status: {StatusCode}", storageObjectId, response.StatusCode);
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

    /// <inheritdoc />
    public string GenerateLocalPreviewFromBytes(FileUploadData fileData)
    {
        if (fileData == null || fileData.Content.Length == 0)
        {
            _logger.LogWarning("GenerateLocalPreviewFromBytes called with null or empty file data");
            return string.Empty;
        }

        try
        {
            return $"data:{fileData.ContentType};base64,{Convert.ToBase64String(fileData.Content)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview from bytes");
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
