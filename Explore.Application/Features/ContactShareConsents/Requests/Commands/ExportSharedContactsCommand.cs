// ABOUTME: MediatR command for exporting shared contacts as CSV or TSV.
// ABOUTME: Authorised via Cerbos — requires ExportSharedContacts permission on the resource.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ContactShareConsent;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ContactShareConsents.Requests.Commands;

[AuthorizeResource("event_contact_share_consent", AuthorizationActions.ExportSharedContacts)]
public class ExportSharedContactsCommand : IRequest<BaseCommandResponse<SharedContactExportResultDto>>, ISecureRequest
{
    public Guid RecipientActorId { get; set; }
    public Guid? EventId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExportedByUserId { get; set; }

    /// <summary>
    /// Export format: "csv" or "tsv".
    /// </summary>
    public string Format { get; set; } = "csv";

    string? ISecureRequest.ResourceId => RecipientActorId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        new Dictionary<string, object>
        {
            ["organizationId"] = RecipientActorId
        };
}
