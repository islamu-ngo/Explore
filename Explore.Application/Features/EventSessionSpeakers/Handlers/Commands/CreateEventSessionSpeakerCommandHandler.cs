using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionSpeaker.Validators;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Handlers.Commands;

public class CreateEventSessionSpeakerCommandHandler : IRequestHandler<CreateEventSessionSpeakerCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionSpeakerRepository _speakerRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateEventSessionSpeakerCommandHandler(
        IEventSessionSpeakerRepository speakerRepository,
        IActorRepository actorRepository,
        IEventSessionRepository eventSessionRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _speakerRepository = speakerRepository;
        _actorRepository = actorRepository;
        _eventSessionRepository = eventSessionRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventSessionSpeakerCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventSessionSpeakerDtoValidator(_actorRepository, _eventSessionRepository);
        var validationResult = await validator.ValidateAsync(request.SpeakerDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Speaker assignment creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var speaker = _mapper.Map<EventSessionSpeaker>(request.SpeakerDto);

        // Set TenantId from the request context
        speaker.TenantId = _tenantContext.TenantId;

        speaker = await _speakerRepository.Create(speaker);

        response.Success = true;
        response.Id = speaker.Id;
        response.Message = "Speaker assigned to session successfully.";

        return response;
    }
}
