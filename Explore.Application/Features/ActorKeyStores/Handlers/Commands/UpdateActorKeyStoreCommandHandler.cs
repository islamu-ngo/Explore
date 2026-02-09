using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.DTOs.ActorKeyStore.Validators;
using Explore.Application.Features.ActorKeyStores.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Handlers.Commands;

public class UpdateActorKeyStoreCommandHandler : IRequestHandler<UpdateActorKeyStoreCommand, BaseCommandResponse<Guid>>
{
    private readonly IActorKeyStoreRepository _actorKeyStoreRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateActorKeyStoreDto> _validator;

    public UpdateActorKeyStoreCommandHandler(
        IActorKeyStoreRepository actorKeyStoreRepository,
        IMapper mapper,
        IValidator<UpdateActorKeyStoreDto> validator)
    {
        _actorKeyStoreRepository = actorKeyStoreRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateActorKeyStoreCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validationResult = await _validator.ValidateAsync(request.ActorKeyStoreDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Actor Key Store update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var existingKeyStore = await _actorKeyStoreRepository.GetById(request.ActorKeyStoreDto.Id);
        if (existingKeyStore == null)
        {
            response.Success = false;
            response.Message = "Actor Key Store not found.";
            return response;
        }

        _mapper.Map(request.ActorKeyStoreDto, existingKeyStore);
        await _actorKeyStoreRepository.Update(existingKeyStore);

        response.Success = true;
        response.Id = existingKeyStore.Id;
        response.Message = "Actor Key Store updated successfully.";

        return response;
    }
}
