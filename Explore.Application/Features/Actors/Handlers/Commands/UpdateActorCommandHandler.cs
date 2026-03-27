// ABOUTME: Handler for all actor updates using the null-check DTO pattern.
// ABOUTME: Checks which DTO is non-null on the command and applies only that specific update.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.DTOs.Actor.Validators;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Actors.Handlers.Commands;

public class UpdateActorCommandHandler : IRequestHandler<UpdateActorCommand, BaseCommandResponse<Guid>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IActorTypeRepository _actorTypeRepository;
    private readonly IDidCustodyTypeRepository _didCustodyTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public UpdateActorCommandHandler(
        IActorRepository actorRepository,
        IActorTypeRepository actorTypeRepository,
        IDidCustodyTypeRepository didCustodyTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        IMapper mapper,
        HybridCache cache)
    {
        _actorRepository = actorRepository;
        _actorTypeRepository = actorTypeRepository;
        _didCustodyTypeRepository = didCustodyTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateActorCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var actor = await _actorRepository.GetById(request.Id);
        if (actor == null)
        {
            response.Success = false;
            response.Message = "Actor not found.";
            return response;
        }

        if (request.ActorDto is not null)
        {
            var validator = new UpdateActorDtoValidator(
                _actorTypeRepository, _didCustodyTypeRepository,
                _storageObjectRepository, _actorRepository);
            var validationResult = await validator.ValidateAsync(request.ActorDto, cancellationToken);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Actor update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            _mapper.Map(request.ActorDto, actor);
        }

        if (request.AppearanceDto is not null)
        {
            var validator = new UpdateActorAppearanceDtoValidator(_storageObjectRepository);
            var validationResult = await validator.ValidateAsync(request.AppearanceDto, cancellationToken);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Actor appearance update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            ApplyAppearanceUpdate(actor, request.AppearanceDto);
        }

        await _actorRepository.Update(actor);

        response.Success = true;
        response.Id = actor.Id;
        response.Message = "Actor updated successfully.";

        await _cache.RemoveAsync($"actor:detail:{actor.Id}", cancellationToken);

        return response;
    }

    private static void ApplyAppearanceUpdate(Domain.Actor actor, UpdateActorAppearanceDto dto)
    {
        if (dto.BackgroundColor is not null)
            actor.BackgroundColor = dto.BackgroundColor;

        if (dto.BackgroundEffect is not null)
            actor.BackgroundEffect = dto.BackgroundEffect;

        if (dto.BannerColor is not null)
            actor.BannerColor = dto.BannerColor;

        if (dto.BannerPictureId is not null)
            actor.BannerPictureId = dto.BannerPictureId;

        if (dto.BackgroundImageId is not null)
            actor.BackgroundImageId = dto.BackgroundImageId;
    }
}
