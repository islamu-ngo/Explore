// ABOUTME: Handles creation of event templates with nested definition and option persistence.
// ABOUTME: Validates governance per definition, ensures template-key uniqueness, and persists three-level hierarchy transactionally.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.DTOs.EventTemplate.Validators;
using Explore.Application.Features.EventTemplates.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings.Definitions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventTemplates.Handlers.Commands;

public class CreateEventTemplateCommandHandler : IRequestHandler<CreateEventTemplateCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventTemplateRepository _eventTemplateRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEventTemplateCommandHandler(
        IEventTemplateRepository eventTemplateRepository,
        ICustomPropertyGovernancePolicy customPropertyGovernancePolicy,
        ICustomPropertyQuotaResolver quotaResolver,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IMapper mapper,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _eventTemplateRepository = eventTemplateRepository;
        _customPropertyGovernancePolicy = customPropertyGovernancePolicy;
        _quotaResolver = quotaResolver;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventTemplateCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventTemplateDtoValidator();
        var validationResult = await validator.ValidateAsync(request.TemplateDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event template creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        if (await _eventTemplateRepository.ExistsTemplateKey(
                _tenantContext.TenantId,
                request.TemplateDto.TemplateKey,
                request.TemplateDto.Version))
        {
            response.Success = false;
            response.Message = "Event template creation failed.";
            response.Errors = ["A template with the same TemplateKey and Version already exists for this tenant."];
            return response;
        }

        var maxDefinitions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key,
            _tenantContext.TenantId,
            cancellationToken);
        if (request.TemplateDto.Definitions.Count > maxDefinitions)
        {
            response.Success = false;
            response.Message = "Event template creation failed.";
            response.Errors = [$"quota_exceeded: Event template definition limit of {maxDefinitions} has been exceeded for this template."];
            return response;
        }

        var definitionsResult = BuildDefinitionEntities(request.TemplateDto.Definitions);
        if (definitionsResult.Errors.Count > 0)
        {
            response.Success = false;
            response.Message = "Event template creation failed.";
            response.Errors = definitionsResult.Errors;
            return response;
        }

        var template = _mapper.Map<EventTemplate>(request.TemplateDto);
        template.TenantId = _tenantContext.TenantId;
        template.CreatedBy = _currentUserService.UserId;
        template.UpdatedBy = _currentUserService.UserId;

        template = await _unitOfWork.ExecuteInTransactionAsync(
            ct => _eventTemplateRepository.CreateWithDefinitions(template, definitionsResult.Definitions, ct),
            cancellationToken);

        response.Success = true;
        response.Id = template.Id;
        response.Message = "Event template created successfully.";

        await _cache.RemoveAsync(
            GetListCacheKey(_tenantContext.TenantId, null, 1, PaginatedResult<object>.DefaultPageSize),
            cancellationToken);

        return response;
    }

    private (IReadOnlyCollection<TemplateDefinitionWithOptions> Definitions, List<string> Errors) BuildDefinitionEntities(
        List<CreateEventTemplateDefinitionDto> definitionDtos)
    {
        var errors = new List<string>();
        var definitions = new List<TemplateDefinitionWithOptions>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var defDto in definitionDtos)
        {
            var governance = _customPropertyGovernancePolicy.EvaluateDefinition(defDto.Namespace, defDto.Key);
            if (!governance.IsValid)
            {
                errors.AddRange(governance.Errors);
                continue;
            }

            var compositeKey = $"{governance.NormalizedNamespace}:{governance.NormalizedKey}";
            if (!seenKeys.Add(compositeKey))
            {
                errors.Add($"Duplicate definition key '{compositeKey}' within the same template.");
                continue;
            }

            var definition = _mapper.Map<EventTemplateCustomPropertyDefinition>(defDto);
            definition.TenantId = _tenantContext.TenantId;
            definition.Namespace = governance.NormalizedNamespace;
            definition.Key = governance.NormalizedKey;
            definition.CreatedBy = _currentUserService.UserId;
            definition.UpdatedBy = _currentUserService.UserId;

            var options = CreateOptionEntities(defDto.Options, definition.Id);
            var defaultOption = options.SingleOrDefault(x => x.IsDefault);

            definitions.Add(new TemplateDefinitionWithOptions(definition, options, defaultOption?.Id));
        }

        return (definitions, errors);
    }

    private List<EventTemplateCustomPropertyOption> CreateOptionEntities(
        IReadOnlyCollection<CreateEventTemplateOptionDto> optionDtos,
        Guid definitionId)
    {
        return optionDtos
            .Select(optionDto => new EventTemplateCustomPropertyOption
            {
                Id = Guid.NewGuid(),
                EventTemplateCustomPropertyDefinitionId = definitionId,
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

    private static string GetListCacheKey(Guid tenantId, int? eventTypeId, int pageNumber, int pageSize)
    {
        return $"event-templates:list:{tenantId}:{eventTypeId}:{pageNumber}:{pageSize}";
    }
}
