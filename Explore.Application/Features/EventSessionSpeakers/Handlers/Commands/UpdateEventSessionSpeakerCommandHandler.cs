// ABOUTME: Handler for updating a session-speaker link with validation.
// ABOUTME: Validates input, fetches entity, applies field updates.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionSpeaker.Validators;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Handlers.Commands;

public class UpdateEventSessionSpeakerCommandHandler : IRequestHandler<UpdateEventSessionSpeakerCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionSpeakerRepository _speakerRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IMapper _mapper;

    public UpdateEventSessionSpeakerCommandHandler(
        IEventSessionSpeakerRepository speakerRepository,
        IActorRepository actorRepository,
        IEventSessionRepository eventSessionRepository,
        IMapper mapper)
    {
        _speakerRepository = speakerRepository;
        _actorRepository = actorRepository;
        _eventSessionRepository = eventSessionRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionSpeakerCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionSpeakerDtoValidator(_actorRepository, _eventSessionRepository);
        var validationResult = await validator.ValidateAsync(request.SpeakerDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Speaker assignment update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var speaker = await _speakerRepository.GetById(request.SpeakerDto.Id);

        if (speaker == null)
        {
            response.Success = false;
            response.Message = "Speaker assignment not found.";
            return response;
        }

        _mapper.Map(request.SpeakerDto, speaker);

        await _speakerRepository.Update(speaker);

        response.Success = true;
        response.Id = speaker.Id;
        response.Message = "Speaker assignment updated successfully.";

        return response;
    }
}
