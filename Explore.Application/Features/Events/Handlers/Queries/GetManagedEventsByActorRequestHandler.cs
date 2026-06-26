// ABOUTME: Query handler for authenticated actor-profile event management lists.
// ABOUTME: Filters actor-owned event DTOs through event view-management authorization before returning HAL-ready results.

using AutoMapper;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Events.Handlers.Queries;

public class GetManagedEventsByActorRequestHandler : IRequestHandler<GetManagedEventsByActorRequest, PaginatedResult<EventListDto>>
{
    private const int MaxPageSize = 100;

    private readonly IEventRepository _eventRepository;
    private readonly IAuthorizationProvider _authorizationProvider;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetManagedEventsByActorRequestHandler> _logger;

    public GetManagedEventsByActorRequestHandler(
        IEventRepository eventRepository,
        IAuthorizationProvider authorizationProvider,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetManagedEventsByActorRequestHandler> logger)
    {
        _eventRepository = eventRepository;
        _authorizationProvider = authorizationProvider;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<PaginatedResult<EventListDto>> Handle(GetManagedEventsByActorRequest request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, MaxPageSize);

        if (request.ActorId == Guid.Empty)
        {
            return PaginatedResult<EventListDto>.Create([], 0, pageNumber, pageSize);
        }

        var events = await _eventRepository.GetEventsByActorWithDetails(request.ActorId, cancellationToken);
        var eventDtos = _mapper.Map<List<EventListDto>>(events);
        var authorizedEvents = await FilterViewManagementAuthorizedAsync(eventDtos, cancellationToken);

        var pageItems = authorizedEvents
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var dto in pageItems)
        {
            dto.FeaturedImageUri = await ResolveImageUrl(dto.FeaturedImageUri);
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return PaginatedResult<EventListDto>.Create(pageItems, authorizedEvents.Count, pageNumber, pageSize);
    }

    private async Task<List<EventListDto>> FilterViewManagementAuthorizedAsync(
        IReadOnlyList<EventListDto> eventDtos,
        CancellationToken cancellationToken)
    {
        if (eventDtos.Count == 0)
            return [];

        var descriptor = ResourceDescriptors.EventList;
        var checks = eventDtos
            .Select(dto => new AuthorizationCheck(
                descriptor.Kind,
                descriptor.GetResourceId(dto),
                AuthorizationActions.Events.ViewManagement,
                descriptor.GetResourceAttributes(dto),
                descriptor.GetScope(dto)))
            .ToList();

        IReadOnlyList<bool> decisions;
        try
        {
            decisions = await _authorizationProvider.IsAllowedBatchAsync(checks, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to authorize managed event list for actor profile; denying all management-only events.");
            return [];
        }

        var authorized = new List<EventListDto>(eventDtos.Count);
        for (var i = 0; i < eventDtos.Count; i++)
        {
            if (i < decisions.Count && decisions[i])
            {
                authorized.Add(eventDtos[i]);
            }
        }

        return authorized;
    }

    private async Task<string?> ResolveImageUrl(string? objectKeyOrUri)
    {
        if (string.IsNullOrEmpty(objectKeyOrUri))
            return null;

        try
        {
            if (objectKeyOrUri.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return objectKeyOrUri;
            }

            if (objectKeyOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                objectKeyOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(objectKeyOrUri, UriKind.Absolute, out var uri))
                {
                    if (uri.AbsolutePath.StartsWith("/api/storageobject/", StringComparison.OrdinalIgnoreCase))
                    {
                        return objectKeyOrUri;
                    }

                    var objectKey = uri.AbsolutePath.TrimStart('/');
                    return await _objectStorageService.GeneratePresignedDownloadUrl(objectKey, 60);
                }

                return objectKeyOrUri;
            }

            return await _objectStorageService.GeneratePresignedDownloadUrl(objectKeyOrUri, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for managed actor event list image.");
            return null;
        }
    }
}
