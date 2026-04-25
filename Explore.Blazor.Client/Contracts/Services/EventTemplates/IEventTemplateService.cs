// ABOUTME: Interface for EventTemplate operations wrapping API client.
// ABOUTME: Follows the same pattern as ICustomPropertyDefinitionService.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventTemplates;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Contracts.Services.EventTemplates;

public interface IEventTemplateService
{
    Task<PaginatedResult<EventTemplateListModel>> GetTemplatesAsync(int? eventTypeId = null, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default);
    Task<EventTemplateDetailModel?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default);
    Task<BaseCommandResponse<Guid>?> CreateTemplateAsync(CreateEventTemplateDto dto, CancellationToken ct = default);
    Task<BaseCommandResponse<Guid>?> UpdateTemplateAsync(Guid id, UpdateEventTemplateDto dto, CancellationToken ct = default);
    Task<bool> DeleteTemplateAsync(Guid id, CancellationToken ct = default);
}