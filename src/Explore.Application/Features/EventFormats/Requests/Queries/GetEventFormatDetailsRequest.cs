// ABOUTME: MediatR query request for fetching a single event format by ID.
// ABOUTME: Returns EventFormatDto.
using Explore.Application.DTOs.EventFormat;
using MediatR;

namespace Explore.Application.Features.EventFormats.Requests.Queries;

public class GetEventFormatDetailsRequest : IRequest<EventFormatDto>
{
    public int Id { get; set; }
}
