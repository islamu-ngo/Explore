// ABOUTME: No-op setup secret provider for benchmark API hosts.
// ABOUTME: Prevents startup bootstrap-state queries from polluting request performance benchmarks.

using Explore.Application.Contracts.Services;

namespace Event.Benchmarks.Api;

internal sealed class BenchmarkSetupSecretProvider : ISetupSecretProvider
{
    public bool IsSetupModeActive => false;

    public bool IsSetupSecretRequired => false;

    public bool IsFromEnvironmentVariable => false;

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public bool ValidateSecret(string? secret) => false;

    public void Lock()
    {
    }

    public string? GetSecretForLogging() => null;
}
