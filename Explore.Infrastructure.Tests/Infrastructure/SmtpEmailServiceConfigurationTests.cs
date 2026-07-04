// ABOUTME: No-container configuration tests for SmtpEmailService failure behavior.
// ABOUTME: Verifies missing SMTP configuration fails safely before provider handoff.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Infrastructure.Mail;
using Explore.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

[Category(InfrastructureTestCategories.Email)]
public sealed class SmtpEmailServiceConfigurationTests
{
    [Test]
    public async Task SendAsync_WhenSmtpConfigMissing_FailsWithoutProviderHandoff()
    {
        var service = CreateService(config: null);

        var result = await service.SendAsync(new EmailMessage
        {
            To = "attendee@example.test",
            Subject = "Configuration missing",
            PlainTextBody = "Body"
        });

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("SMTP is not configured");
    }

    [Test]
    public async Task TestConnectionAsync_WhenSmtpConfigMissing_ReturnsFailure()
    {
        var service = CreateService(config: null);

        var result = await service.TestConnectionAsync();

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("SMTP is not configured");
    }

    private static SmtpEmailService CreateService(SmtpConfiguration? config)
    {
        var resolver = Substitute.For<ISmtpConfigResolver>();
        resolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(config);
        return new SmtpEmailService(resolver, NullLogger<SmtpEmailService>.Instance);
    }
}
