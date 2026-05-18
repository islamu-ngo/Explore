// ABOUTME: Unit tests for discrete PostgreSQL bootstrap doctor checks.
// ABOUTME: Blocks regressions that would bypass BootstrapSecretLoader with pre-built connection strings.

using Explore.Diagnostic.Doctor;
using Explore.Diagnostic.Doctor.Checks;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.Doctor.Checks;

public class BootstrapConfigurationDoctorCheckTests
{
    private const string Root = "/repo";
    private static readonly string ComposePath = Path.Combine(Root, "docker-compose.yml");

    [Test]
    public async Task RunAsync_WithDiscretePostgresVariables_ReturnsPass()
    {
        var fileSystem = new FakeDoctorFileSystem();
        fileSystem.AddFile(ComposePath, ComposeWithBootstrapVariables() + "\n# Never pre-build ConnectionStrings__DefaultConnection here.");
        var check = new BootstrapConfigurationDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Pass);
    }

    [Test]
    public async Task RunAsync_WithPrebuiltDefaultConnection_ReturnsFail()
    {
        var fileSystem = new FakeDoctorFileSystem();
        fileSystem.AddFile(ComposePath, ComposeWithBootstrapVariables() + "\nConnectionStrings__DefaultConnection: Host=db;Password=secret");
        var check = new BootstrapConfigurationDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Fail);
        result.Summary.Should().Contain("pre-builds ConnectionStrings__DefaultConnection");
    }

    private static string ComposeWithBootstrapVariables() =>
        "POSTGRESQL_HOST: postgres\n" +
        "POSTGRESQL_PORT: 5432\n" +
        "POSTGRESQL_DATABASE: explore\n" +
        "POSTGRESQL_USERNAME: explore\n" +
        "POSTGRESQL_PASSWORD: explore\n";
}
