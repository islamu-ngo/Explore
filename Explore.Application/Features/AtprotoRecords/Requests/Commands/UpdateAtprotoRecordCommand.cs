// ABOUTME: MediatR command for updating an existing AT Protocol record.
// ABOUTME: Carries the UpdateAtprotoRecordDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Commands;

[AuthorizeResource("atproto_record", AuthorizationActions.Update)]
public class UpdateAtprotoRecordCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateAtprotoRecordDto AtprotoRecordDto { get; set; }

    string? ISecureRequest.ResourceId => AtprotoRecordDto.Id.ToString();
}
