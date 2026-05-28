// ABOUTME: Handles updates to event templates with full definition and option replacement.
// ABOUTME: Validates governance per definition, checks template-key uniqueness excluding self, and replaces all definitions transactionally.

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

public class UpdateEventTemplateCommandHandler : IRequestHandler<UpdateEventTemplateCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventTemplateRepository _eventTemplateRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventTemplateCommandHandler(
        IEventTemplateRepository eventTemplateRepository,
        ICustomPropertyGovernancePolicy customPropertyGovernancePolicy,
        ICustomPropertyQuotaResolver quotaResolver,
        ICurrentUserService currentUserService,
        IMapper mapper,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _eventTemplateRepository = eventTemplateRepository;
        _customPropertyGovernancePolicy = customPropertyGovernancePolicy;
        _quotaResolver = quotaResolver;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventTemplateCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventTemplateDtoValidator();
        var validationResult = await validator.ValidateAsync(request.TemplateDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event template update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var template = await _eventTemplateRepository.GetTrackedTemplateWithDefinitions(request.TemplateDto.Id, cancellationToken);
        if (template == null)
        {
            response.Success = false;
            response.Message = "Event template not found.";
            return response;
        }

        if (await _eventTemplateRepository.ExistsTemplateKey(
                template.TenantId,
                request.TemplateDto.TemplateKey,
                request.TemplateDto.Version,
                template.Id))
        {
            response.Success = false;
            response.Message = "Event template update failed.";
            response.Errors = ["A template with the same TemplateKey and Version already exists for this tenant."];
            return response;
        }

        var maxDefinitions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key,
            template.TenantId,
            cancellationToken);
        if (request.TemplateDto.Definitions.Count > maxDefinitions)
        {
            response.SetQuotaExceeded(
                "Event template update failed.",
                new QuotaExceededDetails(
                    CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key,
                    maxDefinitions,
                    null,
                    request.TemplateDto.Definitions.Count,
                    "event_template_definitions",
                    template.TenantId));
            return response;
        }

        var maxOptions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key,
            template.TenantId,
            cancellationToken);
        var overOptionDefinition = request.TemplateDto.Definitions
            .FirstOrDefault(definition => definition.Options.Count > maxOptions);
        if (overOptionDefinition is not null)
        {
            response.SetQuotaExceeded(
                "Event template update failed.",
                new QuotaExceededDetails(
                    CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key,
                    maxOptions,
                    null,
                    overOptionDefinition.Options.Count,
                    "event_template_definition_options",
                    template.TenantId));
            return response;
        }

        var definitionsResult = BuildDefinitionEntities(request.TemplateDto.Definitions, template.TenantId);
        if (definitionsResult.Errors.Count > 0)
        {
            response.Success = false;
            response.Message = "Event template update failed.";
            response.Errors = definitionsResult.Errors;
            return response;
        }

        _mapper.Map(request.TemplateDto, template);
        template.UpdatedBy = _currentUserService.UserId;
        template.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(
            ct => _eventTemplateRepository.UpdateWithDefinitions(template, definitionsResult.Definitions, ct),
            cancellationToken);

        response.Success = true;
        response.Id = template.Id;
        response.Message = "Event template updated successfully.";

        await InvalidateCaches(template.TenantId, template.Id, cancellationToken);

        return response;
    }

    private (IReadOnlyCollection<TemplateDefinitionWithOptions> Definitions, List<string> Errors) BuildDefinitionEntities(
        List<CreateEventTemplateDefinitionDto> definitionDtos,
        Guid tenantId)
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
            definition.TenantId = tenantId;
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

    private async Task InvalidateCaches(Guid tenantId, Guid templateId, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(
            $"event-templates:list:{tenantId}:{(int?)null}:1:{PaginatedResult<EventTemplateListDto>.DefaultPageSize}",
            cancellationToken);
        await _cache.RemoveAsync($"event-templates:detail:{templateId}", cancellationToken);
    }
}
