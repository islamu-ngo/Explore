// ABOUTME: Query handler returning full organization details by ID or slug.
// ABOUTME: Maps Organization entity to OrganizationDto with members.
using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Organizations.Handlers.Queries;

public class GetOrganizationDetailsRequestHandler : IRequestHandler<GetOrganizationDetailsRequest, OrganizationDto?>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetOrganizationDetailsRequestHandler> _logger;
    private readonly HybridCache _cache;

    public GetOrganizationDetailsRequestHandler(
        IOrganizationRepository organizationRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetOrganizationDetailsRequestHandler> logger,
        HybridCache cache)
    {
        _organizationRepository = organizationRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
        _cache = cache;
    }

    public async Task<OrganizationDto?> Handle(GetOrganizationDetailsRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"organization:detail:{request.Id}";
        var dto = await _cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                var organization = await _organizationRepository.GetOrganizationWithDetails(request.Id)
                    ?? await _organizationRepository.GetOrganizationWithDetailsByActorId(request.Id);

                return organization is null ? null : _mapper.Map<OrganizationDto>(organization);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        // Resolve presigned URL for profile picture
        if (dto != null)
        {
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return dto;
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
