using System.Net.Http.Json;
using System.Text.Json;
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
    public string ImageKey { get; set; } = string.Empty;
    public string ViewUrl { get; set; } = string.Empty;
}

public class ImageStorageService : IImageStorageService
{
    private readonly HttpClient _httpClient;
    private readonly BffClient _bffClient;

    public ImageStorageService(BffClient bffClient, HttpClient httpClient)
    {
        _bffClient = bffClient;
        _httpClient = httpClient;
    }

    public async Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType)
    {
        try
        {
            // TODO: When backend is ready, uncomment this
            // var response = await _bffClient.GetAsync<ImageUploadResponse>(
            //     $"/api/storage/upload-url?fileName={Uri.EscapeDataString(fileName)}&contentType={Uri.EscapeDataString(contentType)}"
            // );
            // return response;
            
            // Temporary mock response for development
            return new ImageUploadResponse
            {
                UploadUrl = "https://mock-upload-url.com",
                ImageKey = $"images/{Guid.NewGuid()}/{fileName}",
                ViewUrl = "https://via.placeholder.com/400x300"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting upload URL: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file)
    {
        try
        {
            // TODO: When backend is ready, uncomment this
            // using var content = new MultipartFormDataContent();
            // var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
            // fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            // content.Add(fileContent, "file", file.Name);
            
            // var response = await _httpClient.PutAsync(uploadUrl, fileContent);
            // return response.IsSuccessStatusCode;
            
            // Temporary mock for development
            await Task.Delay(500); // Simulate upload
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading image: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> GetImageUrlAsync(string imageKey)
    {
        try
        {
            // TODO: When backend is ready, uncomment this
            // var response = await _bffClient.GetAsync<string>(
            //     $"/api/storage/view-url?imageKey={Uri.EscapeDataString(imageKey)}"
            // );
            // return response;
            
            // Temporary mock for development
            await Task.Delay(100);
            return $"https://via.placeholder.com/400x300?text={Uri.EscapeDataString(Path.GetFileName(imageKey))}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting image URL: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string imageKey)
    {
        try
        {
            // TODO: When backend is ready, uncomment this
            // var response = await _bffClient.DeleteAsync(
            //     $"/api/storage/image?imageKey={Uri.EscapeDataString(imageKey)}"
            // );
            // return response;
            
            // Temporary mock for development
            await Task.Delay(200);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting image: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GenerateLocalPreviewAsync(IBrowserFile file, long maxFileSize = 5 * 1024 * 1024)
    {
        try
        {
            var buffer = new byte[file.Size];
            await file.OpenReadStream(maxFileSize).ReadAsync(buffer);
            var base64 = Convert.ToBase64String(buffer);
            return $"data:{file.ContentType};base64,{base64}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating preview: {ex.Message}");
            return string.Empty;
        }
    }
}
