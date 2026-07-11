// ABOUTME: Handler for grouped actor PATCH updates using the sub-DTO pattern.
// ABOUTME: Validates groups, enforces concurrency, applies explicit mappings, and invalidates detail cache.

using System.Linq;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.DTOs.Actor.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Actors.Handlers.Commands;

public class UpdateActorCommandHandler : IRequestHandler<UpdateActorCommand, BaseCommandResponse<Guid>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IActorTypeRepository _actorTypeRepository;
    private readonly IDidCustodyTypeRepository _didCustodyTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IAuthorizationProvider _authorizationProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;

    public UpdateActorCommandHandler(
        IActorRepository actorRepository,
        IActorTypeRepository actorTypeRepository,
        IDidCustodyTypeRepository didCustodyTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        IAuthorizationProvider authorizationProvider,
        IUnitOfWork unitOfWork,
        HybridCache cache)
    {
        _actorRepository = actorRepository;
        _actorTypeRepository = actorTypeRepository;
        _didCustodyTypeRepository = didCustodyTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _authorizationProvider = authorizationProvider;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateActorCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateActorDtoValidator(
            request.ActorId,
            _actorTypeRepository,
            _didCustodyTypeRepository,
            _storageObjectRepository,
            _actorRepository);
        var validationResult = await validator.ValidateAsync(request.UpdateActorDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Actor update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var transactionResponse = await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var actor = await _actorRepository.GetById(request.ActorId);
            if (actor == null)
            {
                response.Success = false;
                response.Message = "Actor not found.";
                return response;
            }

            await EnsureCanUpdatePresentGroupsAsync(actor, request.UpdateActorDto, token);

            if (actor.ConcurrencyStamp != request.ExpectedConcurrencyStamp)
            {
                throw new ConcurrencyConflictException(
                    ConcurrencyConflictException.ConcurrentUpdate,
                    "The actor was modified by another request. Reload and retry.",
                    nameof(Actor),
                    actor.Id.ToString());
            }

            await ApplyProfileImageAsync(actor, request.UpdateActorDto.ProfileImage);
            await ApplyAppearanceAsync(actor, request.UpdateActorDto.Appearance);
            ApplyProfile(actor, request.UpdateActorDto.Profile);
            ApplyFederationIdentifiers(actor, request.UpdateActorDto.FederationIdentifiers);
            ApplyFederationMetadata(actor, request.UpdateActorDto.FederationMetadata);

            await _actorRepository.Update(actor);

            response.Success = true;
            response.Id = actor.Id;
            response.Message = "Actor updated successfully.";
            return response;
        }, cancellationToken);

        if (transactionResponse.Success)
        {
            await _cache.RemoveAsync($"actor:detail:{transactionResponse.Id}", cancellationToken);
        }

        return transactionResponse;
    }

    private async Task EnsureCanUpdatePresentGroupsAsync(Actor actor, UpdateActorDto dto, CancellationToken cancellationToken)
    {
        var checks = BuildPresentGroupAuthorizationChecks(actor, dto);
        if (checks.Count == 0)
        {
            return;
        }

        var decisions = await _authorizationProvider.IsAllowedBatchAsync(checks, cancellationToken);
        if (decisions.Any(isAllowed => !isAllowed))
        {
            throw new AuthorizationException(ResourceKinds.Actor, AuthorizationActions.Update);
        }
    }

    private static IReadOnlyList<AuthorizationCheck> BuildPresentGroupAuthorizationChecks(Actor actor, UpdateActorDto dto)
    {
        var checks = new List<AuthorizationCheck>();
        AddGroupCheck(checks, actor, dto.Profile, "profile");
        AddGroupCheck(checks, actor, dto.ProfileImage, "profileImage");
        AddGroupCheck(checks, actor, dto.Appearance, "appearance");
        AddGroupCheck(checks, actor, dto.FederationIdentifiers, "federationIdentifiers");
        AddGroupCheck(checks, actor, dto.FederationMetadata, "federationMetadata");
        return checks;
    }

    private static void AddGroupCheck<TGroup>(List<AuthorizationCheck> checks, Actor actor, TGroup? group, string groupName)
        where TGroup : class
    {
        if (group is null)
        {
            return;
        }

        checks.Add(new AuthorizationCheck(
            ResourceKinds.Actor,
            actor.Id.ToString(),
            AuthorizationActions.Update,
            ActorGroupAuthorizationAttributes(actor, groupName),
            new AuthorizationScope(actor.TenantId.ToString())));
    }

    private static IReadOnlyDictionary<string, object> ActorGroupAuthorizationAttributes(Actor actor, string groupName) =>
        new Dictionary<string, object>
        {
            ["actorId"] = actor.Id.ToString(),
            ["tenantId"] = actor.TenantId.ToString(),
            ["userId"] = actor.UserId?.ToString() ?? string.Empty,
            ["organizationId"] = actor.OrganizationId?.ToString() ?? string.Empty,
            ["groupId"] = actor.GroupId?.ToString() ?? string.Empty,
            ["actorUpdateGroup"] = groupName
        };

    private static void ApplyProfile(Actor actor, UpdateActorProfileDto? dto)
    {
        if (dto is null)
        {
            return;
        }

        if (dto.ActorTypeId.HasValue)
        {
            actor.ActorTypeId = dto.ActorTypeId.Value;
        }

        if (dto.DisplayName is not null)
        {
            actor.DisplayName = dto.DisplayName;
        }
    }

    private async Task ApplyProfileImageAsync(Actor actor, UpdateActorProfileImageDto? dto)
    {
        if (dto is null || !dto.ProfilePictureId.HasValue)
        {
            return;
        }

        actor.ProfilePictureId = dto.ProfilePictureId.Value;
        await LinkStorageObjectToActorAsync(actor.Id, dto.ProfilePictureId);
    }

    private async Task ApplyAppearanceAsync(Actor actor, UpdateActorAppearanceDto? dto)
    {
        if (dto is null)
        {
            return;
        }

        ApplyOptional(dto.BackgroundColor, value => actor.BackgroundColor = value);
        ApplyOptional(dto.BackgroundEffect, value => actor.BackgroundEffect = value);
        ApplyOptional(dto.BannerColor, value => actor.BannerColor = value);

        if (dto.BannerPictureId.HasValue)
        {
            actor.BannerPictureId = dto.BannerPictureId.Value;
            await LinkStorageObjectToActorAsync(actor.Id, dto.BannerPictureId);
        }

        if (dto.BackgroundImageId.HasValue)
        {
            actor.BackgroundImageId = dto.BackgroundImageId.Value;
            await LinkStorageObjectToActorAsync(actor.Id, dto.BackgroundImageId);
        }
    }

    private static void ApplyFederationIdentifiers(Actor actor, UpdateActorFederationIdentifiersDto? dto)
    {
        if (dto is null)
        {
            return;
        }

        ApplyOptional(dto.Did, value => actor.Did = value);
        ApplyOptional(dto.Handle, value => actor.Handle = value);
        ApplyOptional(dto.DidCustodyTypeId, value => actor.DidCustodyTypeId = value);
    }

    private static void ApplyFederationMetadata(Actor actor, UpdateActorFederationMetadataDto? dto)
    {
        if (dto is null)
        {
            return;
        }

        ApplyOptional(dto.PdsHost, value => actor.PdsHost = value);
        ApplyOptional(dto.Description, value => actor.Description = value);
        ApplyOptional(dto.IndexedAt, value => actor.IndexedAt = value);
        ApplyOptional(dto.ProfilePictureCid, value => actor.ProfilePictureCid = value);
        ApplyOptional(dto.ProfilePictureUri, value => actor.ProfilePictureUri = value);
    }

    private async Task LinkStorageObjectToActorAsync(Guid actorId, OptionalUpdate<Guid?> update)
    {
        if (!update.HasValue || update.Value is not { } storageObjectId)
        {
            return;
        }

        var storageObject = await _storageObjectRepository.GetById(storageObjectId);
        if (storageObject != null)
        {
            storageObject.ActorId = actorId;
        }
    }

    private static void ApplyOptional<T>(OptionalUpdate<T> update, Action<T?> apply)
    {
        if (update.HasValue)
        {
            apply(update.Value);
        }
    }
}
