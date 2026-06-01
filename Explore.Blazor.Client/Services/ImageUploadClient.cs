// ABOUTME: Image upload transport client for BFF upload sessions, upload proxy, and presigned S3 uploads.
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
            logger.LogInformation("Getting upload URL for: {FileName}, type: {ContentType}", fileName, contentType);

            var request = new UploadRequestDto
            {
                FileName = fileName,
                ContentType = contentType
            };

            var isBrowser = OperatingSystem.IsBrowser();
            var bffUploadSession = isBrowser && expectedSizeBytes.HasValue
                ? await GetUploadSessionViaBffAsync(fileName, contentType, expectedSizeBytes.Value)
                : null;
            if (isBrowser && bffUploadSession != null)
            {
                return bffUploadSession;
            }

            if (isBrowser)
            {
                logger.LogWarning("BFF upload session request returned no usable response in browser runtime.");
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
                logger.LogWarning("UploadUrl is null or empty. Check server S3 configuration (bucket/endpoint/credentials)");
                return null;
            }

            logger.LogDebug("Got upload URL: {UploadUrlPreview}...", response.UploadUrl?.Substring(0, Math.Min(50, response.UploadUrl?.Length ?? 0)));
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
            logger.LogError(ex, "API error getting upload URL: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting upload URL");
            return null;
        }
    }

    public async Task<ImageUploadResult?> UploadViaBffProxyAsync(string uploadSessionId, FileUploadData fileData)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(uploadSessionId), "uploadSessionId");
            form.Add(new StringContent(fileData.ContentType), "contentType");

            using var fileContent = new ByteArrayContent(fileData.Content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileData.ContentType);
            form.Add(fileContent, "file", fileData.FileName);

            using var response = bffClient is not null
                ? await bffClient.PostMultipartAsync(UploadProxyPath, form)
                : await httpClientFactory.CreateClient("BffClient").PostAsync(UploadProxyPath, form);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("BFF upload proxy completed successfully for {FileName}", fileData.FileName);
                var uploadResult = await response.ReadJsonOrDefaultAsync<BffStorageUploadProxyResponse>();
                return MapBffUploadResult(uploadResult);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("BFF upload proxy failed for {FileName}. Status={StatusCode}, Body={Body}",
                fileData.FileName, (int)response.StatusCode, errorBody);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BFF upload proxy request failed for {FileName}", fileData.FileName);
            return null;
        }
    }

    public async Task<ImageUploadResult?> UploadViaBffProxyAsync(string uploadSessionId, IBrowserFile file)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(uploadSessionId), "uploadSessionId");
            form.Add(new StringContent(file.ContentType), "contentType");

            await using var stream = file.OpenReadStream(maxAllowedSize: file.Size);
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.Name);

            using var response = bffClient is not null
                ? await bffClient.PostMultipartAsync(UploadProxyPath, form)
                : await httpClientFactory.CreateClient("BffClient").PostAsync(UploadProxyPath, form);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("BFF upload proxy completed successfully for {FileName}", file.Name);
                var uploadResult = await response.ReadJsonOrDefaultAsync<BffStorageUploadProxyResponse>();
                return MapBffUploadResult(uploadResult);
            }

            var hasBody = response.Content.Headers.ContentLength.GetValueOrDefault() > 0;
            logger.LogError("BFF upload proxy failed for {FileName}. Status={StatusCode}, HasBody={HasBody}",
                file.Name, (int)response.StatusCode, hasBody);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BFF upload proxy request failed for {FileName}", file.Name);
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
            logger.LogWarning("Invalid upload URL format: {UploadUrl} - aborting upload", uploadUrl);
            return false;
        }

        try
        {
            logger.LogInformation("Uploading to S3: {FileName}, Size: {Size} bytes, ContentType: {ContentType}, Host: {Host}",
                file.Name, file.Size, file.ContentType, validatedUri.Host);

            using var s3Client = httpClientFactory.CreateClient("S3Upload");
            await using var stream = file.OpenReadStream(maxAllowedSize: DefaultMaxFileSize);
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            logger.LogDebug("Sending PUT request to S3...");
            var response = await s3Client.PutAsync(validatedUri, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("S3 upload failed: {StatusCode} - {ReasonPhrase} - {ErrorContent}",
                    (int)response.StatusCode, response.ReasonPhrase, errorContent);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    logger.LogError("S3 403 Forbidden - Check: 1) CORS config on bucket 2) Pre-signed URL expired 3) Content-Type mismatch");
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    logger.LogError("S3 400 Bad Request - Check: 1) Content-Type header matches pre-signed URL 2) Request headers are correct");
                }
            }
            else
            {
                var etag = response.Headers.ETag?.Tag ?? "no-etag";
                logger.LogInformation("S3 upload successful! ETag: {ETag}", etag);
            }

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request error uploading to S3. This may be a CORS issue - check bucket CORS configuration.");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image to S3");
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
            logger.LogWarning("Invalid upload URL format: {UploadUrl} - aborting upload", uploadUrl);
            return false;
        }

        if (OperatingSystem.IsBrowser())
        {
            logger.LogWarning("Browser runtime requires a server-issued upload session for {FileName}", fileData.FileName);
            return false;
        }

        try
        {
            logger.LogInformation("Uploading to S3: {FileName}, Size: {Size} bytes, ContentType: {ContentType}, Host: {Host}",
                fileData.FileName, fileData.Size, fileData.ContentType, validatedUri.Host);

            using var s3Client = httpClientFactory.CreateClient("S3Upload");
            using var content = new ByteArrayContent(fileData.Content);
            content.Headers.ContentType = new MediaTypeHeaderValue(fileData.ContentType);

            logger.LogDebug("Sending PUT request to S3 with ByteArrayContent ({Size} bytes)...", fileData.Size);
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var response = await s3Client.PutAsync(validatedUri, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("S3 upload failed: {StatusCode} - {ReasonPhrase} - {ErrorContent}",
                    (int)response.StatusCode, response.ReasonPhrase, errorContent);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    logger.LogError("S3 403 Forbidden - Check: 1) CORS config on bucket 2) Pre-signed URL expired 3) Content-Type mismatch");
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    logger.LogError("S3 400 Bad Request - Check: 1) Content-Type header matches pre-signed URL 2) Request headers are correct");
                }

                return false;
            }

            var etag = response.Headers.ETag?.Tag ?? "no-etag";
            logger.LogInformation("S3 upload successful! ETag: {ETag}, Size: {Size} bytes", etag, fileData.Size);
            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogError("S3 upload timed out after 3 minutes for file {FileName}", fileData.FileName);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request error uploading to S3. This may be a CORS issue - check bucket CORS configuration. File: {FileName}", fileData.FileName);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error uploading image to S3. File: {FileName}", fileData.FileName);
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

                logger.LogWarning("Upload session request failed via BFF. Status={StatusCode}, Error={Error}",
                    result.StatusCode is null ? null : (int)result.StatusCode,
                    result.ErrorMessage);
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
            logger.LogWarning(ex, "Error calling upload session endpoint via BFF");
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
                ErrorMessage = "Storage upload completed without metadata."
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

                logger.LogWarning("Upload URL request failed via BFF. Status={StatusCode}, Error={Error}",
                    result.StatusCode is null ? null : (int)result.StatusCode,
                    result.ErrorMessage);
                return null;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error calling upload URL endpoint via BFF");
            return null;
        }
    }
}
