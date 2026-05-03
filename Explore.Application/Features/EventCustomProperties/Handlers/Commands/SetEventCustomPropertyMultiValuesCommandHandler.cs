// ABOUTME: Handles bulk replacement of all values for a multi-value custom property definition.
// ABOUTME: Atomically removes existing values and inserts the new set with sequential ordinals.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventCustomProperty.Validators;
using Explore.Application.Features.CustomProperties;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Handlers.Commands;

public class SetEventCustomPropertyMultiValuesCommandHandler : IRequestHandler<SetEventCustomPropertyMultiValuesCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventCustomPropertyRepository _eventCustomPropertyRepository;
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public SetEventCustomPropertyMultiValuesCommandHandler(
        IEventCustomPropertyRepository eventCustomPropertyRepository,
        IEventCustomPropertyProjectionUpdater projectionUpdater,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _eventCustomPropertyRepository = eventCustomPropertyRepository;
        _projectionUpdater = projectionUpdater;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SetEventCustomPropertyMultiValuesCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new SetEventCustomPropertyValueDtoValidator();
        var errors = new List<string>();
        for (var i = 0; i < request.Values.Count; i++)
        {
            var validationResult = await validator.ValidateAsync(request.Values[i], cancellationToken);
            if (!validationResult.IsValid)
            {
                errors.AddRange(validationResult.Errors.Select(e => $"Value[{i}]: {e.ErrorMessage}"));
            }
        }

        if (errors.Count > 0)
        {
            response.Success = false;
            response.Message = "Event custom property multi-value set failed.";
            response.Errors = errors;
            return response;
        }

        var definition = await _eventCustomPropertyRepository.GetDefinitionWithDetails(request.DefinitionId);
        if (definition is null || definition.EventId != request.EventId)
        {
            response.Success = false;
            response.Message = "Event custom property multi-value set failed.";
            response.Errors = ["Event custom property definition was not found for the requested event."];
            return response;
        }

        var runtimeValidationErrors = CustomPropertyRuntimeValueValidator.ValidateMany(definition, request.Values);
        if (runtimeValidationErrors.Count > 0)
        {
            response.Success = false;
            response.Message = "Event custom property multi-value set failed.";
            response.Errors = runtimeValidationErrors;
            return response;
        }

        var values = request.Values
            .Select((dto, index) =>
            {
                var value = _mapper.Map<EventCustomPropertyValue>(dto);
                value.EventCustomPropertyDefinitionId = request.DefinitionId;
                value.EventId = request.EventId;
                value.TenantId = _tenantContext.TenantId;
                value.Ordinal = index;
                value.CreatedBy = _currentUserService.UserId;
                value.UpdatedBy = _currentUserService.UserId;
                return value;
            })
            .ToList();

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _eventCustomPropertyRepository.SetMultiValues(request.DefinitionId, request.EventId, values, ct);
                await _projectionUpdater.UpdateForDefinitionAsync(request.DefinitionId, ct);
            },
            cancellationToken);

        response.Success = true;
        response.Id = request.DefinitionId;
        response.Message = "Event custom property values set successfully.";

        return response;
    }
}
