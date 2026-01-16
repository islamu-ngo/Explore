using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor.Validators;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Responses;

namespace Explore.Application.Features.Actors.Handlers.Commands;

public class UpdateActorCommandHandler : IRequestHandler<UpdateActorCommand, BaseCommandResponse<Guid>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IActorTypeRepository _actorTypeRepository;
    private readonly IDidCustodyTypeRepository _didCustodyTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMapper _mapper;

    public UpdateActorCommandHandler(
        IActorRepository actorRepository,
        IActorTypeRepository actorTypeRepository,
        IDidCustodyTypeRepository didCustodyTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        IMapper mapper)
    {
        _actorRepository = actorRepository;
        _actorTypeRepository = actorTypeRepository;
        _didCustodyTypeRepository = didCustodyTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateActorCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateActorDtoValidator(
            _actorTypeRepository,
            _didCustodyTypeRepository,
            _storageObjectRepository,
            _actorRepository);

        var validationResult = await validator.ValidateAsync(request.ActorDto);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Actor update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var actor = await _actorRepository.GetById(request.ActorDto.Id);

        if (actor == null)
        {
            response.Success = false;
            response.Message = "Actor not found.";
            return response;
        }

        _mapper.Map(request.ActorDto, actor);

        await _actorRepository.Update(actor);

        response.Success = true;
        response.Id = actor.Id;
        response.Message = "Actor updated successfully.";

        return response;
    }
}
