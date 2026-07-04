// ABOUTME: Image upload transport client for BFF upload sessions, upload proxy, and trusted direct provider uploads.
// ABOUTME: Keeps upload-specific raw multipart/streaming HTTP isolated from ImageStorageService orchestration.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Extensions;
using Explore.Blazor.Client.Services.Http;
using Microsoft.AspNetCore.Components.Forms;

namespace Explore.Blazor.Client.Services;

public interface IImageUploadClient
{
    Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType, long? expectedSizeBytes = null);

    Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file);

    Task<bool> UploadImageFromBytesAsync(string uploadUrl, FileUploadData fileData);

    Task<ImageUploadResult?> UploadViaBffProxyAsync(string uploadSessionId, FileUploadData fileData);

    Task<ImageUploadResult?> UploadViaBffProxyAsync(string uploadSessionId, IBrowserFile file);
}

public sealed class ImageUploadClient(
    IEventApiClient apiClient,
    IHttpClientFactory httpClientFactory,
    ILogger<ImageUploadClient> logger,
    BffClient? bffClient = null,
    IApiClientExecutor? apiClientExecutor = null) : IImageUploadClient
{
    private const string GenerateUploadUrlPath = "/api/storageobject/generate-upload-url";
    private const string GenerateUploadSessionPath = "/bff/storage/upload-session";
    private const string UploadProxyPath = "/bff/storage/upload-proxy";
    private const long DefaultMaxFileSize = 10 * 1024 * 1024;

    private readonly IApiClientExecutor _apiClientExecutor = apiClientExecutor ?? new ApiClientExecutor();

    public async Task<ImageUploadResponse?> GetUploadUrlAsync(string fileName, string contentType, long? expectedSizeBytes = null)
    {
        try
        {
            var safeFileName = ImageUploadClientPolicy.BuildSafeFileName(fileName, contentType);
            logger.LogInformation(
                "Getting upload URL for selected image. ContentTypeBucket={ContentTypeBucket}, SizeBucket={SizeBucket}",
                ImageUploadClientPolicy.GetContentTypeBucket(contentType),
                expectedSizeBytes.HasValue ? ImageUploadClientPolicy.GetSizeBucket(expectedSizeBytes.Value) : "unknown");

            var request = new UploadRequestDto
            {
                FileName = safeFileName,
                ContentType = contentType
            };

            var isBrowser = OperatingSystem.IsBrowser();
            var bffUploadSession = expectedSizeBytes.HasValue
                ? await GetUploadSessionViaBffAsync(safeFileName, contentType, expectedSizeBytes.Value)
                : null;
            if (bffUploadSession != null)
            {
                return bffUploadSession;
            }

            if (isBrowser)
            {
                logger.LogWarning("BFF upload session request returned no usable response in browser runtime.");
                return null;
            }

            if (expectedSizeBytes.HasValue)
            {
                logger.LogWarning("BFF upload session request returned no usable response for sized upload. Direct provider fallback is disabled for browser-originated image uploads.");
                return null;
            }

            var response = await GetUploadUrlViaBffAsync(request);
            if (response == null)
            {
                logger.LogWarning("BFF upload URL request returned no usable response. Falling back to generated API client.");
                response = await apiClient.GenerateStorageObjectUploadUrlAsync(request);
            }

            if (response == null)
            {
                logger.LogWarning("GenerateUploadUrlAsync returned null response");
                return null;
            }

            if (string.IsNullOrWhiteSpace(response.UploadUrl))
            {
                logger.LogWarning("UploadUrl is null or empty. Check server storage provider configuration.");
                return null;
            }

            logger.LogDebug("Got trusted direct upload URL response for content type {ContentType}", contentType);
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
            logger.LogWarning(
                "API error getting upload URL. StatusCode={StatusCode}, FailureType={FailureType}",
                ex.StatusCode,
                ImageUploadClientPolicy.GetFailureType(ex));
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Error getting upload URL. FailureType={FailureType}",
                ImageUploadClientPolicy.GetFailureType(ex));
            return null;
        }
    }

    public async Task<ImageUploadResult?> UploadViaBffProxyAsync(string uploadSessionId, FileUploadData fileData)
    {
        try
        {
            var safeFileName = ImageUploadClientPolicy.BuildSafeFileName(fileData.FileName, fileData.ContentType);
            using var form = new MultipartFormDataContent();
            using var uploadSessionContent = new StringContent(uploadSessionId);
            using var contentTypeContent = new StringContent(fileData.ContentType);
            form.Add(uploadSessionContent, "uploadSessionId");
            form.Add(contentTypeContent, "contentType");

            using var fileContent = new ByteArrayContent(fileData.Content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileData.ContentType);
            form.Add(fileContent, "file", safeFileName);

            using var response = bffClient is not null
                ? await bffClient.PostMultipartAsync(UploadProxyPath, form)
                : await httpClientFactory.CreateClient("BffClient").PostAsync(UploadProxyPath, form);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "BFF upload proxy completed successfully. SizeBucket={SizeBucket}, ContentTypeBucket={ContentTypeBucket}",
                    ImageUploadClientPolicy.GetSizeBucket(fileData.Size),
                    ImageUploadClientPolicy.GetContentTypeBucket(fileData.ContentType));
                var uploadResult = await response.ReadJsonOrDefaultAsync<BffStorageUploadProxyResponse>();
                return MapBffUploadResult(uploadResult);
            }

            var hasBody = response.Content.Headers.ContentLength.GetValueOrDefault() > 0;
            logger.LogWarning(
                "BFF upload proxy failed. Status={StatusCode}, HasBody={HasBody}, SizeBucket={SizeBucket}",
                (int)response.StatusCode,
                hasBody,
                ImageUploadClientPolicy.GetSizeBucket(fileData.Size));
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "BFF upload proxy request failed. FailureType={FailureType}, SizeBucket={SizeBucket}",
                ImageUploadClientPolicy.GetFailureType(ex),
                ImageUploadClientPolicy.GetSizeBucket(fileData.Size));
            return null;
        }
    }

    public async Task<ImageUploadResult?> UploadViaBffProxyAsync(string uploadSessionId, IBrowserFile file)
    {
        try
        {
            var safeFileName = ImageUploadClientPolicy.BuildSafeFileName(file.Name, file.ContentType);
            using var form = new MultipartFormDataContent();
            using var uploadSessionContent = new StringContent(uploadSessionId);
            using var contentTypeContent = new StringContent(file.ContentType);
            form.Add(uploadSessionContent, "uploadSessionId");
            form.Add(contentTypeContent, "contentType");

            await using var stream = file.OpenReadStream(maxAllowedSize: file.Size);
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", safeFileName);

            using var response = bffClient is not null
                ? await bffClient.PostMultipartAsync(UploadProxyPath, form)
                : await httpClientFactory.CreateClient("BffClient").PostAsync(UploadProxyPath, form);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "BFF upload proxy completed successfully. SizeBucket={SizeBucket}, ContentTypeBucket={ContentTypeBucket}",
                    ImageUploadClientPolicy.GetSizeBucket(file.Size),
                    ImageUploadClientPolicy.GetContentTypeBucket(file.ContentType));
                var uploadResult = await response.ReadJsonOrDefaultAsync<BffStorageUploadProxyResponse>();
                return MapBffUploadResult(uploadResult);
            }

            var hasBody = response.Content.Headers.ContentLength.GetValueOrDefault() > 0;
            logger.LogWarning(
                "BFF upload proxy failed. Status={StatusCode}, HasBody={HasBody}, SizeBucket={SizeBucket}",
                (int)response.StatusCode,
                hasBody,
                ImageUploadClientPolicy.GetSizeBucket(file.Size));
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "BFF upload proxy request failed. FailureType={FailureType}, SizeBucket={SizeBucket}",
                ImageUploadClientPolicy.GetFailureType(ex),
                ImageUploadClientPolicy.GetSizeBucket(file.Size));
            return null;
        }
    }

    public async Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file)
    {
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            logger.LogWarning("Invalid upload URL (empty/null) - aborting upload");
            return false;
        }

        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var validatedUri))
        {
            logger.LogWarning("Invalid upload URL format - aborting upload.");
            return false;
        }

        try
        {
            logger.LogInformation(
                "Uploading directly to storage provider. SizeBucket={SizeBucket}, ContentTypeBucket={ContentTypeBucket}, Host={Host}",
                ImageUploadClientPolicy.GetSizeBucket(file.Size),
                ImageUploadClientPolicy.GetContentTypeBucket(file.ContentType),
                validatedUri.Host);

            using var directUploadClient = httpClientFactory.CreateClient(StorageHttpClientNames.DirectUpload);
            await using var stream = file.OpenReadStream(maxAllowedSize: DefaultMaxFileSize);
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            logger.LogDebug("Sending PUT request to direct storage provider URL.");
            var response = await directUploadClient.PutAsync(validatedUri, content);

            if (!response.IsSuccessStatusCode)
            {
                var hasBody = response.Content.Headers.ContentLength.GetValueOrDefault() > 0;
                logger.LogWarning(
                    "Direct storage upload failed. StatusCode={StatusCode}, HasBody={HasBody}",
                    (int)response.StatusCode,
                    hasBody);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    logger.LogWarning("Direct upload 403 Forbidden - check provider CORS, URL expiry, and Content-Type match.");
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    logger.LogWarning("Direct upload 400 Bad Request - check provider URL and request headers.");
                }
            }
            else
            {
                var etag = response.Headers.ETag?.Tag ?? "no-etag";
                logger.LogInformation("Direct storage upload successful. ETag: {ETag}", etag);
            }

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
                "HTTP request error during direct storage upload. FailureType={FailureType}",
                ImageUploadClientPolicy.GetFailureType(ex));
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Error uploading image to direct storage provider. FailureType={FailureType}",
                ImageUploadClientPolicy.GetFailureType(ex));
            return false;
        }
    }

    public async Task<bool> UploadImageFromBytesAsync(string uploadUrl, FileUploadData fileData)
    {
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            logger.LogWarning("Invalid upload URL (empty/null) - aborting upload");
            return false;
        }

        if (fileData == null || fileData.Content.Length == 0)
        {
            logger.LogWarning("Invalid file data (null or empty) - aborting upload");
            return false;
        }

        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var validatedUri))
        {
            logger.LogWarning("Invalid upload URL format - aborting upload.");
            return false;
        }

        if (OperatingSystem.IsBrowser())
        {
            logger.LogWarning("Browser runtime requires a server-issued upload session for selected image.");
            return false;
        }

        try
        {
            logger.LogInformation(
                "Uploading directly to storage provider. SizeBucket={SizeBucket}, ContentTypeBucket={ContentTypeBucket}, Host={Host}",
                ImageUploadClientPolicy.GetSizeBucket(fileData.Size),
                ImageUploadClientPolicy.GetContentTypeBucket(fileData.ContentType),
                validatedUri.Host);

            using var directUploadClient = httpClientFactory.CreateClient(StorageHttpClientNames.DirectUpload);
            using var content = new ByteArrayContent(fileData.Content);
            content.Headers.ContentType = new MediaTypeHeaderValue(fileData.ContentType);

            logger.LogDebug("Sending PUT request to direct storage provider URL with ByteArrayContent ({Size} bytes).", fileData.Size);
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var response = await directUploadClient.PutAsync(validatedUri, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var hasBody = response.Content.Headers.ContentLength.GetValueOrDefault() > 0;
                logger.LogWarning(
                    "Direct storage upload failed. StatusCode={StatusCode}, HasBody={HasBody}",
                    (int)response.StatusCode,
                    hasBody);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    logger.LogWarning("Direct upload 403 Forbidden - check provider CORS, URL expiry, and Content-Type match.");
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    logger.LogWarning("Direct upload 400 Bad Request - check provider URL and request headers.");
                }

                return false;
            }

            var etag = response.Headers.ETag?.Tag ?? "no-etag";
            logger.LogInformation("Direct storage upload successful. ETag: {ETag}, Size: {Size} bytes", etag, fileData.Size);
            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Direct storage upload timed out after 3 minutes.");
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
                "HTTP request error during direct storage upload. FailureType={FailureType}",
                ImageUploadClientPolicy.GetFailureType(ex));
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Unexpected error uploading image to direct storage provider. FailureType={FailureType}",
                ImageUploadClientPolicy.GetFailureType(ex));
            return false;
        }
    }

    private async Task<ImageUploadResponse?> GetUploadSessionViaBffAsync(
        string fileName,
        string contentType,
        long expectedSizeBytes)
    {
        var request = new BffStorageUploadSessionRequest
        {
            FileName = fileName,
            ContentType = contentType,
            ExpectedSizeBytes = expectedSizeBytes
        };

        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BffStorageUploadSessionResponse>(
                ct => bffClient is not null
                    ? bffClient.PostAsync(GenerateUploadSessionPath, request, ct)
                    : httpClientFactory.CreateClient("BffClient").PostAsync(GenerateUploadSessionPath, JsonContent.Create(request), ct),
                "BFF upload session");

            if (!result.IsSuccess)
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    logger.LogWarning("Upload session request returned 401 Unauthorized");
                    return null;
                }

                logger.LogWarning("Upload session request failed via BFF. Status={StatusCode}, HasError={HasError}",
                    result.StatusCode is null ? null : (int)result.StatusCode,
                    !string.IsNullOrWhiteSpace(result.ErrorMessage));
                return null;
            }

            var session = result.Value;
            if (session == null || string.IsNullOrWhiteSpace(session.UploadSessionId))
            {
                return null;
            }

            return new ImageUploadResponse
            {
                UploadSessionId = session.UploadSessionId,
                ObjectKey = string.Empty,
                ViewUrl = string.Empty,
                ExpiresInMinutes = session.ExpiresInMinutes <= 0 ? 60 : session.ExpiresInMinutes
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Error calling upload session endpoint via BFF. FailureType={FailureType}",
                ImageUploadClientPolicy.GetFailureType(ex));
            return null;
        }
    }

    private static ImageUploadResult? MapBffUploadResult(BffStorageUploadProxyResponse? uploadResult)
    {
        if (uploadResult is null || uploadResult.StorageObjectId == Guid.Empty)
        {
            return new ImageUploadResult
            {
                Success = false,
                ErrorMessage = ImageUploadClientPolicy.StorageUploadCompletedWithoutMetadataMessage
            };
        }

        return new ImageUploadResult
        {
            Success = true,
            StorageObjectId = uploadResult.StorageObjectId,
            ViewUrl = uploadResult.ViewUrl,
            ObjectKey = string.Empty
        };
    }

    private async Task<UploadUrlResponseDto?> GetUploadUrlViaBffAsync(UploadRequestDto request)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<UploadUrlResponseDto>(
                ct => httpClientFactory.CreateClient("BffClient").PostAsync(GenerateUploadUrlPath, JsonContent.Create(request), ct),
                "BFF upload URL");

            if (!result.IsSuccess)
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    logger.LogWarning("Upload URL request returned 401 Unauthorized");
                    return null;
                }

                logger.LogWarning("Upload URL request failed via BFF. Status={StatusCode}, HasError={HasError}",
                    result.StatusCode is null ? null : (int)result.StatusCode,
                    !string.IsNullOrWhiteSpace(result.ErrorMessage));
                return null;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Error calling upload URL endpoint via BFF. FailureType={FailureType}",
                ImageUploadClientPolicy.GetFailureType(ex));
            return null;
        }
    }
}
