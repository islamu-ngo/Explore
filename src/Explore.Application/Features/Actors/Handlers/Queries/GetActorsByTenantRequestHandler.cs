// ABOUTME: Query handler returning all actors belonging to a specific tenant.
// ABOUTME: Used for tenant-scoped actor resolution.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorsByTenantRequestHandler : IRequestHandler<GetActorsByTenantRequest, List<ActorListDto>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetActorsByTenantRequestHandler> _logger;

    public GetActorsByTenantRequestHandler(
        IActorRepository actorRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetActorsByTenantRequestHandler> logger)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<List<ActorListDto>> Handle(GetActorsByTenantRequest request, CancellationToken cancellationToken)
    {
        var actors = await _actorRepository.GetActorsByTenant(request.TenantId);
        var dtos = _mapper.Map<List<ActorListDto>>(actors);

        // Resolve presigned URLs for profile pictures
        foreach (var dto in dtos)
        {
            dto.ProfilePictureUri = await ResolveImageUrl(dto.ProfilePictureUri);
        }

        return dtos;
    }

    private Task<string?> ResolveImageUrl(string? objectKeyOrUri)
        => StoragePresentationUrlResolver.ResolveImageUrlAsync(
            objectKeyOrUri,
            _objectStorageService,
            _logger,
            "tenant actor profile image");
}
