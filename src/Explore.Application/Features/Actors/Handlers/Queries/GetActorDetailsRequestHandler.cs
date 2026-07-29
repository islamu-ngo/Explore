// ABOUTME: Query handler returning a single actor's full details by ID.
// ABOUTME: Maps actor entity to ActorDto via AutoMapper.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorDetailsRequestHandler : IRequestHandler<GetActorDetailsRequest, ActorDto>
{
    private readonly IActorRepository _actorRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetActorDetailsRequestHandler> _logger;

    public GetActorDetailsRequestHandler(
        IActorRepository actorRepository,
        ITenantContext tenantContext,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetActorDetailsRequestHandler> logger)
    {
        _actorRepository = actorRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<ActorDto> Handle(GetActorDetailsRequest request, CancellationToken cancellationToken)
    {
        var actor = await _actorRepository.GetPublicActorProfileAsync(request.Id, cancellationToken);
        var dto = _mapper.Map<ActorDto>(actor);

        // Resolve presigned URL for profile picture
        if (dto != null)
        {
            dto.IsLocallyDiscoverable = await _actorRepository.GetLocallyDiscoverableSubscriptionTargetAsync(
                _tenantContext.TenantId,
                dto.Id,
                cancellationToken) is not null;
            dto.ProfilePictureUri = await ResolveImageUrl(dto.ProfilePictureUri);
        }

        return dto;
    }

    private Task<string?> ResolveImageUrl(string? objectKeyOrUri)
        => StoragePresentationUrlResolver.ResolveImageUrlAsync(
            objectKeyOrUri,
            _objectStorageService,
            _logger,
            "actor detail profile image");
}
