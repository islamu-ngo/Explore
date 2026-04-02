// ABOUTME: MediatR query for organisation members to view shared contacts with pagination.
// ABOUTME: Authorised via Cerbos — requires ViewSharedContacts permission on the event_contact_share_consent resource.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ContactShareConsent;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ContactShareConsents.Requests.Queries;

[AuthorizeResource("event_contact_share_consent", AuthorizationActions.ViewSharedContacts)]
public class GetOrganizationSharedContactsQuery : IRequest<PaginatedResult<SharedContactDto>>, ISecureRequest
{
    public Guid RecipientActorId { get; set; }
    public Guid? EventId { get; set; }
    public string? EmailSearch { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => RecipientActorId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        new Dictionary<string, object>
        {
            ["organizationId"] = RecipientActorId
        };
}
