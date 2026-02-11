using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Organizations.Handlers.Queries;

public class GetOrganizationListRequestHandler : IRequestHandler<GetOrganizationListRequest, PaginatedResult<OrganizationListDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetOrganizationListRequestHandler> _logger;

    public GetOrganizationListRequestHandler(
        IOrganizationRepository organizationRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetOrganizationListRequestHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<PaginatedResult<OrganizationListDto>> Handle(GetOrganizationListRequest request, CancellationToken cancellationToken)
    {
        // Get organizations with ApprovalStatus for admin purposes
        var (organizations, totalCount) = await _organizationRepository.GetOrganizationsWithDetailsPaged(request.PageNumber, request.PageSize);
        var organizationDtos = _mapper.Map<List<OrganizationListDto>>(organizations);

        // Resolve presigned URLs for profile pictures
        foreach (var dto in organizationDtos)
        {
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return PaginatedResult<OrganizationListDto>.Create(organizationDtos, totalCount, request.PageNumber, request.PageSize);
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
            // Check if it's already a full URL (legacy data)
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
