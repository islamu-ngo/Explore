// ABOUTME: Supplies the outbound transport and DNS resolver used by CarpaNet OAuth sessions.
// ABOUTME: Keeps production on the hardened ATProto handler while allowing deterministic host-level verification.

using CarpaNet.Identity;
using Explore.Atproto.Transport;

namespace Explore.Blazor.Services.Auth;

public interface IAtprotoOAuthTransportFactory
{
    HttpMessageHandler CreatePrimaryHandler(AtprotoOutboundPolicy policy, TimeSpan connectTimeout);

    IDnsResolver CreateDnsResolver();
}

public sealed class AtprotoOAuthTransportFactory : IAtprotoOAuthTransportFactory
{
    public HttpMessageHandler CreatePrimaryHandler(AtprotoOutboundPolicy policy, TimeSpan connectTimeout) =>
        AtprotoHardenedHttpClient.CreatePrimaryHandler(policy, connectTimeout);

    public IDnsResolver CreateDnsResolver() => new DefaultDnsResolver();
}
