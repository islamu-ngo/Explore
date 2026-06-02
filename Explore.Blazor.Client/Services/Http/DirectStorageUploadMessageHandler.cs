// ABOUTME: Configures browser direct-upload requests that use server-issued provider URLs.
// ABOUTME: Keeps cross-origin storage upload HTTP details isolated from upload orchestration services.

using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services.Http;

/// <summary>
/// Message handler for cross-origin direct storage uploads from Blazor WASM.
/// Configures CORS mode and omits browser credentials because server-issued provider URLs carry their own authorization.
/// </summary>
public class DirectStorageUploadMessageHandler : DelegatingHandler
{
    private readonly ILogger<DirectStorageUploadMessageHandler>? _logger;

    public DirectStorageUploadMessageHandler()
    {
    }

    public DirectStorageUploadMessageHandler(ILogger<DirectStorageUploadMessageHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Set CORS mode for cross-origin provider requests.
        request.SetBrowserRequestMode(BrowserRequestMode.Cors);

        // Do not include app cookies for direct provider upload URLs.
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);

        _logger?.LogDebug("[Storage Direct Upload] {Method} {Host} - Mode: CORS, Credentials: Omit",
            request.Method, request.RequestUri?.Host);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("[Storage Direct Upload] {Method} {Uri} - Failed with status {StatusCode}",
                    request.Method, request.RequestUri, (int)response.StatusCode);
            }
            else
            {
                _logger?.LogInformation("[Storage Direct Upload] {Method} {Host} - Success ({StatusCode})",
                    request.Method, request.RequestUri?.Host, (int)response.StatusCode);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Storage Direct Upload] {Method} {Uri} - Request failed with exception",
                request.Method, request.RequestUri);
            throw;
        }
    }
}
