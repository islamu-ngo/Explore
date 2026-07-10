// ABOUTME: Browser-safe Web Push configuration provider for downstream API/BFF endpoints.
// ABOUTME: Returns only enabled state and VAPID public key, never private key or subject internals.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.WebPush;

public sealed class WebPushConfigurationProvider(IOptions<WebPushSettings> options) : IWebPushConfigurationProvider
{
    public WebPushPublicConfiguration GetPublicConfiguration()
    {
        var settings = options.Value;
        return new WebPushPublicConfiguration(settings.Enabled, settings.VapidPublicKey.Trim());
    }
}
