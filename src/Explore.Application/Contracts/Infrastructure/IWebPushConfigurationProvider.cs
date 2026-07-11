// ABOUTME: Application contract for exposing browser-safe Web Push configuration only.
// ABOUTME: Keeps VAPID private-key material server-side while API/BFF lanes can read the public key.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IWebPushConfigurationProvider
{
    WebPushPublicConfiguration GetPublicConfiguration();
}
