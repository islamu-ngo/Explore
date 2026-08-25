// ABOUTME: MediatR query request for fetching a user profile by ID.
// ABOUTME: Returns UserDto.
using System;
using Explore.Application.DTOs.User;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

public sealed record GetUserRequest(Guid UserId = default) : IRequest<UserDto>;
