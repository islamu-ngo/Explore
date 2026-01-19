using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Organizations.Handlers.Queries;

public class GetOrganizationDetailsRequestHandler : IRequestHandler<GetOrganizationDetailsRequest, OrganizationDto>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetOrganizationDetailsRequestHandler> _logger;

    public GetOrganizationDetailsRequestHandler(
        IOrganizationRepository organizationRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetOrganizationDetailsRequestHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<OrganizationDto> Handle(GetOrganizationDetailsRequest request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetOrganizationWithDetails(request.Id);
        var dto = _mapper.Map<OrganizationDto>(organization);

        // Resolve presigned URL for profile picture
        if (dto != null)
        {
            dto.ActorProfilePictureUri = ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return dto;
    }

    /// <summary>
    /// Resolves an image object key to a presigned URL for viewing.
    /// </summary>
    private string? ResolveImageUrl(string? objectKeyOrUri)
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
                    return _objectStorageService.GeneratePresignedDownloadUrl(objectKey, 60);
                }
                return objectKeyOrUri;
            }

            // It's an object key - generate presigned URL
            return _objectStorageService.GeneratePresignedDownloadUrl(objectKeyOrUri, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for object key: {ObjectKey}", objectKeyOrUri);
            return null;
        }
    }
}
