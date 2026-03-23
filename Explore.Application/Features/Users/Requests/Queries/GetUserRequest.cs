// ABOUTME: MediatR query request for fetching a user profile by ID.
// ABOUTME: Returns UserDto.
using System;
using Explore.Application.DTOs.User;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

public class GetUserRequest : IRequest<UserDto>
{
    public Guid UserId { get; set; }
}
