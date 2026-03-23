// ABOUTME: Handler for updating an event registration with validation.
// ABOUTME: Validates input, fetches entity, applies status or seat changes.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration.Validators;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Commands;

public class UpdateEventRegistrationCommandHandler : IRequestHandler<UpdateEventRegistrationCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IApprovalStatusRepository _approvalStatusRepository;
    private readonly IMapper _mapper;

    public UpdateEventRegistrationCommandHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IUserRepository userRepository,
        IEventSessionRepository eventSessionRepository,
        IApprovalStatusRepository approvalStatusRepository,
        IMapper mapper)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _userRepository = userRepository;
        _eventSessionRepository = eventSessionRepository;
        _approvalStatusRepository = approvalStatusRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventRegistrationDtoValidator(_userRepository, _eventSessionRepository, _approvalStatusRepository);
        var validationResult = await validator.ValidateAsync(request.EventRegistrationDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event Registration update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var eventRegistration = await _eventRegistrationRepository.GetById(request.EventRegistrationDto.Id);

        if (eventRegistration == null)
        {
            response.Success = false;
            response.Message = "Event Registration not found.";
            return response;
        }

        _mapper.Map(request.EventRegistrationDto, eventRegistration);
        await _eventRegistrationRepository.Update(eventRegistration);

        response.Success = true;
        response.Id = eventRegistration.Id;
        response.Message = "Event Registration updated successfully.";

        return response;
    }
}
