// ABOUTME: Client-side wrapper around POST /bff/language with CultureRegistry validation.
// ABOUTME: Validates input against the allowlist before any HTTP call; logs and swallows transport errors.

using Explore.Blazor.Client.Contracts.Services;
using Explore.Domain.Common.Localization;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Default <see cref="ILanguagePreferenceService"/> implementation.
/// </summary>
public sealed class LanguagePreferenceService : ILanguagePreferenceService
{
    private const string BffClientName = "BffClient";
    private const string LanguageEndpointPath = "/bff/language";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LanguagePreferenceService> _logger;

    public LanguagePreferenceService(IHttpClientFactory httpClientFactory, ILogger<LanguagePreferenceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SetLanguageAsync(string languageCode, CancellationToken ct = default)
    {
        if (!CultureRegistry.TryGetEntry(languageCode, out var entry))
        {
            _logger.LogWarning(
                "[LOCALIZATION] Rejected language preference '{Code}'; not in CultureRegistry",
                languageCode);
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(BffClientName);
            var response = await client.PostAsync(
                $"{LanguageEndpointPath}?lang={Uri.EscapeDataString(entry.Code)}",
                content: null,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LOCALIZATION] /bff/language returned {Status} for code {Code}",
                    (int)response.StatusCode, entry.Code);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION] Failed to persist language preference for code {Code}", entry.Code);
            return false;
        }
    }
}
