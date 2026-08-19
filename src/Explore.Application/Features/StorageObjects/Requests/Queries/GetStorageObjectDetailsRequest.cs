// ABOUTME: MediatR query request for fetching a single storage object by ID.
// ABOUTME: Returns StorageObjectDto.
using Explore.Application.Authorization;
using Explore.Application.DTOs.StorageObject;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.StorageObjects.View)]
public class GetStorageObjectDetailsRequest : IRequest<StorageObjectDto?>, ISecureRequest
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => Id == Guid.Empty ? null : Id.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new StorageObjectCollectionAuthorizationFacts(TenantId);
}
