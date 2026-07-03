// ABOUTME: DataProtection-backed implementation for protecting event-report evidence text.
// ABOUTME: Keeps sensitive reporter text encrypted before it reaches persistence.

using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.DataProtection;

namespace Explore.Infrastructure.Services;

public sealed class EventReportEvidenceProtector(IDataProtectionProvider dataProtectionProvider) : IEventReportEvidenceProtector
{
    private const string Purpose = "ISLAMU.EventReporting.Evidence.v1";

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    public string Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return _protector.Protect(plaintext);
    }

    public string Unprotect(string protectedText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedText);
        return _protector.Unprotect(protectedText);
    }
}
