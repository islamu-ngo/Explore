// ABOUTME: Handles setting a single custom property value for an event session (upsert by definition+session+ordinal).
// ABOUTME: Validates the value DTO and delegates persistence to the repository's upsert operation.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionCustomProperty.Validators;
using Explore.Application.Features.CustomProperties;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Handlers.Commands;

public class SetEventSessionCustomPropertyValueCommandHandler : IRequestHandler<SetEventSessionCustomPropertyValueCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionCustomPropertyRepository _sessionCustomPropertyRepository;
    private readonly IEventSessionCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public SetEventSessionCustomPropertyValueCommandHandler(
        IEventSessionCustomPropertyRepository sessionCustomPropertyRepository,
        IEventSessionCustomPropertyProjectionUpdater projectionUpdater,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _sessionCustomPropertyRepository = sessionCustomPropertyRepository;
        _projectionUpdater = projectionUpdater;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SetEventSessionCustomPropertyValueCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new SetEventSessionCustomPropertyValueDtoValidator();
        var validationResult = await validator.ValidateAsync(request.ValueDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session custom property value set failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var definition = await _sessionCustomPropertyRepository.GetDefinitionWithDetails(request.ValueDto.EventSessionCustomPropertyDefinitionId);
        if (definition is null || definition.EventSessionId != request.ValueDto.EventSessionId)
        {
            response.Success = false;
            response.Message = "Event session custom property value set failed.";
            response.Errors = ["Event session custom property definition was not found for the requested session."];
            return response;
        }

        if (!definition.IsMulti && request.ValueDto.Ordinal > 0)
        {
            response.Success = false;
            response.Message = "Event session custom property value set failed.";
            response.Errors = ["Single-value custom property definitions only accept ordinal 0."];
            return response;
        }

        if (definition.IsMulti)
        {
            var incomingKey = CustomPropertyValueNormalization.CreateKey(request.ValueDto);
            var duplicateExistingValue = definition.Values
                .Where(value => value.Ordinal != request.ValueDto.Ordinal)
                .Any(value => CustomPropertyValueNormalization.CreateKey(value) == incomingKey);

            if (duplicateExistingValue)
            {
                response.Success = false;
                response.Message = "Event session custom property value set failed.";
                response.Errors = ["Duplicate normalized values are not allowed for the same definition and session."];
                return response;
            }
        }

        var value = _mapper.Map<EventSessionCustomPropertyValue>(request.ValueDto);
        value.TenantId = _tenantContext.TenantId;
        value.CreatedBy = _currentUserService.UserId;
        value.UpdatedBy = _currentUserService.UserId;

        var persisted = await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var saved = await _sessionCustomPropertyRepository.SetValue(value, ct);
                await _projectionUpdater.UpdateForValueAsync(saved.Id, ct);
                return saved;
            },
            cancellationToken);

        response.Success = true;
        response.Id = persisted.Id;
        response.Message = "Event session custom property value set successfully.";

        return response;
    }
}
