// ABOUTME: MediatR query request for fetching a single registration mode by ID.
// ABOUTME: Returns RegistrationModeDto.
using Explore.Application.DTOs.RegistrationMode;
using MediatR;

namespace Explore.Application.Features.RegistrationModes.Requests.Queries;

public sealed record GetRegistrationModeDetailsRequest(int Id = default) : IRequest<RegistrationModeDto>;
