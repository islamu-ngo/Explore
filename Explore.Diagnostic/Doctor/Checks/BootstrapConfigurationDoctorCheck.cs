// ABOUTME: Verifies Compose bootstrap variables follow the discrete PostgreSQL secret contract.
// ABOUTME: Flags pre-built connection strings because BootstrapSecretLoader owns composition.

using Explore.Diagnostic.Doctor.Infrastructure;

namespace Explore.Diagnostic.Doctor.Checks;

public sealed class BootstrapConfigurationDoctorCheck(IDoctorFileSystem fileSystem, string repositoryRoot) : IDoctorCheck
{
    private static readonly string[] RequiredBootstrapVariables =
    [
        "POSTGRESQL_HOST",
        "POSTGRESQL_PORT",
        "POSTGRESQL_DATABASE",
        "POSTGRESQL_USERNAME",
        "POSTGRESQL_PASSWORD",
    ];

    public string Code => "bootstrap.postgres.discrete-env";
    public DoctorCheckCategory Category => DoctorCheckCategory.Bootstrap;

    public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var composePath = Path.Combine(repositoryRoot, "docker-compose.yml");
        if (!fileSystem.FileExists(composePath))
        {
            return Task.FromResult(DoctorCheckResult.Fail(
                Code,
                Category,
                "docker-compose.yml is missing, so bootstrap environment variables cannot be verified.",
                "Restore docker-compose.yml or run doctor from the repository root.",
                "docs/SELF_HOSTING.md"));
        }

        var compose = fileSystem.ReadAllText(composePath);
        var activeComposeLines = string.Join(
            Environment.NewLine,
            compose
                .Split('\n')
                .Select(line => line.TrimStart())
                .Where(line => !line.StartsWith('#')));
        var missing = RequiredBootstrapVariables.Where(variable => !compose.Contains(variable, StringComparison.Ordinal)).ToList();
        if (missing.Count > 0)
        {
            return Task.FromResult(DoctorCheckResult.Fail(
                Code,
                Category,
                "Compose bootstrap configuration is missing required discrete PostgreSQL variables.",
                $"Add the missing variables to the x-postgres-bootstrap-env block: {string.Join(", ", missing)}.",
                "docs/SECRETS.md"));
        }

        if (activeComposeLines.Contains("ConnectionStrings__DefaultConnection", StringComparison.Ordinal))
        {
            return Task.FromResult(DoctorCheckResult.Fail(
                Code,
                Category,
                "Compose pre-builds ConnectionStrings__DefaultConnection instead of using discrete bootstrap values.",
                "Remove the pre-built connection string and let BootstrapSecretLoader compose it from POSTGRESQL_* values.",
                "docs/SECRETS.md"));
        }

        return Task.FromResult(DoctorCheckResult.Pass(
            Code,
            Category,
            "Compose uses the discrete PostgreSQL bootstrap contract expected by BootstrapSecretLoader.",
            "Keep POSTGRESQL_* values discrete and never print their raw password value in diagnostics.",
            "docs/SECRETS.md"));
    }
}
