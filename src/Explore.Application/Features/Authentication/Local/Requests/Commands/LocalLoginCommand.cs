// ABOUTME: MediatR command for validating local Identity credentials and issuing a platform session.
// ABOUTME: Carries the immutable credential request through the application authentication boundary.

using Explore.Application.Features.Authentication.Local.Models;
using MediatR;

namespace Explore.Application.Features.Authentication.Local.Requests.Commands;

public sealed record LocalLoginCommand(LocalAuthRequestDto Request) : IRequest<LocalAuthResponseDto>;
