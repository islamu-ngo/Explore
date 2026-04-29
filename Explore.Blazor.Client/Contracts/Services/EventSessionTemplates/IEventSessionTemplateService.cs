// ABOUTME: Interface for event session template operations wrapping API client.
// ABOUTME: Keeps Blazor components behind a typed BFF-safe service abstraction.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventSessionTemplates;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Contracts.Services.EventSessionTemplates;

public interface IEventSessionTemplateService
{
    Task<PaginatedResult<EventSessionTemplateListModel>> GetTemplatesAsync(Guid? eventTemplateId = null, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default);
    Task<EventSessionTemplateDetailModel?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default);
    Task<BaseCommandResponse<Guid>?> CreateTemplateAsync(CreateEventSessionTemplateDto dto, CancellationToken ct = default);
    Task<BaseCommandResponse<Guid>?> UpdateTemplateAsync(Guid id, UpdateEventSessionTemplateDto dto, CancellationToken ct = default);
    Task<bool> DeleteTemplateAsync(Guid id, CancellationToken ct = default);
}
