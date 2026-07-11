// ABOUTME: Connects Web Push HTTP requests to the same public IP address validated by the SSRF policy.
// ABOUTME: Prevents DNS rebinding between endpoint validation and the outbound TLS connection.

using System.Net;
using System.Net.Sockets;

namespace Explore.Infrastructure.WebPush;

internal static class WebPushSafeConnector
{
    public static async ValueTask<Stream> ConnectAsync(
        WebPushEndpointSafetyPolicy safetyPolicy,
        DnsEndPoint endpoint,
        CancellationToken cancellationToken)
    {
        var safety = await safetyPolicy.ResolveHostAsync(endpoint.Host, cancellationToken);
        if (!safety.IsAllowed)
        {
            throw new HttpRequestException("Web Push endpoint did not resolve to a permitted public address.");
        }

        Exception? lastFailure = null;
        foreach (var address in safety.Addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, endpoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                socket.Dispose();
                lastFailure = ex;

                if (ex is OperationCanceledException)
                {
                    throw;
                }
            }
        }

        throw new HttpRequestException("Web Push endpoint connection failed.", lastFailure);
    }
}
