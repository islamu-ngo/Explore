// ABOUTME: MediatR query for streaming storage object content by stable metadata ID.
// ABOUTME: Avoids exposing provider object keys or filesystem paths as browser-facing identifiers.

using Explore.Application.Authorization;
using Explore.Application.Models.Storage;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.StorageObjects.Download)]
public sealed record GetStorageObjectContentRequest : IRequest<StorageObjectContentResult?>, ISecureRequest
{
    public Guid StorageObjectId { get; init; }

    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => StorageObjectId == Guid.Empty ? null : StorageObjectId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new StorageObjectCollectionAuthorizationFacts(TenantId);
}
