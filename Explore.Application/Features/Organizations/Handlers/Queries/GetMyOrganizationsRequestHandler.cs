// ABOUTME: Query handler returning organizations the current user is a member of.
// ABOUTME: Filters by user ID, maps to OrganizationListDto.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Organizations.Handlers.Queries;

public class GetMyOrganizationsRequestHandler : IRequestHandler<GetMyOrganizationsRequest, PaginatedResult<OrganizationListDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetMyOrganizationsRequestHandler> _logger;

    public GetMyOrganizationsRequestHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetMyOrganizationsRequestHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<PaginatedResult<OrganizationListDto>> Handle(GetMyOrganizationsRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out Guid userGuid))
        {
            return PaginatedResult<OrganizationListDto>.Create(new List<OrganizationListDto>(), 0, request.PageNumber, request.PageSize);
        }

        // Get paginated organizations for the user
        var (organizations, totalCount) = await _organizationRepository.GetMyOrganizationsPaged(userGuid, request.PageNumber, request.PageSize);

        // Get memberships to add user role info
        var memberships = await _organizationMemberRepository.GetMembershipsByUser(userGuid);
        var membershipDict = memberships.ToDictionary(m => m.OrganizationId, m => m.RoleId);

        // Map OrganizationMember entities to OrganizationListDto
        var dtos = new List<OrganizationListDto>();
        foreach (var org in organizations)
        {
            var dto = _mapper.Map<OrganizationListDto>(org);
            if (membershipDict.TryGetValue(org.Id, out var roleId))
            {
                dto.CurrentUserRole = (RoleEnum)roleId;
            }
            // Resolve presigned URL for profile picture
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
            dtos.Add(dto);
        }

        return PaginatedResult<OrganizationListDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);
    }

    /// <summary>
    /// Resolves an image object key to a presigned URL for viewing.
    /// </summary>
    private async Task<string?> ResolveImageUrl(string? objectKeyOrUri)
    {
        if (string.IsNullOrEmpty(objectKeyOrUri))
            return null;

        try
        {
            // Check if it's a relative path (local API endpoint)
            if (objectKeyOrUri.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return objectKeyOrUri;
            }

            // Check if it's already a full URL (legacy data or absolute local API path)
            if (objectKeyOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                objectKeyOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(objectKeyOrUri, UriKind.Absolute, out var uri))
                {
                    // If it is a local API endpoint, return it as-is
                    if (uri.AbsolutePath.StartsWith("/api/storageobject/", StringComparison.OrdinalIgnoreCase))
                    {
                        return objectKeyOrUri;
                    }

                    var objectKey = uri.AbsolutePath.TrimStart('/');
                    return await _objectStorageService.GeneratePresignedDownloadUrl(objectKey, 60);
                }
                return objectKeyOrUri;
            }

            // It's an object key - generate presigned URL
            return await _objectStorageService.GeneratePresignedDownloadUrl(objectKeyOrUri, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for object key: {ObjectKey}", objectKeyOrUri);
            return null;
        }
    }
}
