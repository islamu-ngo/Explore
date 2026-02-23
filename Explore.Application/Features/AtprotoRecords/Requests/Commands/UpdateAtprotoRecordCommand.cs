using Explore.Application.Authorization;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Commands;

[AuthorizeResource("atproto_record", PermissionAction.Update)]
public class UpdateAtprotoRecordCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateAtprotoRecordDto AtprotoRecordDto { get; set; }

    string? ISecureRequest.ResourceId => AtprotoRecordDto.Id.ToString();
}
