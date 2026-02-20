// ABOUTME: Handles retrieval of a paginated list of all Groups with details.
// ABOUTME: Resolves profile picture storage object keys to presigned URLs for each group.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Group;
using Explore.Application.Features.Groups.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Groups.Handlers.Queries;

public class GetGroupListRequestHandler : IRequestHandler<GetGroupListRequest, PaginatedResult<GroupListDto>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetGroupListRequestHandler> _logger;

    public GetGroupListRequestHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetGroupListRequestHandler> logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<PaginatedResult<GroupListDto>> Handle(GetGroupListRequest request, CancellationToken cancellationToken)
    {
        var (groups, totalCount) = await _groupRepository.GetGroupsWithDetailsPaged(request.PageNumber, request.PageSize);
        var groupDtos = _mapper.Map<List<GroupListDto>>(groups);

        foreach (var dto in groupDtos)
        {
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return PaginatedResult<GroupListDto>.Create(groupDtos, totalCount, request.PageNumber, request.PageSize);
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
