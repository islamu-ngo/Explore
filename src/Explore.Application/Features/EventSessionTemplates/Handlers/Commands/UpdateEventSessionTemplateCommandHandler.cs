// ABOUTME: Handles updates to event session templates with full definition and option replacement.
// ABOUTME: Validates governance per definition, checks session-template-key uniqueness excluding self, and replaces all definitions transactionally.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.DTOs.EventSessionTemplate.Validators;
using Explore.Application.Features.EventSessionTemplates.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings.Definitions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessionTemplates.Handlers.Commands;

public class UpdateEventSessionTemplateCommandHandler : IRequestHandler<UpdateEventSessionTemplateCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionTemplateRepository _sessionTemplateRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventSessionTemplateCommandHandler(
        IEventSessionTemplateRepository sessionTemplateRepository,
        ICustomPropertyGovernancePolicy customPropertyGovernancePolicy,
        ICustomPropertyQuotaResolver quotaResolver,
        ICurrentUserService currentUserService,
        IMapper mapper,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _sessionTemplateRepository = sessionTemplateRepository;
        _customPropertyGovernancePolicy = customPropertyGovernancePolicy;
        _quotaResolver = quotaResolver;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionTemplateCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionTemplateDtoValidator();
        var validationResult = await validator.ValidateAsync(request.SessionTemplateDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session template update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var sessionTemplate = await _sessionTemplateRepository.GetTrackedSessionTemplateWithDefinitions(request.SessionTemplateDto.Id, cancellationToken);
        if (sessionTemplate == null)
        {
            response.Success = false;
            response.Message = "Event session template not found.";
            return response;
        }

        if (await _sessionTemplateRepository.ExistsSessionTemplateKey(
                sessionTemplate.EventTemplateId,
                request.SessionTemplateDto.SessionTemplateKey,
                request.SessionTemplateDto.Version,
                sessionTemplate.Id))
        {
            response.Success = false;
            response.Message = "Event session template update failed.";
            response.Errors = ["A session template with the same SessionTemplateKey and Version already exists for this event template."];
            return response;
        }

        var maxDefinitions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key,
            sessionTemplate.TenantId,
            cancellationToken);
        if (request.SessionTemplateDto.Definitions.Count > maxDefinitions)
        {
            response.SetQuotaExceeded(
                "Event session template update failed.",
                new QuotaExceededDetails(
                    CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key,
                    maxDefinitions,
                    null,
                    request.SessionTemplateDto.Definitions.Count,
                    "event_session_template_definitions",
                    sessionTemplate.TenantId));
            return response;
        }

        var maxOptions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key,
            sessionTemplate.TenantId,
            cancellationToken);
        var overOptionDefinition = request.SessionTemplateDto.Definitions
            .FirstOrDefault(definition => definition.Options.Count > maxOptions);
        if (overOptionDefinition is not null)
        {
            response.SetQuotaExceeded(
                "Event session template update failed.",
                new QuotaExceededDetails(
                    CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key,
                    maxOptions,
                    null,
                    overOptionDefinition.Options.Count,
                    "event_session_template_definition_options",
                    sessionTemplate.TenantId));
            return response;
        }

        var definitionsResult = BuildDefinitionEntities(request.SessionTemplateDto.Definitions, sessionTemplate.TenantId);
        if (definitionsResult.Errors.Count > 0)
        {
            response.Success = false;
            response.Message = "Event session template update failed.";
            response.Errors = definitionsResult.Errors;
            return response;
        }

        _mapper.Map(request.SessionTemplateDto, sessionTemplate);
        sessionTemplate.UpdatedBy = _currentUserService.UserId;
        sessionTemplate.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(
            ct => _sessionTemplateRepository.UpdateWithDefinitions(sessionTemplate, definitionsResult.Definitions, ct),
            cancellationToken);

        response.Success = true;
        response.Id = sessionTemplate.Id;
        response.Message = "Event session template updated successfully.";

        await InvalidateCaches(sessionTemplate.EventTemplateId, sessionTemplate.Id, cancellationToken);

        return response;
    }

    private (IReadOnlyCollection<SessionTemplateDefinitionWithOptions> Definitions, List<string> Errors) BuildDefinitionEntities(
        List<CreateEventSessionTemplateDefinitionDto> definitionDtos,
        Guid tenantId)
    {
        var errors = new List<string>();
        var definitions = new List<SessionTemplateDefinitionWithOptions>();
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
                errors.Add($"Duplicate definition key '{compositeKey}' within the same session template.");
                continue;
            }

            var definition = _mapper.Map<EventSessionTemplateCustomPropertyDefinition>(defDto);
            definition.TenantId = tenantId;
            definition.Namespace = governance.NormalizedNamespace;
            definition.Key = governance.NormalizedKey;
            definition.CreatedBy = _currentUserService.UserId;
            definition.UpdatedBy = _currentUserService.UserId;

            var options = CreateOptionEntities(defDto.Options, definition.Id);
            var defaultOption = options.SingleOrDefault(x => x.IsDefault);

            definitions.Add(new SessionTemplateDefinitionWithOptions(definition, options, defaultOption?.Id));
        }

        return (definitions, errors);
    }

    private List<EventSessionTemplateCustomPropertyOption> CreateOptionEntities(
        IReadOnlyCollection<CreateEventSessionTemplateOptionDto> optionDtos,
        Guid definitionId)
    {
        return optionDtos
            .Select(optionDto => new EventSessionTemplateCustomPropertyOption
            {
                Id = Guid.CreateVersion7(),
                EventSessionTemplateCustomPropertyDefinitionId = definitionId,
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

    private async Task InvalidateCaches(Guid eventTemplateId, Guid sessionTemplateId, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(
            $"session-templates:list:{eventTemplateId}:1:{PaginatedResult<EventSessionTemplateListDto>.DefaultPageSize}",
            cancellationToken);
        await _cache.RemoveAsync($"session-templates:detail:{sessionTemplateId}", cancellationToken);
    }
}
