using System.Net.Http.Json;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Microsoft.AspNetCore.Components.Forms;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for handling image storage operations with S3 pre-signed URLs
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Get a pre-signed URL for uploading an image
    /// </summary>
    Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType);

    /// <summary>
    /// Upload an image file using a pre-signed URL
    /// </summary>
    Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file);

    /// <summary>
    /// Upload an image and create a StorageObject record, returning the storage object ID
    /// </summary>
    Task<ImageUploadResult?> UploadImageAndCreateRecordAsync(IBrowserFile file);

    /// <summary>
    /// Get a pre-signed URL for viewing an image
    /// </summary>
    Task<string?> GetImageUrlAsync(string imageKey);

    /// <summary>
    /// Delete an image from storage
    /// </summary>
    Task<bool> DeleteImageAsync(string imageKey);

    /// <summary>
    /// Generate a preview URL from a browser file (local preview before upload)
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

public class ImageStorageService : IImageStorageService
{
    private readonly IEventApiClient _apiClient;
    private readonly HttpClient _httpClient;

    public ImageStorageService(IEventApiClient apiClient, HttpClient httpClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType)
    {
        try
        {
            Console.WriteLine($"[IMAGE STORAGE] Getting upload URL for: {fileName}, type: {contentType}");
            
            var request = new UploadRequestDto
            {
                FileName = fileName,
                ContentType = contentType
            };
            
            var response = await _apiClient.GenerateUploadUrlAsync(request);

            // Defensive: server might return non-null DTO but with an empty UploadUrl.
            if (response == null)
            {
                Console.WriteLine("[IMAGE STORAGE] GenerateUploadUrlAsync returned null response");
                return null;
            }

            if (string.IsNullOrWhiteSpace(response.UploadUrl))
            {
                Console.WriteLine("[IMAGE STORAGE] UploadUrl is null or empty. Check server S3 configuration (bucket/endpoint/credentials)." );
                return null;
            }
            
            Console.WriteLine($"[IMAGE STORAGE] Got upload URL: {response.UploadUrl?.Substring(0, Math.Min(50, response.UploadUrl?.Length ?? 0))}...");
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
            Console.WriteLine($"[IMAGE STORAGE] API error getting upload URL: {ex.StatusCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IMAGE STORAGE] Error getting upload URL: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file)
    {
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            Console.WriteLine("[IMAGE STORAGE] Invalid upload URL (empty/null) - aborting upload.");
            return false;
        }

        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var validatedUri))
        {
            Console.WriteLine("[IMAGE STORAGE] Invalid upload URL format - aborting upload.");
            return false;
        }

        try
        {
            Console.WriteLine($"[IMAGE STORAGE] Uploading to S3: {file.Name}");
            
            // Create a new HttpClient for direct S3 upload (without auth headers)
            using var s3Client = new HttpClient();

            // Read the file content
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            // PUT request to the pre-signed URL
            var response = await s3Client.PutAsync(validatedUri, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[IMAGE STORAGE] S3 upload failed: {response.StatusCode} - {errorContent}");
            }
            else
            {
                Console.WriteLine("[IMAGE STORAGE] S3 upload successful");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IMAGE STORAGE] Error uploading image: {ex.Message}");
            return false;
        }
    }

    public async Task<ImageUploadResult?> UploadImageAndCreateRecordAsync(IBrowserFile file)
    {
        try
        {
            Console.WriteLine($"[IMAGE STORAGE] Starting upload process for: {file.Name}");
            
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

            Console.WriteLine($"[IMAGE STORAGE] Creating StorageObject record...");
            var createResponse = await _apiClient.StorageObjectPOSTAsync(createDto);

            if (createResponse?.Success == true)
            {
                Console.WriteLine($"[IMAGE STORAGE] StorageObject created with ID: {createResponse.Id}");
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
                Console.WriteLine($"[IMAGE STORAGE] StorageObject creation failed: {createResponse?.Message}");
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = createResponse?.Message ?? "Failed to create storage object record"
                };
            }
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[IMAGE STORAGE] API error: {ex.StatusCode} - {ex.Message}");
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = $"API error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IMAGE STORAGE] Error in upload process: {ex.Message}");
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string?> GetImageUrlAsync(string imageKey)
    {
        try
        {
            // For now, construct the URL directly
            // In production, you might want to call an API to get a fresh signed URL
            return imageKey;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IMAGE STORAGE] Error getting image URL: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string imageKey)
    {
        try
        {
            // TODO: Implement delete via API
            Console.WriteLine($"[IMAGE STORAGE] Delete not implemented for: {imageKey}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IMAGE STORAGE] Error deleting image: {ex.Message}");
            return false;
        }
    }

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
            Console.WriteLine($"[IMAGE STORAGE] Error generating preview: {ex.Message}");
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
