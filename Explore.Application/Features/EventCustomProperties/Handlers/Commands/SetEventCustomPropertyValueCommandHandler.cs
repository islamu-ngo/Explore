// ABOUTME: Handles setting a single custom property value for an event (upsert by definition+event+ordinal).
// ABOUTME: Validates the value DTO and delegates persistence to the repository's upsert operation.

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

public class SetEventCustomPropertyValueCommandHandler : IRequestHandler<SetEventCustomPropertyValueCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventCustomPropertyRepository _eventCustomPropertyRepository;
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public SetEventCustomPropertyValueCommandHandler(
        IEventCustomPropertyRepository eventCustomPropertyRepository,
        IEventCustomPropertyProjectionUpdater projectionUpdater,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _eventCustomPropertyRepository = eventCustomPropertyRepository;
        _projectionUpdater = projectionUpdater;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SetEventCustomPropertyValueCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new SetEventCustomPropertyValueDtoValidator();
        var validationResult = await validator.ValidateAsync(request.ValueDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event custom property value set failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var definition = await _eventCustomPropertyRepository.GetDefinitionWithDetails(request.ValueDto.EventCustomPropertyDefinitionId);
        if (definition is null || definition.EventId != request.ValueDto.EventId)
        {
            response.Success = false;
            response.Message = "Event custom property value set failed.";
            response.Errors = ["Event custom property definition was not found for the requested event."];
            return response;
        }

        var runtimeValidationErrors = CustomPropertyRuntimeValueValidator.ValidateSingle(definition, request.ValueDto);
        if (runtimeValidationErrors.Count > 0)
        {
            response.Success = false;
            response.Message = "Event custom property value set failed.";
            response.Errors = runtimeValidationErrors;
            return response;
        }

        var value = _mapper.Map<EventCustomPropertyValue>(request.ValueDto);
        value.TenantId = _tenantContext.TenantId;
        value.CreatedBy = _currentUserService.UserId;
        value.UpdatedBy = _currentUserService.UserId;

        var persisted = await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var saved = await _eventCustomPropertyRepository.SetValue(value, ct);
                await _projectionUpdater.UpdateForValueAsync(saved.Id, ct);
                return saved;
            },
            cancellationToken);

        response.Success = true;
        response.Id = persisted.Id;
        response.Message = "Event custom property value set successfully.";

        return response;
    }
}
