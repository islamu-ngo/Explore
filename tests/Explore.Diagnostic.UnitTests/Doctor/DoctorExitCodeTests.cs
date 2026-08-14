// ABOUTME: Unit tests for doctor PASS/WARN/FAIL exit-code semantics.
// ABOUTME: Ensures warnings stay non-blocking while hard failures fail automation.

using Explore.Diagnostic.Doctor;

namespace Explore.Diagnostic.UnitTests.Doctor;

public class DoctorExitCodeTests
{
    [Test]
    public async Task FromReport_WithOnlyPassAndWarn_ReturnsSuccess()
    {
        var report = new DoctorReport([
            DoctorCheckResult.Pass("pass", DoctorCheckCategory.Tooling, "ok", "none", "docs/OPERATIONS.md"),
            DoctorCheckResult.Warn("warn", DoctorCheckCategory.Configuration, "warn", "fix", "docs/TROUBLESHOOTING.md"),
        ]);

        await Assert.That(DoctorExitCodes.FromReport(report)).IsEqualTo(DoctorExitCodes.Success);
    }

    [Test]
    public async Task FromReport_WithFail_ReturnsHardFailure()
    {
        var report = new DoctorReport([
            DoctorCheckResult.Fail("fail", DoctorCheckCategory.Tooling, "failed", "fix", "docs/TROUBLESHOOTING.md"),
        ]);

        await Assert.That(DoctorExitCodes.FromReport(report)).IsEqualTo(DoctorExitCodes.HardFailure);
    }
}
