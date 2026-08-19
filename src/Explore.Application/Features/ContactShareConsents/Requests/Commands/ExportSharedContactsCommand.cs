// ABOUTME: MediatR command for exporting shared contacts as CSV or TSV.
// ABOUTME: Authorised via Cerbos — requires ExportSharedContacts permission on the resource.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ContactShareConsent;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.ContactShareConsents.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventContactShareConsent, AuthorizationActions.ExportSharedContacts)]
public class ExportSharedContactsCommand : IRequest<BaseCommandResponse<SharedContactExportResultDto>>, ISecureRequest
{
    public Guid RecipientActorId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? EventId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExportedByUserId { get; set; }
    public string PurposeCode { get; set; } = "ORGANIZER_CONTACT_SHARE_EXPORT";
    public string ConsentPurposeCode { get; set; } = ConsentPurposeCodes.OrganizerFutureCommunications;
    public string PolicyVersion { get; set; } = "phase13.v1";

    /// <summary>
    /// Export format: "csv" or "tsv".
    /// </summary>
    public string Format { get; set; } = "csv";

    string? ISecureRequest.ResourceId => OrganizationId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new ContactShareAuthorizationFacts(TenantId, OrganizationId);
}
