// ABOUTME: Contract for managing footer link groups and links through the generated API client.
// ABOUTME: Uses generated request and response DTOs as the only backend contract models.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Footer;

public interface IFooterAdminService
{
    Task<FooterSettingsDto?> GetFooterSettingsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FooterLinkGroupListDto>> GetLinkGroupsAsync(CancellationToken cancellationToken = default);
    Task<FooterLinkGroupDetailsDto?> GetLinkGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> CreateLinkGroupAsync(CreateFooterLinkGroupRequest request, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> UpdateLinkGroupAsync(Guid id, UpdateFooterLinkGroupRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteLinkGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> ReorderLinkGroupsAsync(IEnumerable<Guid> orderedIds, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> CreateLinkAsync(Guid groupId, CreateFooterLinkRequest request, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> UpdateLinkAsync(Guid id, UpdateFooterLinkRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteLinkAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> UpdateTenantSettingsAsync(UpdateTenantFooterSettingsRequest request, CancellationToken cancellationToken = default);
}
