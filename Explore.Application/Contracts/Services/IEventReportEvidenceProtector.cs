// ABOUTME: Application contract for protecting sensitive event-report evidence text.
// ABOUTME: Lets handlers store encrypted reporter text without depending on infrastructure details.

namespace Explore.Application.Contracts.Services;

public interface IEventReportEvidenceProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedText);
}
