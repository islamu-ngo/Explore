using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.User;
using Explore.Application.Features.Users.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Users.Handlers.Queries;

public class GetUserRequestHandler : IRequestHandler<GetUserRequest, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserRequestHandler> _logger;
    private readonly HybridCache _cache;

    public GetUserRequestHandler(
        IUserRepository userRepository,
        IObjectStorageService objectStorageService,
        IMapper mapper,
        ILogger<GetUserRequestHandler> logger,
        HybridCache cache)
    {
        _userRepository = userRepository;
        _objectStorageService = objectStorageService;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"user:detail:{request.UserId}";

        var userDto = await _cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                var user = await _userRepository.GetUserWithDetails(request.UserId);
                if (user == null)
                {
                    return null;
                }

                var dto = _mapper.Map<UserDto>(user);

                if (user.Actor?.ProfilePicture != null)
                {
                    dto.ProfileImageKey = user.Actor.ProfilePicture.Uri;
                    dto.ProfileImageUri = user.Actor.ProfilePicture.Uri;
                }
                else if (!string.IsNullOrEmpty(user.Actor?.ProfilePictureUri))
                {
                    dto.ProfileImageUri = user.Actor.ProfilePictureUri;
                }

                return dto;
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        if (userDto != null && !string.IsNullOrEmpty(userDto.ProfileImageKey))
        {
            userDto.ProfileImageUri = ResolveImageUrl(userDto.ProfileImageKey);
        }

        return userDto;
    }

    private string? ResolveImageUrl(string? objectKeyOrUri)
    {
        if (string.IsNullOrEmpty(objectKeyOrUri))
        {
            return null;
        }

        try
        {
            // Handle legacy full URLs (https://bucket.endpoint/key)
            if (objectKeyOrUri.StartsWith("http://") || objectKeyOrUri.StartsWith("https://"))
            {
                if (Uri.TryCreate(objectKeyOrUri, UriKind.Absolute, out var uri))
                {
                    // Extract the object key from the URL path
                    var objectKey = uri.AbsolutePath.TrimStart('/');
                    return _objectStorageService.GeneratePresignedDownloadUrl(objectKey, 60);
                }
                _logger.LogWarning("Failed to parse legacy URL: {Uri}", objectKeyOrUri);
                return objectKeyOrUri; // Return as-is if parsing fails
            }

            // New format: just the object key
            return _objectStorageService.GeneratePresignedDownloadUrl(objectKeyOrUri, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for: {ObjectKeyOrUri}", objectKeyOrUri);
            return null;
        }
    }
}
