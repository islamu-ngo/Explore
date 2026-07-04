// ABOUTME: MediatR query for streaming storage object content by stable metadata ID.
// ABOUTME: Avoids exposing provider object keys or filesystem paths as browser-facing identifiers.

using Explore.Application.Authorization;
using Explore.Application.Models.Storage;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.StorageObjects.Download)]
public sealed class GetStorageObjectContentRequest : IRequest<StorageObjectContentResult?>, ISecureRequest
{
    public Guid StorageObjectId { get; set; }

    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => StorageObjectId == Guid.Empty ? null : StorageObjectId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D")
        };
}
