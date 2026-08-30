// ABOUTME: Resolves direct-transfer destination addresses for public-origin SSRF validation.
// ABOUTME: Returns the complete DNS answer set so Application rejects mixed public/private rebinding results.

namespace Explore.Infrastructure.Services;

using System.Net;
using Explore.Application.Features.ConfigurationManifest.Managed;

public sealed class ConfigurationTransferDestinationResolver
    : IConfigurationTransferDestinationResolver
{
    public async Task<IReadOnlyCollection<IPAddress>> ResolveAsync(
        string host,
        CancellationToken cancellationToken) =>
        await Dns.GetHostAddressesAsync(host, cancellationToken);
}
