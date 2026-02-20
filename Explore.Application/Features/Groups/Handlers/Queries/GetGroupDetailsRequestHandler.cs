// ABOUTME: Handles retrieval of a single Group with full details, cached with HybridCache.
// ABOUTME: Resolves profile picture storage object keys to presigned URLs.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Group;
using Explore.Application.Features.Groups.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Groups.Handlers.Queries;

public class GetGroupDetailsRequestHandler : IRequestHandler<GetGroupDetailsRequest, GroupDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetGroupDetailsRequestHandler> _logger;
    private readonly HybridCache _cache;

    public GetGroupDetailsRequestHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetGroupDetailsRequestHandler> logger,
        HybridCache cache)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
        _cache = cache;
    }

    public async Task<GroupDto> Handle(GetGroupDetailsRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"group:detail:{request.Id}";
        var dto = await _cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                var group = await _groupRepository.GetGroupWithDetails(request.Id);
                return _mapper.Map<GroupDto>(group);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        if (dto != null)
        {
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return dto;
    }

    private async Task<string?> ResolveImageUrl(string? objectKeyOrUri)
    {
        if (string.IsNullOrEmpty(objectKeyOrUri))
            return null;

        try
        {
            if (objectKeyOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                objectKeyOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(objectKeyOrUri, UriKind.Absolute, out var uri))
                {
                    var objectKey = uri.AbsolutePath.TrimStart('/');
                    return await _objectStorageService.GeneratePresignedDownloadUrl(objectKey, 60);
                }
                return objectKeyOrUri;
            }

            return await _objectStorageService.GeneratePresignedDownloadUrl(objectKeyOrUri, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for object key: {ObjectKey}", objectKeyOrUri);
            return null;
        }
    }
}
