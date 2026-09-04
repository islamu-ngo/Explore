// ABOUTME: Contract for system-level reference lookups (file types, DID custody types).
// ABOUTME: Encapsulates platform media and identity custody taxonomies for admin configuration.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface ISystemLookupService
{
    Task<ICollection<FileTypeListDto>> GetFileTypesAsync();
    Task<ICollection<DidCustodyTypeListDto>> GetDidCustodyTypesAsync();
}
