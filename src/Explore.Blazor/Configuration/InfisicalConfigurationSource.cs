// ABOUTME: Defines the Blazor-local Infisical source used during BFF configuration bootstrap.
// ABOUTME: Keeps startup secret loading inside the isolated Blazor server project.

namespace Explore.Blazor.Configuration;

using Microsoft.Extensions.Configuration;

public sealed class InfisicalConfigurationSource : IConfigurationSource
{
    public required string Url { get; set; }

    public required string ProjectId { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }

    public required string Environment { get; set; }

    public List<string> Paths { get; } = ["/"];

    public bool ThrowOnFirstLoadFailure { get; set; } = true;

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new InfisicalConfigurationProvider(this);
}
