// ABOUTME: Unit tests for Docker tooling doctor checks.
// ABOUTME: Proves checks use read-only version commands and fail hard when Compose is unavailable.

using Explore.Diagnostic.Doctor;
using Explore.Diagnostic.Doctor.Checks;
using Explore.Diagnostic.Doctor.Infrastructure;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.Doctor.Checks;

public class DockerDoctorCheckTests
{
    [Test]
    public async Task RunAsync_UsesOnlyReadOnlyVersionCommands()
    {
        var processRunner = new FakeDoctorProcessRunner();
        processRunner.AddResult("docker", "--version", new DoctorProcessResult(0, "Docker version 28", string.Empty));
        processRunner.AddResult("docker", "compose version", new DoctorProcessResult(0, "Docker Compose version v2", string.Empty));
        var check = new DockerDoctorCheck(processRunner);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Pass);
        processRunner.Calls.Should().Equal(("docker", "--version"), ("docker", "compose version"));
    }

    [Test]
    public async Task RunAsync_WhenComposeUnavailable_ReturnsFail()
    {
        var processRunner = new FakeDoctorProcessRunner();
        processRunner.AddResult("docker", "--version", new DoctorProcessResult(0, "Docker version 28", string.Empty));
        processRunner.AddResult("docker", "compose version", new DoctorProcessResult(1, string.Empty, "compose missing"));
        var check = new DockerDoctorCheck(processRunner);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Fail);
        result.Summary.Should().Contain("docker compose");
    }
}
