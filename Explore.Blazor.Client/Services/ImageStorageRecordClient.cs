// ABOUTME: Storage object record client for image upload metadata persistence.
// ABOUTME: Keeps generated API record creation and ProblemDetails mapping out of ImageStorageService orchestration.

using Explore.Blazor.Client.Clients;
using Microsoft.AspNetCore.Components.Forms;

namespace Explore.Blazor.Client.Services;

public interface IImageStorageRecordClient
{
    Task<ImageUploadResult?> CreateRecordFromBytesAsync(ImageUploadTarget uploadTarget, FileUploadData fileData);

    Task<ImageUploadResult?> CreateRecordFromFileAsync(ImageUploadTarget uploadTarget, IBrowserFile file);
}

public sealed class ImageStorageRecordClient(
    IEventApiClient apiClient,
    IImageContentClassifier contentClassifier,
    ILogger<ImageStorageRecordClient> logger) : IImageStorageRecordClient
{
    public async Task<ImageUploadResult?> CreateRecordFromBytesAsync(ImageUploadTarget uploadResponse, FileUploadData fileData)
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
                ErrorMessage = ImageUploadClientPolicy.MetadataBuildFailureMessage
            };
        }

        logger.LogInformation(
            "Creating StorageObject record for selected image. SizeBucket={SizeBucket}, ContentTypeBucket={ContentTypeBucket}",
            ImageUploadClientPolicy.GetSizeBucket(fileData.Size),
            ImageUploadClientPolicy.GetContentTypeBucket(fileData.ContentType));

        BaseCommandResponseOfGuid? createResponse;
        try
        {
            createResponse = await apiClient.CreateStorageObjectAsync(createDto);
            logger.LogInformation(
                "StorageObject API response received. Success={Success}, HasId={HasId}, HasMessage={HasMessage}",
                createResponse?.Success,
                createResponse?.Id is not null,
                !string.IsNullOrWhiteSpace(createResponse?.Message));
        }
        catch (ApiException<ProblemDetails> apiEx)
        {
            logger.LogWarning(
                "StorageobjectPOSTAsync returned {StatusCode} for selected image. HasProblemDetails={HasProblemDetails}, FailureType={FailureType}",
                apiEx.StatusCode,
                apiEx.Result is not null,
                ImageUploadClientPolicy.GetFailureType(apiEx));
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = ImageUploadClientPolicy.MetadataFailureMessage
            };
        }
        catch (Exception apiEx)
        {
            logger.LogWarning(
                "Exception calling StorageobjectPOSTAsync for selected image. FailureType={FailureType}",
                ImageUploadClientPolicy.GetFailureType(apiEx));
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = ImageUploadClientPolicy.MetadataFailureMessage
            };
        }

        return MapCreateResponse(createResponse, uploadResponse, logDetailedSuccess: true);
    }

    public async Task<ImageUploadResult?> CreateRecordFromFileAsync(ImageUploadTarget uploadResponse, IBrowserFile file)
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
                ErrorMessage = ImageUploadClientPolicy.MetadataBuildFailureMessage
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
            logger.LogWarning(
                "StorageobjectPOSTAsync returned {StatusCode} for selected image. HasProblemDetails={HasProblemDetails}, FailureType={FailureType}",
                apiEx.StatusCode,
                apiEx.Result is not null,
                ImageUploadClientPolicy.GetFailureType(apiEx));
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = ImageUploadClientPolicy.MetadataFailureMessage
            };
        }

        return MapCreateResponse(createResponse, uploadResponse, logDetailedSuccess: false);
    }

    private ImageUploadResult? MapCreateResponse(
        BaseCommandResponseOfGuid? createResponse,
        ImageUploadTarget uploadResponse,
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

            return result;
        }

        logger.LogWarning(
            "StorageObject creation failed. HasMessage={HasMessage}",
            !string.IsNullOrWhiteSpace(createResponse?.Message));
        return new ImageUploadResult
        {
            Success = false,
            ErrorMessage = ImageUploadClientPolicy.MetadataFailureMessage
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
            logger.LogWarning("Cannot build CreateStorageObjectDto: both ViewUrl and ObjectKey are empty.");
            return null;
        }

        var safeFileName = ImageUploadClientPolicy.BuildSafeFileName(fileName, contentType);
        var extension = Path.GetExtension(safeFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = contentClassifier.GetDefaultExtension(contentType);
        }

        return new CreateStorageObjectDto
        {
            FileTypeId = contentClassifier.GetFileTypeId(contentType),
            Uri = uri,
            FullName = safeFileName,
            Extension = extension,
            Size = size,
            TenantId = Guid.Empty
        };
    }
}
