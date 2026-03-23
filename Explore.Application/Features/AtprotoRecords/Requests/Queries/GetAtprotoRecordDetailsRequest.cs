// ABOUTME: MediatR query request for fetching a single AT Protocol record by ID.
// ABOUTME: Returns AtprotoRecordDto.
using Explore.Application.DTOs.AtprotoRecord;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Queries;

public class GetAtprotoRecordDetailsRequest : IRequest<AtprotoRecordDto?>
{
    public Guid Id { get; set; }
}
