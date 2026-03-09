// ABOUTME: Configuration for trusted reverse proxies and forwarded header processing in the API host.
// ABOUTME: Ensures host and client IP are derived from forwarded headers only when the proxy boundary is explicit.

using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Explore.API.Configuration;

public sealed class ForwardedHeadersTrustOptions
{
    public const string SectionName = "ForwardedHeadersTrust";

    public int? ForwardLimit { get; set; } = 1;

    public bool TrustLoopbackProxy { get; set; }

    public List<string> KnownProxies { get; init; } = [];

    public List<string> KnownNetworks { get; init; } = [];

    public void ApplyTo(ForwardedHeadersOptions options)
    {
        var hasTrustedProxyBoundary = TrustLoopbackProxy || KnownProxies.Count > 0 || KnownNetworks.Count > 0;

        options.ForwardedHeaders = hasTrustedProxyBoundary
            ? ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            : ForwardedHeaders.None;
        options.ForwardLimit = ForwardLimit;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        if (TrustLoopbackProxy)
        {
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
        }

        foreach (var knownProxy in KnownProxies)
        {
            if (IPAddress.TryParse(knownProxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }

        foreach (var knownNetwork in KnownNetworks)
        {
            if (TryParseNetwork(knownNetwork, out var network) && network is not null)
            {
                options.KnownIPNetworks.Add(network.Value);
            }
        }
    }

    private static bool TryParseNetwork(string candidate, out global::System.Net.IPNetwork? network)
    {
        network = null;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var segments = candidate.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2 || !IPAddress.TryParse(segments[0], out var prefix) || !int.TryParse(segments[1], out var prefixLength))
        {
            return false;
        }

        network = global::System.Net.IPNetwork.Parse($"{prefix}/{prefixLength}");
        return true;
    }
}
