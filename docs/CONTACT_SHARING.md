ABOUTME: Documents implemented contact share consent behavior and privacy boundaries.
ABOUTME: Prevents unsupported claims about permanent deletion, mailing-list removal, or server-to-server sharing.

# Contact Sharing

> **Audience:** Admins | Integrators | Contributors
> **Status:** Implemented
> **Owner:** Product/Admin
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.Application/Features/ContactShareConsents/`, `Explore.API/Controllers/ContactShareConsentController.cs`, `Explore.Application/Services/ContactShareConsentService.cs`, `Explore.Domain/EventContactShareConsent.cs`, `Explore.Persistence/Repositories/EventContactShareConsentRepository.cs`

Contact sharing records a user's explicit consent to share registration contact details with an approved organization for future communications. The implemented behavior is consent storage, user withdrawal, organization read access, and browser-download export; it is not an email fanout or external mailing-list integration.

## Consent Model

`EventContactShareConsent` is scoped by tenant, user, recipient actor, and purpose:

| Field Family | Behavior |
|---|---|
| Recipient | Approved organization actor receiving access. |
| Purpose | `ORGANIZER_FUTURE_COMMUNICATIONS`. |
| Source event / registration intent | Audit context only; not the consent identity boundary. |
| Snapshot data | Email, normalized email, consent text, and UI version are stored as snapshots. |
| Status | Granted or withdrawn; withdrawal sets `WithdrawnAt`. |

Persistence enforces uniqueness for `(TenantId, UserId, RecipientActorId, PurposeCode)`. Do not describe consent as event-scoped; the source event is provenance for when consent was captured.

## Capture Flow

Registration handling calls `IContactShareConsentService.ProcessRegistrationConsent(...)`. The service records consent only when:

1. The registrant opts in.
2. The event publisher resolves to an approved organization.
3. The user has an email snapshot to share.

The purpose is `ORGANIZER_FUTURE_COMMUNICATIONS`. Consent text and UI version are stored with the snapshot, and the service supplies defaults when those values are omitted by the registration flow.

The registration UI displays the consent checkbox and links users to settings where they can review connected apps.

## API Surface

| Action | Endpoint | Boundary |
|---|---|---|
| List my consents | `GET /api/contactshareconsent/my` | Authenticated user. |
| Check recipient consent | `GET /api/contactshareconsent/check/{recipientActorId}` | Authenticated user. |
| Withdraw consent | `POST /api/contactshareconsent/withdraw/{id}` | Authenticated user for their own consent. |
| List organization shared contacts | `GET /api/contactshareconsent/organization/{recipientActorId}` | Organization shared-contact authorization. |
| Export organization shared contacts | `POST /api/contactshareconsent/organization/{recipientActorId}/export` | Organization shared-contact export authorization. |

Organization read and export requests use application resource authorization for the `event_contact_share_consent` resource kind and the `ViewSharedContacts` or `ExportSharedContacts` action.

## Privacy Boundary

- Organization access reads stored contact snapshots for granted consents.
- Export uses stored snapshots and writes an export audit record with row count, format, exporter, and email snapshot details.
- Withdrawal changes future application reads by moving the consent to withdrawn status.
- Withdrawal is not a permanent-delete operation and does not prove removal from external mailing lists.

Do not document legal or privacy guarantees beyond these source-backed behaviors.

## Verified Export Behavior

Exports are generated as browser-download files in CSV or TSV format. The inspected source does not show server-to-server sharing, outbound email fanout, or external marketing-provider synchronization.

## Admin And User Surfaces

| Surface | Source |
|---|---|
| Registration consent checkbox | `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` |
| User connected-app withdrawal | `Explore.Blazor.Client/Pages/User/Components/SettingsConnectedApps.razor` |
| Organization shared contacts list/export | `Explore.Blazor.Client/Pages/Organizations/OrganizationSharedContacts.razor` |

The user settings UI warns that withdrawing consent does not guarantee removal from external mailing lists. Keep that warning unless a future source-backed integration proves stronger behavior.

## Related Documentation

- [ADMIN_GUIDE.md](ADMIN_GUIDE.md) - organization admin workflow context.
- [API.md](API.md) - API conventions and error shape.
- [API_COOKBOOK.md](API_COOKBOOK.md) - integration guidance.
- [SECURITY.md](SECURITY.md) - authorization action catalog.
