// ABOUTME: Contract for sending emails with provider-agnostic SMTP abstraction.
// Supports multi-tenant configuration via the cascading settings engine.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Provider-agnostic email service. SMTP configuration is resolved per-tenant
/// from the cascading settings engine (SystemSetting → TenantSetting).
/// <para>
/// Instance admin can lock SMTP settings to enforce a SaaS-wide server,
/// or leave them unlocked so tenants can bring their own SMTP provider.
/// </para>
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email using the resolved SMTP configuration for the current tenant.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with diagnostics.</returns>
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the current tenant's SMTP connection without sending an email.
    /// Used by admin UI to validate configuration before saving.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating connection success or failure with timing.</returns>
    Task<EmailResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}
