// ABOUTME: Unit tests for structured database bootstrap doctor checks.
// ABOUTME: Blocks raw default connection strings and incomplete role credentials in Compose.

using Explore.Diagnostic.Doctor;
using Explore.Diagnostic.Doctor.Checks;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.Doctor.Checks;

public class BootstrapConfigurationDoctorCheckTests
{
    private const string Root = "/repo";
    private static readonly string ComposePath = Path.Combine(Root, "docker-compose.yml");

    [Test]
    public async Task RunAsync_WithStructuredDatabaseVariables_ReturnsPass()
    {
        var fileSystem = new FakeDoctorFileSystem();
        fileSystem.AddFile(ComposePath, ComposeWithDatabaseVariables() + "\n# Never pre-build ConnectionStrings__DefaultConnection here.");
        var check = new BootstrapConfigurationDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Pass);
    }

    [Test]
    public async Task RunAsync_WithPrebuiltDefaultConnection_ReturnsFail()
    {
        var fileSystem = new FakeDoctorFileSystem();
        fileSystem.AddFile(ComposePath, ComposeWithDatabaseVariables() + "\nConnectionStrings__DefaultConnection: Host=db;Password=secret");
        var check = new BootstrapConfigurationDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Fail);
        result.Summary.Should().Contain("raw ConnectionStrings__DefaultConnection");
        result.Summary.Should().NotContain("secret");
    }

    [Test]
    public async Task RunAsync_WhenMigratorCredentialsAreMissing_ReturnsFail()
    {
        var fileSystem = new FakeDoctorFileSystem();
        fileSystem.AddFile(ComposePath, ComposeWithDatabaseVariables().Replace("Database__Migrator__Password: migrator", string.Empty, StringComparison.Ordinal));
        var check = new BootstrapConfigurationDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Fail);
        result.Remediation.Should().Contain("Database__Migrator__Password");
    }

    private static string ComposeWithDatabaseVariables() =>
        "Database__Provider: PostgreSql\n" +
        "Database__Host: postgres\n" +
        "Database__Port: 5432\n" +
        "Database__Database: explore\n" +
        "Database__Runtime__Username: runtime\n" +
        "Database__Runtime__Password: runtime\n" +
        "Database__Migrator__Username: migrator\n" +
        "Database__Migrator__Password: migrator\n";
}
