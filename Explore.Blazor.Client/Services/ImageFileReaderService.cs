// ABOUTME: Reads browser-selected files into stable byte buffers for client upload flows.
// ABOUTME: Isolates IBrowserFile stream handling from ImageStorageService orchestration.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface IImageFileReaderService
{
    Task<FileUploadData?> ReadFileAsync(IBrowserFile file, long maxFileSize);
}

public sealed class ImageFileReaderService(ILogger<ImageFileReaderService> logger) : IImageFileReaderService
{
    public async Task<FileUploadData?> ReadFileAsync(IBrowserFile file, long maxFileSize)
    {
        if (file == null)
        {
            logger.LogWarning("ReadFileAsync called with null file");
            return null;
        }

        try
        {
            logger.LogInformation("Reading file into memory: {FileName}, Size: {Size} bytes, ContentType: {ContentType}",
                file.Name, file.Size, file.ContentType);

            if (file.Size > maxFileSize)
            {
                logger.LogWarning("File {FileName} exceeds max size ({Size} > {MaxSize})",
                    file.Name, file.Size, maxFileSize);
                return null;
            }

            await using var stream = file.OpenReadStream(maxAllowedSize: maxFileSize);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            logger.LogDebug("Successfully read {ByteCount} bytes from {FileName}", bytes.Length, file.Name);

            return new FileUploadData
            {
                Content = bytes,
                FileName = file.Name,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading file {FileName} into memory", file.Name);
            return null;
        }
    }
}
