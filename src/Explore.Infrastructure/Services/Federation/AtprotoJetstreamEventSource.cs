// ABOUTME: Contains CarpaNet's sealed ClientWebSocket-backed Jetstream client behind a bounded stream contract.
// ABOUTME: Applies only the two community calendar collections and never exposes the socket outside Infrastructure.

using System.Runtime.CompilerServices;
using CarpaNet.Jetstream;

namespace Explore.Infrastructure.Services.Federation;

public sealed record AtprotoJetstreamSubscription(
    Uri Endpoint,
    IReadOnlyList<string> WantedCollections,
    IReadOnlyList<string> WantedDids,
    long? Cursor,
    int MaxMessageSizeBytes);

public interface IAtprotoJetstreamEventSource
{
    IAsyncEnumerable<JetstreamEvent> SubscribeAsync(
        AtprotoJetstreamSubscription subscription,
        CancellationToken cancellationToken);
}

internal sealed class CarpaNetJetstreamEventSource : IAtprotoJetstreamEventSource
{
    public async IAsyncEnumerable<JetstreamEvent> SubscribeAsync(
        AtprotoJetstreamSubscription subscription,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (subscription.WantedDids.Count == 0)
        {
            throw new InvalidOperationException("A curated DID allowlist is required before opening a Jetstream subscription.");
        }

        using var client = new JetstreamClient(subscription.Endpoint)
        {
            BufferSize = subscription.MaxMessageSizeBytes
        };
        var options = new JetstreamSubscribeOptions
        {
            WantedCollections = subscription.WantedCollections,
            WantedDids = subscription.WantedDids,
            Cursor = subscription.Cursor,
            MaxMessageSizeBytes = subscription.MaxMessageSizeBytes,
            Compress = false,
            RequireHello = false
        };

        await foreach (JetstreamEvent value in client.SubscribeAsync(options, cancellationToken))
        {
            yield return value;
        }
    }
}
