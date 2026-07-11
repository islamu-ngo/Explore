// ABOUTME: Unit tests for bounded doctor runner behavior.
// ABOUTME: Ensures failed checks become visible WARN results rather than being swallowed.

using Explore.Diagnostic.Doctor;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.Doctor;

public class DoctorRunnerTests
{
    [Test]
    public async Task RunAsync_WhenCheckThrows_ReturnsWarnResultWithRedactedEvidence()
    {
        var runner = new DoctorRunner([new ThrowingCheck()]);

        var report = await runner.RunAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        report.Results.Should().ContainSingle();
        var result = report.Results[0];
        result.Status.Should().Be(DoctorCheckStatus.Warn);
        result.RedactedEvidence.Should().Contain("Password=<redacted>");
        result.RedactedEvidence.Should().NotContain("super-secret");
    }

    private sealed class ThrowingCheck : IDoctorCheck
    {
        public string Code => "test.throwing";
        public DoctorCheckCategory Category => DoctorCheckCategory.Configuration;

        public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Password=super-secret");
    }
}
