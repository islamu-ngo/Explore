// ABOUTME: Handles updates to session-local custom property definitions with governance and option replacement.
// ABOUTME: Preserves provenance fields (read-only) while allowing organizer customization of instantiated definitions.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.DTOs.EventSessionCustomProperty.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings.Definitions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessionCustomProperties.Handlers.Commands;

public class UpdateEventSessionCustomPropertyDefinitionCommandHandler : IRequestHandler<UpdateEventSessionCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionCustomPropertyRepository _sessionCustomPropertyRepository;
    private readonly IEventSessionCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventSessionCustomPropertyDefinitionCommandHandler(
        IEventSessionCustomPropertyRepository sessionCustomPropertyRepository,
        IEventSessionCustomPropertyProjectionUpdater projectionUpdater,
        ICustomPropertyGovernancePolicy customPropertyGovernancePolicy,
        ICustomPropertyQuotaResolver quotaResolver,
        ICurrentUserService currentUserService,
        IMapper mapper,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _sessionCustomPropertyRepository = sessionCustomPropertyRepository;
        _projectionUpdater = projectionUpdater;
        _customPropertyGovernancePolicy = customPropertyGovernancePolicy;
        _quotaResolver = quotaResolver;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionCustomPropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionCustomPropertyDefinitionDtoValidator();
        var validationResult = await validator.ValidateAsync(request.DefinitionDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session custom property definition update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var definition = await _sessionCustomPropertyRepository.GetTrackedDefinitionWithOptions(request.DefinitionDto.Id, cancellationToken);
        if (definition == null)
        {
            response.Success = false;
            response.Message = "Event session custom property definition not found.";
            return response;
        }

        if (definition.ConcurrencyStamp != request.DefinitionDto.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event session custom-property definition changed since it was loaded. Reload and try again.",
                "event_session_custom_property_definition",
                definition.Id.ToString());
        }

        var governance = _customPropertyGovernancePolicy.EvaluateDefinition(request.DefinitionDto.Namespace, request.DefinitionDto.Key);
        if (!governance.IsValid)
        {
            response.Success = false;
            response.Message = "Event session custom property definition update failed.";
            response.Errors = governance.Errors.ToList();
            return response;
        }

        if (await _sessionCustomPropertyRepository.ExistsDefinitionKey(
                definition.EventSessionId,
                governance.NormalizedNamespace,
                governance.NormalizedKey,
                definition.Id))
        {
            response.Success = false;
            response.Message = "Event session custom property definition update failed.";
            response.Errors = ["A custom property definition with the same Namespace + Key already exists for this session."];
            return response;
        }

        var maxOptions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key,
            definition.TenantId,
            cancellationToken);
        if (request.DefinitionDto.Options.Count > maxOptions)
        {
            response.Success = false;
            response.Message = "Event session custom property definition update failed.";
            response.Errors = [$"quota_exceeded: Custom-property option limit of {maxOptions} has been exceeded for this definition."];
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
            async ct =>
            {
                await _sessionCustomPropertyRepository.UpdateWithOptions(definition, options, defaultOption?.Id, ct);
                await _projectionUpdater.UpdateForDefinitionAsync(definition.Id, ct);
            },
            cancellationToken);

        response.Success = true;
        response.Id = definition.Id;
        response.Message = "Event session custom property definition updated successfully.";

        await InvalidateCaches(definition.EventSessionId, definition.Id, cancellationToken);

        return response;
    }

    private List<EventSessionCustomPropertyOption> CreateOptionEntities(
        IReadOnlyCollection<CreateEventSessionCustomPropertyOptionDto> optionDtos,
        Guid definitionId)
    {
        return optionDtos
            .Select(optionDto => new EventSessionCustomPropertyOption
            {
                Id = Guid.NewGuid(),
                EventSessionCustomPropertyDefinitionId = definitionId,
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

    private async Task InvalidateCaches(Guid eventSessionId, Guid definitionId, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(
            $"session-custom-properties:list:{eventSessionId}:1:{PaginatedResult<EventSessionCustomPropertyDefinitionListDto>.DefaultPageSize}",
            cancellationToken);
        await _cache.RemoveAsync($"session-custom-properties:detail:{definitionId}", cancellationToken);
    }
}
