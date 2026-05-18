// ABOUTME: Storage object record client for image upload metadata persistence.
// ABOUTME: Keeps generated API record creation and ProblemDetails mapping out of ImageStorageService orchestration.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Microsoft.AspNetCore.Components.Forms;

namespace Explore.Blazor.Client.Services;

public interface IImageStorageRecordClient
{
    Task<ImageUploadResult?> CreateRecordFromBytesAsync(ImageUploadResponse uploadResponse, FileUploadData fileData);

    Task<ImageUploadResult?> CreateRecordFromFileAsync(ImageUploadResponse uploadResponse, IBrowserFile file);
}

public sealed class ImageStorageRecordClient(
    IEventApiClient apiClient,
    IImageContentClassifier contentClassifier,
    ILogger<ImageStorageRecordClient> logger) : IImageStorageRecordClient
{
    public async Task<ImageUploadResult?> CreateRecordFromBytesAsync(ImageUploadResponse uploadResponse, FileUploadData fileData)
    {
        var createDto = BuildCreateStorageObjectDto(
            fileData.ContentType,
            uploadResponse.ViewUrl,
            uploadResponse.ObjectKey,
            fileData.FileName,
            fileData.Size);
        if (createDto == null)
        {
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = "Failed to build storage metadata for uploaded image."
            };
        }

        logger.LogInformation("Creating StorageObject record for {FileName}", fileData.FileName);

        BaseCommandResponseOfGuid? createResponse;
        try
        {
            createResponse = await apiClient.CreateStorageObjectAsync(createDto);
            logger.LogInformation("StorageObject API response received: Success={Success}, Id={Id}, Message={Message}",
                createResponse?.Success, createResponse?.Id, createResponse?.Message);
        }
        catch (ApiException<ProblemDetails> apiEx)
        {
            var problemMessage = BuildProblemDetailsMessage(apiEx.Result);
            logger.LogError(apiEx,
                "StorageobjectPOSTAsync returned {StatusCode} for {FileName}. Details: {ProblemMessage}",
                apiEx.StatusCode,
                fileData.FileName,
                problemMessage);
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = $"API call failed ({apiEx.StatusCode}): {problemMessage}"
            };
        }
        catch (Exception apiEx)
        {
            logger.LogError(apiEx, "Exception calling StorageobjectPOSTAsync for {FileName}", fileData.FileName);
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = $"API call failed: {apiEx.Message}"
            };
        }

        return MapCreateResponse(createResponse, uploadResponse, fileData.FileName, logDetailedSuccess: true);
    }

    public async Task<ImageUploadResult?> CreateRecordFromFileAsync(ImageUploadResponse uploadResponse, IBrowserFile file)
    {
        var createDto = BuildCreateStorageObjectDto(
            file.ContentType,
            uploadResponse.ViewUrl,
            uploadResponse.ObjectKey,
            file.Name,
            file.Size);
        if (createDto == null)
        {
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = "Failed to build storage metadata for uploaded image."
            };
        }

        logger.LogInformation("Creating StorageObject record");
        BaseCommandResponseOfGuid? createResponse;
        try
        {
            createResponse = await apiClient.CreateStorageObjectAsync(createDto);
        }
        catch (ApiException<ProblemDetails> apiEx)
        {
            var problemMessage = BuildProblemDetailsMessage(apiEx.Result);
            logger.LogError(apiEx,
                "StorageobjectPOSTAsync returned {StatusCode} for {FileName}. Details: {ProblemMessage}",
                apiEx.StatusCode,
                file.Name,
                problemMessage);
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = $"API call failed ({apiEx.StatusCode}): {problemMessage}"
            };
        }

        return MapCreateResponse(createResponse, uploadResponse, file.Name, logDetailedSuccess: false);
    }

    private ImageUploadResult? MapCreateResponse(
        BaseCommandResponseOfGuid? createResponse,
        ImageUploadResponse uploadResponse,
        string fileName,
        bool logDetailedSuccess)
    {
        if (createResponse?.Success == true)
        {
            if (logDetailedSuccess)
            {
                logger.LogInformation("StorageObject created successfully! ID: {StorageObjectId}", createResponse.Id);
            }
            else
            {
                logger.LogInformation("StorageObject created with ID: {StorageObjectId}", createResponse.Id);
            }

            var result = new ImageUploadResult
            {
                Success = true,
                StorageObjectId = createResponse.Id ?? Guid.Empty,
                ViewUrl = uploadResponse.ViewUrl,
                ObjectKey = uploadResponse.ObjectKey
            };

            if (logDetailedSuccess)
            {
                logger.LogInformation("Returning successful ImageUploadResult for {FileName}", fileName);
            }

            return result;
        }

        var errorMsg = createResponse?.Message ?? "Failed to create storage object record";
        logger.LogWarning("StorageObject creation failed: {Message}", errorMsg);
        return new ImageUploadResult
        {
            Success = false,
            ErrorMessage = errorMsg
        };
    }

    private CreateStorageObjectDto? BuildCreateStorageObjectDto(
        string contentType,
        string? viewUrl,
        string? objectKey,
        string fileName,
        long size)
    {
        var uri = !string.IsNullOrWhiteSpace(viewUrl) ? viewUrl : objectKey;
        if (string.IsNullOrWhiteSpace(uri))
        {
            logger.LogError("Cannot build CreateStorageObjectDto: both ViewUrl and ObjectKey are empty for {FileName}", fileName);
            return null;
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = contentClassifier.GetDefaultExtension(contentType);
        }

        return new CreateStorageObjectDto
        {
            FileTypeId = contentClassifier.GetFileTypeId(contentType),
            Uri = uri,
            FullName = fileName,
            Extension = extension,
            Size = size,
            TenantId = Guid.Empty
        };
    }

    private static string BuildProblemDetailsMessage(ProblemDetails? problemDetails)
    {
        if (problemDetails == null)
        {
            return "Bad request";
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(problemDetails.Title))
        {
            parts.Add(problemDetails.Title!);
        }

        if (!string.IsNullOrWhiteSpace(problemDetails.Detail))
        {
            parts.Add(problemDetails.Detail!);
        }

        if (problemDetails.AdditionalProperties.TryGetValue("errors", out var errorsObject))
        {
            var validationErrors = FlattenValidationErrors(errorsObject);
            if (!string.IsNullOrWhiteSpace(validationErrors))
            {
                parts.Add(validationErrors);
            }
        }

        return parts.Count == 0 ? "Bad request" : string.Join(" | ", parts);
    }

    private static string FlattenValidationErrors(object? errorsObject)
    {
        if (errorsObject is not JsonElement errorsJson || errorsJson.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var messages = new List<string>();
        foreach (var entry in errorsJson.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var fieldMessages = new List<string>();
            foreach (var item in entry.Value.EnumerateArray())
            {
                var message = item.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    fieldMessages.Add(message);
                }
            }

            if (fieldMessages.Count > 0)
            {
                messages.Add($"{entry.Name}: {string.Join(", ", fieldMessages)}");
            }
        }

        return string.Join("; ", messages);
    }
}
