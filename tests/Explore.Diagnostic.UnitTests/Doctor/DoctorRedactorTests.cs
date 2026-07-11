// ABOUTME: Unit tests for doctor output redaction.
// ABOUTME: Protects passwords, tokens, cookies, authorization values, and URI credentials from CLI output.

using Explore.Diagnostic.Doctor;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.Doctor;

public class DoctorRedactorTests
{
    [Test]
    public void Redact_RemovesSensitiveKeyValues()
    {
        var redacted = DoctorRedactor.Redact("Host=db;Password=s3cr3t Token=abc Authorization=Bearer xyz");

        redacted.Should().Contain("Password=<redacted>");
        redacted.Should().Contain("Token=<redacted>");
        redacted.Should().Contain("Authorization=<redacted>");
        redacted.Should().NotContain("s3cr3t");
        redacted.Should().NotContain("abc");
        redacted.Should().NotContain("Bearer");
    }

    [Test]
    public void Redact_RemovesUriCredentials()
    {
        var redacted = DoctorRedactor.Redact("postgres://user:password@database:5432/explore");

        redacted.Should().Be("postgres://<redacted>@database:5432/explore");
    }
}
