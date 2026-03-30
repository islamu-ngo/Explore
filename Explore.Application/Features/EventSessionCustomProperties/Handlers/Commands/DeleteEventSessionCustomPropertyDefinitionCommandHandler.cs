// ABOUTME: Handles deletion of session-local custom property definitions with cascaded value and option cleanup.
// ABOUTME: Uses hard delete so namespace+key can be reused without stale-row conflicts.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessionCustomProperties.Handlers.Commands;

public class DeleteEventSessionCustomPropertyDefinitionCommandHandler : IRequestHandler<DeleteEventSessionCustomPropertyDefinitionCommand, bool>
{
    private readonly IEventSessionCustomPropertyRepository _sessionCustomPropertyRepository;
    private readonly HybridCache _cache;

    public DeleteEventSessionCustomPropertyDefinitionCommandHandler(
        IEventSessionCustomPropertyRepository sessionCustomPropertyRepository,
        HybridCache cache)
    {
        _sessionCustomPropertyRepository = sessionCustomPropertyRepository;
        _cache = cache;
    }

    public async Task<bool> Handle(DeleteEventSessionCustomPropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _sessionCustomPropertyRepository.GetDefinitionWithDetails(request.Id);
        if (definition == null)
        {
            return false;
        }

        var deleted = await _sessionCustomPropertyRepository.DeleteDefinition(request.Id, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        await _cache.RemoveAsync(
            $"session-custom-properties:list:{definition.EventSessionId}:1:{PaginatedResult<object>.DefaultPageSize}",
            cancellationToken);
        await _cache.RemoveAsync($"session-custom-properties:detail:{definition.Id}", cancellationToken);

        return true;
    }
}
