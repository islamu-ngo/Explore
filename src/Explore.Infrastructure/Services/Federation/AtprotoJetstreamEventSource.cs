// ABOUTME: Owns one CarpaNet Jetstream client per active session behind a bounded session contract.
// ABOUTME: Starts paused, sends the exact community filters after connection, and serializes later updates.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using CarpaNet.Jetstream;

namespace Explore.Infrastructure.Services.Federation;

public sealed record AtprotoJetstreamSubscription(
    Uri Endpoint,
    IReadOnlyList<string> WantedCollections,
    IReadOnlyList<string> WantedDids,
    long? Cursor,
    int MaxMessageSizeBytes);

public interface IAtprotoJetstreamSession : IAsyncDisposable
{
    IAsyncEnumerable<JetstreamEvent> ReadEventsAsync(CancellationToken cancellationToken);

    Task SendOptionsUpdateAsync(
        JetstreamOptionsUpdate update,
        CancellationToken cancellationToken);
}

public interface IAtprotoJetstreamEventSource
{
    Task<IAtprotoJetstreamSession> OpenSessionAsync(
        AtprotoJetstreamSubscription subscription,
        TimeSpan readinessTimeout,
        CancellationToken cancellationToken);
}

internal sealed class CarpaNetJetstreamEventSource : IAtprotoJetstreamEventSource
{
    public async Task<IAtprotoJetstreamSession> OpenSessionAsync(
        AtprotoJetstreamSubscription subscription,
        TimeSpan readinessTimeout,
        CancellationToken cancellationToken)
    {
        var client = new JetstreamClient(subscription.Endpoint)
        {
            BufferSize = subscription.MaxMessageSizeBytes
        };
        var options = new JetstreamSubscribeOptions
        {
            Cursor = subscription.Cursor,
            MaxMessageSizeBytes = subscription.MaxMessageSizeBytes,
            Compress = false,
            RequireHello = true
        };
        var session = new CarpaNetJetstreamSession(client, options, cancellationToken);
        try
        {
            await session.SendInitialOptionsAsync(
                CreateOptionsUpdate(subscription),
                readinessTimeout,
                cancellationToken);
            return session;
        }
        catch
        {
            await session.DisposeAsync();
            throw;
        }
    }

    internal static JetstreamOptionsUpdate CreateOptionsUpdate(AtprotoJetstreamSubscription subscription) =>
        new()
        {
            Payload = new JetstreamOptionsPayload
            {
                WantedCollections =
                [
                    AtprotoJetstreamConstants.EventCollection,
                    AtprotoJetstreamConstants.RsvpCollection
                ],
                WantedDids = [.. subscription.WantedDids],
                MaxMessageSizeBytes = subscription.MaxMessageSizeBytes
            }
        };

    private sealed class CarpaNetJetstreamSession : IAtprotoJetstreamSession
    {
        private static readonly TimeSpan ReadinessRetryDelay = TimeSpan.FromMilliseconds(25);
        private readonly JetstreamClient _client;
        private readonly CancellationTokenSource _lifetimeCancellation;
        private readonly IAsyncEnumerator<JetstreamEvent> _enumerator;
        private readonly SemaphoreSlim _sendGate = new(1, 1);
        private Task<bool> _nextEvent;
        private int _readStarted;
        private int _disposed;

        public CarpaNetJetstreamSession(
            JetstreamClient client,
            JetstreamSubscribeOptions options,
            CancellationToken cancellationToken)
        {
            _client = client;
            _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _enumerator = client
                .SubscribeAsync(options, _lifetimeCancellation.Token)
                .GetAsyncEnumerator(_lifetimeCancellation.Token);
            _nextEvent = _enumerator.MoveNextAsync().AsTask();
        }

        public async IAsyncEnumerable<JetstreamEvent> ReadEventsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _readStarted, 1) != 0)
            {
                throw new InvalidOperationException("The Jetstream session event reader has already been started.");
            }

            while (await _nextEvent)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return _enumerator.Current;
                _nextEvent = _enumerator.MoveNextAsync().AsTask();
            }
        }

        public async Task SendOptionsUpdateAsync(
            JetstreamOptionsUpdate update,
            CancellationToken cancellationToken)
        {
            await _sendGate.WaitAsync(cancellationToken);
            try
            {
                await _client.SendOptionsUpdateAsync(update, cancellationToken);
            }
            finally
            {
                _sendGate.Release();
            }
        }

        public async Task SendInitialOptionsAsync(
            JetstreamOptionsUpdate update,
            TimeSpan readinessTimeout,
            CancellationToken cancellationToken)
        {
            using var readinessCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readinessCancellation.CancelAfter(readinessTimeout);
            try
            {
                while (true)
                {
                    if (_nextEvent.IsCompleted && !await _nextEvent)
                    {
                        throw new InvalidOperationException("Jetstream closed before accepting its initial options.");
                    }

                    try
                    {
                        await SendOptionsUpdateAsync(update, readinessCancellation.Token);
                        return;
                    }
                    catch (InvalidOperationException exception) when (
                        string.Equals(exception.Message, "WebSocket is not connected", StringComparison.Ordinal))
                    {
                        await Task.Delay(ReadinessRetryDelay, readinessCancellation.Token);
                    }
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Jetstream did not become ready within the configured connection bound.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _lifetimeCancellation.Cancel();
            Exception? receiveFailure = null;
            try
            {
                await _nextEvent;
            }
            catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                receiveFailure = exception;
            }

            try
            {
                await _enumerator.DisposeAsync();
            }
            finally
            {
                _client.Dispose();
                _sendGate.Dispose();
                _lifetimeCancellation.Dispose();
            }

            if (receiveFailure is not null)
            {
                ExceptionDispatchInfo.Capture(receiveFailure).Throw();
            }
        }
    }
}
