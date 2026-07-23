// ABOUTME: Handles updates to shared Layer 3 custom-property definitions with governance and option replacement enforcement.
// ABOUTME: Keeps shared-definition update semantics explicit before template/runtime flows are introduced.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.DTOs.CustomPropertyDefinition.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.CustomPropertyDefinitions.Handlers.Commands;

public class UpdateCustomPropertyDefinitionCommandHandler : IRequestHandler<UpdateCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>
{
    private readonly ICustomPropertyDefinitionRepository _customPropertyDefinitionRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomPropertyDefinitionCommandHandler(
        ICustomPropertyDefinitionRepository customPropertyDefinitionRepository,
        ICustomPropertyGovernancePolicy customPropertyGovernancePolicy,
        ICustomPropertyQuotaResolver quotaResolver,
        ICurrentUserService currentUserService,
        IMapper mapper,
        HybridCache cache,
        IUnitOfWork unitOfWork)
    {
        _customPropertyDefinitionRepository = customPropertyDefinitionRepository;
        _customPropertyGovernancePolicy = customPropertyGovernancePolicy;
        _quotaResolver = quotaResolver;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateCustomPropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateCustomPropertyDefinitionDtoValidator();
        var validationResult = await validator.ValidateAsync(request.DefinitionDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Custom-property definition update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var definition = await _customPropertyDefinitionRepository.GetTrackedDefinitionWithOptions(request.DefinitionDto.Id, cancellationToken);
        if (definition == null)
        {
            response.Success = false;
            response.Message = "Custom-property definition not found.";
            return response;
        }

        if (definition.ConcurrencyStamp != request.DefinitionDto.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The custom-property definition changed since it was loaded. Reload and try again.",
                "custom_property_definition",
                definition.Id.ToString());
        }

        var governance = _customPropertyGovernancePolicy.EvaluateDefinition(request.DefinitionDto.Namespace, request.DefinitionDto.Key);
        if (!governance.IsValid)
        {
            response.Success = false;
            response.Message = "Custom-property definition update failed.";
            response.Errors = governance.Errors.ToList();
            return response;
        }

        if (await _customPropertyDefinitionRepository.ExistsScopedMachineKey(
                definition.TenantId,
                request.DefinitionDto.EntityTypeName,
                governance.NormalizedNamespace,
                governance.NormalizedKey,
                definition.Id))
        {
            response.Success = false;
            response.Message = "Custom-property definition update failed.";
            response.Errors = ["A custom-property definition with the same Namespace + Key already exists in this scope."];
            return response;
        }

        var maxOptions = await _quotaResolver.GetIntAsync(
            CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key,
            definition.TenantId,
            cancellationToken);
        if (request.DefinitionDto.Options.Count > maxOptions)
        {
            response.SetQuotaExceeded(
                "Custom-property definition update failed.",
                new QuotaExceededDetails(
                    CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key,
                    maxOptions,
                    null,
                    request.DefinitionDto.Options.Count,
                    "custom_property_definition_options",
                    definition.TenantId));
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
            ct => _customPropertyDefinitionRepository.UpdateWithOptions(definition, options, defaultOption?.Id, ct),
            cancellationToken);

        response.Success = true;
        response.Id = definition.Id;
        response.Message = "Custom-property definition updated successfully.";

        await InvalidateCaches(definition.EntityTypeName, definition.Id, cancellationToken);

        return response;
    }

    private List<CustomPropertyOption> CreateOptionEntities(IReadOnlyCollection<CreateCustomPropertyOptionDto> optionDtos, Guid definitionId)
    {
        return optionDtos
            .Select(optionDto => new CustomPropertyOption
            {
                Id = Guid.CreateVersion7(),
                CustomPropertyDefinitionId = definitionId,
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

    private async Task InvalidateCaches(EntityTypeName entityTypeName, Guid definitionId, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync($"custom-property-definitions:list:{entityTypeName}:1:{PaginatedResult<CustomPropertyDefinitionListDto>.DefaultPageSize}", cancellationToken);
        await _cache.RemoveAsync($"custom-property-definitions:detail:{definitionId}", cancellationToken);
    }
}
