// ABOUTME: MediatR command for deleting an AT Protocol record by ID.
// ABOUTME: Carries the target record ID.
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Commands;

[AuthorizeResource("atproto_record", PermissionAction.Delete)]
public class DeleteAtprotoRecordCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
