using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.User;
using Explore.Application.Features.Users.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.Users.Handlers.Queries;

public class GetUserRequestHandler : IRequestHandler<GetUserRequest, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserRequestHandler> _logger;

    public GetUserRequestHandler(
        IUserRepository userRepository,
        IObjectStorageService objectStorageService,
        IMapper mapper,
        ILogger<GetUserRequestHandler> logger)
    {
        _userRepository = userRepository;
        _objectStorageService = objectStorageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserWithDetails(request.UserId);

        if (user == null)
        {
            return null;
        }

        var userDto = _mapper.Map<UserDto>(user);

        // Generate presigned URL for profile picture if it exists
        if (user.Actor?.ProfilePicture != null)
        {
            userDto.ProfileImageKey = user.Actor.ProfilePicture.Uri;
            userDto.ProfileImageUri = ResolveImageUrl(user.Actor.ProfilePicture.Uri);
        }
        else if (!string.IsNullOrEmpty(user.Actor?.ProfilePictureUri))
        {
            // Fallback to ATProto profile picture URI if available
            userDto.ProfileImageUri = user.Actor.ProfilePictureUri;
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
