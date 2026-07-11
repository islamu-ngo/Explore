// ABOUTME: Tenant event-defaults settings payload for typed document storage.
// ABOUTME: Stores non-secret defaults applied by later settings workflows.

namespace Explore.Domain.Settings.Documents.Payloads;

public sealed record EventDefaultsSettings
{
    public bool RequireApproval { get; init; } = true;

    public bool UserSubmissionEnabled { get; init; }
}
