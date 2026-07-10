// ABOUTME: Application contract for sending a prepared generic Web Push notification.
// ABOUTME: Keeps provider-specific WebPushClient behavior inside Infrastructure.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IWebPushNotificationSender
{
    Task<WebPushSendResult> SendAsync(WebPushSendEnvelope envelope, CancellationToken cancellationToken = default);
}
