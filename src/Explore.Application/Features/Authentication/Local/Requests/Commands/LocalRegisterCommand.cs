// ABOUTME: MediatR command for creating local Identity credentials and issuing a platform session.
// ABOUTME: Carries the immutable registration request through the application authentication boundary.

using Explore.Application.Features.Authentication.Local.Models;
using MediatR;

namespace Explore.Application.Features.Authentication.Local.Requests.Commands;

public sealed record LocalRegisterCommand(LocalRegistrationRequestDto Request)
    : IRequest<LocalRegistrationResponseDto>;
