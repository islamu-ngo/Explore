// ABOUTME: MediatR query request for fetching a single madhab by ID.
// ABOUTME: Returns MadhabDto.
using Explore.Application.DTOs.Madhab;
using MediatR;

namespace Explore.Application.Features.Madhabs.Requests.Queries;

public sealed record GetMadhabDetailsRequest(int Id = default) : IRequest<MadhabDto>;
