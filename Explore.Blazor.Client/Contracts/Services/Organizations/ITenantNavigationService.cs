// ABOUTME: Service for managing tenant navigation links.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Contracts.Services.Organizations;

/// <summary>
/// Service interface for managing tenant navigation links.
/// Provides methods to retrieve, create, update, delete, and reorder navigation links.
/// DTOs are sourced from the generated NSwag client (Explore.Blazor.Client.Clients).
/// </summary>
public interface ITenantNavigationService
{
    /// <summary>
    /// Retrieves all navigation links for the current tenant.
    /// </summary>
    /// <returns>Collection of navigation link DTOs, or empty list if none exist or on error.</returns>
    Task<ICollection<TenantNavigationLinkDto>> GetNavigationLinksAsync();

    /// <summary>
    /// Creates a new navigation link for the tenant.
    /// </summary>
    /// <param name="dto">DTO containing the navigation link data to create.</param>
    /// <returns>Response containing the created link ID, or null on error.</returns>
    Task<BaseCommandResponse<Guid>?> CreateNavigationLinkAsync(CreateTenantNavigationLinkDto dto);

    /// <summary>
    /// Updates an existing navigation link.
    /// </summary>
    /// <param name="id">The ID of the navigation link to update.</param>
    /// <param name="dto">DTO containing the updated navigation link data.</param>
    /// <returns>Response indicating success or failure.</returns>
    Task<BaseCommandResponse<bool>?> UpdateNavigationLinkAsync(Guid id, UpdateTenantNavigationLinkDto dto);

    /// <summary>
    /// Deletes a navigation link.
    /// </summary>
    /// <param name="id">The ID of the navigation link to delete.</param>
    /// <returns>Response indicating success or failure.</returns>
    Task<BaseCommandResponse<bool>?> DeleteNavigationLinkAsync(Guid id);

    /// <summary>
    /// Reorders multiple navigation links.
    /// </summary>
    /// <param name="orders">List of navigation links with their new order values.</param>
    /// <returns>Response indicating success or failure.</returns>
    Task<BaseCommandResponse<bool>?> ReorderNavigationLinksAsync(List<UpdateTenantNavigationLinkOrderDto> orders);
}
