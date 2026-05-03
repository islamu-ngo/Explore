// ABOUTME: Handles creation of ad-hoc event-local custom property definitions with governance and option persistence.
// ABOUTME: Used when organizers add properties directly without a template; mirrors shared-definition create flow scoped to EventId.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventCustomProperty.Validators;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings.Definitions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventCustomProperties.Handlers.Commands;

public class CreateEventCustomPropertyDefinitionCommandHandler : IRequestHandler<CreateEventCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventCustomPropertyRepository _eventCustomPropertyRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEventCustomPropertyDefinitionCommandHandler(
        IEventCustomPropertyRepository eventCustomPropertyRepository,
        ICustomPropertyGovernancePolicy customPropertyGovernancePolicy,
        ICustomPropertyQuotaResolver quotaResolver,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IMapper mapper,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _eventCustomPropertyRepository = eventCustomPropertyRepository;
        _customPropertyGovernancePolicy = customPropertyGovernancePolicy;
        _quotaResolver = quotaResolver;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCustomPropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventCustomPropertyDefinitionDtoValidator();
        var validationResult = await validator.ValidateAsync(request.DefinitionDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event custom property definition creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var governance = _customPropertyGovernancePolicy.EvaluateDefinition(request.DefinitionDto.Namespace, request.DefinitionDto.Key);
        if (!governance.IsValid)
        {
            response.Success = false;
            response.Message = "Event custom property definition creation failed.";
            response.Errors = governance.Errors.ToList();
            return response;
        }

        if (await _eventCustomPropertyRepository.ExistsDefinitionKey(
                request.DefinitionDto.EventId,
                governance.NormalizedNamespace,
                governance.NormalizedKey))
        {
            response.Success = false;
            response.Message = "Event custom property definition creation failed.";
            response.Errors = ["A custom property definition with the same Namespace + Key already exists for this event."];
            return response;
        }

        var maxDefinitions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerEvent.Key,
            _tenantContext.TenantId,
            cancellationToken);
        var currentDefinitionCount = await _eventCustomPropertyRepository.CountDefinitionsForEvent(
            request.DefinitionDto.EventId,
            cancellationToken);
        if (currentDefinitionCount >= maxDefinitions)
        {
            response.Success = false;
            response.Message = "Event custom property definition creation failed.";
            response.Errors = [$"quota_exceeded: Event custom-property definition limit of {maxDefinitions} has been reached."];
            return response;
        }

        var maxOptions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key,
            _tenantContext.TenantId,
            cancellationToken);
        if (request.DefinitionDto.Options.Count > maxOptions)
        {
            response.Success = false;
            response.Message = "Event custom property definition creation failed.";
            response.Errors = [$"quota_exceeded: Custom-property option limit of {maxOptions} has been exceeded for this definition."];
            return response;
        }

        var definition = _mapper.Map<EventCustomPropertyDefinition>(request.DefinitionDto);
        definition.TenantId = _tenantContext.TenantId;
        definition.Namespace = governance.NormalizedNamespace;
        definition.Key = governance.NormalizedKey;
        definition.InstantiatedAt = DateTimeOffset.UtcNow;
        definition.CreatedBy = _currentUserService.UserId;
        definition.UpdatedBy = _currentUserService.UserId;

        var options = CreateOptionEntities(request.DefinitionDto.Options, definition.Id);
        var defaultOption = options.SingleOrDefault(x => x.IsDefault);

        definition = await _unitOfWork.ExecuteInTransactionAsync(
            ct => _eventCustomPropertyRepository.CreateWithOptions(definition, options, defaultOption?.Id, ct),
            cancellationToken);

        response.Success = true;
        response.Id = definition.Id;
        response.Message = "Event custom property definition created successfully.";

        await _cache.RemoveAsync(
            GetListCacheKey(request.DefinitionDto.EventId, 1, PaginatedResult<object>.DefaultPageSize),
            cancellationToken);

        return response;
    }

    private List<EventCustomPropertyOption> CreateOptionEntities(
        IReadOnlyCollection<DTOs.EventCustomProperty.CreateEventCustomPropertyOptionDto> optionDtos,
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

    private static string GetListCacheKey(Guid eventId, int pageNumber, int pageSize)
    {
        return $"event-custom-properties:list:{eventId}:{pageNumber}:{pageSize}";
    }
}
