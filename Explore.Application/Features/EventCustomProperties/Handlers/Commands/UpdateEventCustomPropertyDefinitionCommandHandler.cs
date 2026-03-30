// ABOUTME: Handles updates to event-local custom property definitions with governance and option replacement.
// ABOUTME: Preserves provenance fields (read-only) while allowing organizer customization of instantiated definitions.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.DTOs.EventCustomProperty.Validators;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventCustomProperties.Handlers.Commands;

public class UpdateEventCustomPropertyDefinitionCommandHandler : IRequestHandler<UpdateEventCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventCustomPropertyRepository _eventCustomPropertyRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventCustomPropertyDefinitionCommandHandler(
        IEventCustomPropertyRepository eventCustomPropertyRepository,
        ICustomPropertyGovernancePolicy customPropertyGovernancePolicy,
        ICurrentUserService currentUserService,
        IMapper mapper,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _eventCustomPropertyRepository = eventCustomPropertyRepository;
        _customPropertyGovernancePolicy = customPropertyGovernancePolicy;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventCustomPropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventCustomPropertyDefinitionDtoValidator();
        var validationResult = await validator.ValidateAsync(request.DefinitionDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event custom property definition update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var definition = await _eventCustomPropertyRepository.GetTrackedDefinitionWithOptions(request.DefinitionDto.Id, cancellationToken);
        if (definition == null)
        {
            response.Success = false;
            response.Message = "Event custom property definition not found.";
            return response;
        }

        var governance = _customPropertyGovernancePolicy.EvaluateDefinition(request.DefinitionDto.Namespace, request.DefinitionDto.Key);
        if (!governance.IsValid)
        {
            response.Success = false;
            response.Message = "Event custom property definition update failed.";
            response.Errors = governance.Errors.ToList();
            return response;
        }

        if (await _eventCustomPropertyRepository.ExistsDefinitionKey(
                definition.EventId,
                governance.NormalizedNamespace,
                governance.NormalizedKey,
                definition.Id))
        {
            response.Success = false;
            response.Message = "Event custom property definition update failed.";
            response.Errors = ["A custom property definition with the same Namespace + Key already exists for this event."];
            return response;
        }

        _mapper.Map(request.DefinitionDto, definition);
        definition.Namespace = governance.NormalizedNamespace;
        definition.Key = governance.NormalizedKey;
        definition.UpdatedBy = _currentUserService.UserId;
        definition.UpdatedAt = DateTime.UtcNow;

        var options = CreateOptionEntities(request.DefinitionDto.Options, definition.Id);
        var defaultOption = options.SingleOrDefault(x => x.IsDefault);

        await _unitOfWork.ExecuteInTransactionAsync(
            ct => _eventCustomPropertyRepository.UpdateWithOptions(definition, options, defaultOption?.Id, ct),
            cancellationToken);

        response.Success = true;
        response.Id = definition.Id;
        response.Message = "Event custom property definition updated successfully.";

        await InvalidateCaches(definition.EventId, definition.Id, cancellationToken);

        return response;
    }

    private List<EventCustomPropertyOption> CreateOptionEntities(
        IReadOnlyCollection<CreateEventCustomPropertyOptionDto> optionDtos,
        Guid definitionId)
    {
        return optionDtos
            .Select(optionDto => new EventCustomPropertyOption
            {
                Id = Guid.NewGuid(),
                EventCustomPropertyDefinitionId = definitionId,
                Namespace = CustomPropertyIdentity.NormalizeNamespace(optionDto.Namespace),
                Key = CustomPropertyIdentity.NormalizeKey(optionDto.Key),
                DisplayName = optionDto.DisplayName,
                Description = optionDto.Description,
                Value = optionDto.Value,
                IsDefault = optionDto.IsDefault,
                IsActive = optionDto.IsActive,
                SortOrder = optionDto.SortOrder,
                CreatedBy = _currentUserService.UserId,
                UpdatedBy = _currentUserService.UserId,
            })
            .ToList();
    }

    private async Task InvalidateCaches(Guid eventId, Guid definitionId, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(
            $"event-custom-properties:list:{eventId}:1:{PaginatedResult<EventCustomPropertyDefinitionListDto>.DefaultPageSize}",
            cancellationToken);
        await _cache.RemoveAsync($"event-custom-properties:detail:{definitionId}", cancellationToken);
    }
}
