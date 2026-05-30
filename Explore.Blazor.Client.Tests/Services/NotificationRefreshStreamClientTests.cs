// ABOUTME: Unit tests for browser notification SSE client event dispatch.
// ABOUTME: Verifies parsed refresh hints are surfaced to Blazor consumers without JS runtime calls.

using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class NotificationRefreshStreamClientTests
{
    [Test]
    public async Task HandleNotificationRefresh_RaisesRefreshReceivedWithParsedGeneratedAt()
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var received = new List<Contracts.Services.Notifications.NotificationRefreshHintReceivedEventArgs>();
        var client = CreateClient();
        client.RefreshReceived += args =>
        {
            received.Add(args);
            return Task.CompletedTask;
        };

        await client.HandleNotificationRefresh(5, true, "unread-count-changed", generatedAt.ToString("O"));

        await Assert.That(received).Count().IsEqualTo(1);
        await Assert.That(received[0].UnreadCount).IsEqualTo(5);
        await Assert.That(received[0].HasUnread).IsTrue();
        await Assert.That(received[0].Reason).IsEqualTo("unread-count-changed");
        await Assert.That(received[0].GeneratedAt).IsEqualTo(generatedAt);
    }

    [Test]
    public async Task HandleNotificationRefresh_WithMissingReason_UsesSafeDefaultReason()
    {
        var received = new List<Contracts.Services.Notifications.NotificationRefreshHintReceivedEventArgs>();
        var client = CreateClient();
        client.RefreshReceived += args =>
        {
            received.Add(args);
            return Task.CompletedTask;
        };

        await client.HandleNotificationRefresh(0, false, null, null);

        await Assert.That(received).Count().IsEqualTo(1);
        await Assert.That(received[0].Reason).IsEqualTo("refresh");
        await Assert.That(received[0].GeneratedAt).IsGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    private static NotificationRefreshStreamClient CreateClient()
    {
        return new NotificationRefreshStreamClient(
            Substitute.For<IJSRuntime>(),
            Substitute.For<NavigationManager>(),
            Substitute.For<ILogger<NotificationRefreshStreamClient>>());
    }
}
