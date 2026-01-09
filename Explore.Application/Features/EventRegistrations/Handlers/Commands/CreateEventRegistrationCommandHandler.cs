using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration.Validators;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Commands
{
    public class CreateEventRegistrationCommandHandler : IRequestHandler<CreateEventRegistrationCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventRegistrationRepository _eventRegistrationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly IApprovalStatusRepository _approvalStatusRepository;
        private readonly IMapper _mapper;

        public CreateEventRegistrationCommandHandler(
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

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEventRegistrationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateEventRegistrationDtoValidator(_userRepository, _eventSessionRepository, _approvalStatusRepository, _eventRegistrationRepository);
            var validationResult = await validator.ValidateAsync(request.EventRegistrationDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event Registration failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var eventRegistration = _mapper.Map<EventRegistration>(request.EventRegistrationDto);
            eventRegistration = await _eventRegistrationRepository.Create(eventRegistration);

            response.Success = true;
            response.Id = eventRegistration.Id;
            response.Message = "Event Registration created successfully.";

            return response;
        }
    }
}
