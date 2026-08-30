// ABOUTME: Client boundary for anonymous published legal-document reads.
// ABOUTME: Keeps API transport failures outside routable legal page components.

namespace Explore.Blazor.Client.Contracts.Services.LegalDocuments;

using Explore.Blazor.Client.Clients;

public interface ILegalDocumentService
{
    Task<PublicLegalDocumentDto?> GetAsync(
        string kindCode,
        CancellationToken cancellationToken = default);
}
