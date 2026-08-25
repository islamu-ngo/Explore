// ABOUTME: MediatR query request for fetching a single session-language link by ID.
// ABOUTME: Returns EventSessionLanguageDto.
using Explore.Application.DTOs.EventSessionLanguage;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Queries;

public sealed record GetEventSessionLanguageDetailsRequest : IRequest<EventSessionLanguageDto>
{
    public int Id { get; init; }
}
