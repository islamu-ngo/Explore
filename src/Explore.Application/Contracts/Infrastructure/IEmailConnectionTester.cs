// ABOUTME: Application boundary for testing the configured email provider connection.
// ABOUTME: Keeps SMTP client and transport details outside controllers, health checks, and handlers.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IEmailConnectionTester
{
    Task<EmailResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}
