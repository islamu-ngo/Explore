// ABOUTME: MediatR command for creating a new AT Protocol record.
// ABOUTME: Carries the CreateAtprotoRecordDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Commands;

[AuthorizeResource(ResourceKinds.AtprotoRecord, AuthorizationActions.Create)]
public class CreateAtprotoRecordCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateAtprotoRecordDto AtprotoRecordDto { get; set; }

    string? ISecureRequest.ResourceId => null;
}
