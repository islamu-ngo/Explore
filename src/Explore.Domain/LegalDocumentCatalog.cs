// ABOUTME: Defines the closed legal document taxonomy and its role-owned scope.
// ABOUTME: Prevents imported artifacts or registry growth from granting publication authority.

namespace Explore.Domain;

using System.Collections.Frozen;

public enum LegalDocumentScope
{
    Instance = 1,
    Tenant = 2
}

public enum LegalDocumentOwnerRole
{
    InstanceOperator = 1,
    TenantOperator = 2
}

public enum LegalDocumentAudience
{
    Public = 1,
    RegisteredUsers = 2,
    Administrators = 3,
    Developers = 4,
    EventParticipants = 5
}

public enum LegalDocumentLifecycleState
{
    Draft = 1,
    ReviewRequired = 2,
    Approved = 3,
    Scheduled = 4,
    Published = 5,
    Retired = 6
}

public enum LegalDocumentTemplateSourceKind
{
    ProjectOwned = 1,
    ApprovedFoss = 2
}

public enum LegalDocumentKind
{
    TermsOfService = 1,
    PrivacyNotice = 2,
    CookiePolicy = 3,
    AcceptableUsePolicy = 4,
    CommunityGuidelines = 5,
    ModerationReportingAppealPolicy = 6,
    AccessibilityStatement = 7,
    LegalNotice = 8,
    SecurityDisclosurePolicy = 9,
    RetentionErasurePortabilityNotice = 10,
    SubprocessorNotice = 11,
    OpenSourceAttribution = 12,
    ApiDeveloperTerms = 13,
    FederationNotice = 14,
    PaymentResponsibilities = 15,
    SupportAvailabilityEolMigrationNotice = 16,
    TenantTerms = 17,
    TenantPrivacyNotice = 18,
    TenantCodeOfConduct = 19,
    OrganizerSubmissionTerms = 20,
    EventPublicationModerationPolicy = 21,
    CancellationRefundPolicy = 22,
    RegistrationParticipantPrivacyNotice = 23,
    MediaPhotographyNotice = 24,
    SafeguardingMinorParticipationPolicy = 25,
    VenueAccessibilityPolicy = 26,
    ComplaintCorrectionCopyrightNotice = 27,
    SponsorshipPartnerDisclosure = 28,
    TenantRetentionContactSharingNotice = 29
}

public sealed record LegalDocumentKindDescriptor(
    LegalDocumentKind Kind,
    LegalDocumentScope Scope,
    LegalDocumentOwnerRole OwnerRole);

public static class LegalDocumentKindCatalog
{
    private static readonly FrozenDictionary<LegalDocumentKind, LegalDocumentKindDescriptor>
        KindEntries = Create();

    public static IReadOnlyDictionary<LegalDocumentKind, LegalDocumentKindDescriptor>
        Entries => KindEntries;

    public static LegalDocumentKindDescriptor Get(LegalDocumentKind kind) =>
        KindEntries.TryGetValue(kind, out LegalDocumentKindDescriptor? descriptor)
            ? descriptor
            : throw new ArgumentOutOfRangeException(nameof(kind));

    private static FrozenDictionary<LegalDocumentKind, LegalDocumentKindDescriptor>
        Create() =>
        Enum.GetValues<LegalDocumentKind>()
            .Select(kind =>
            {
                LegalDocumentScope scope = kind <=
                    LegalDocumentKind.SupportAvailabilityEolMigrationNotice
                    ? LegalDocumentScope.Instance
                    : LegalDocumentScope.Tenant;
                LegalDocumentOwnerRole role = scope == LegalDocumentScope.Instance
                    ? LegalDocumentOwnerRole.InstanceOperator
                    : LegalDocumentOwnerRole.TenantOperator;
                return new LegalDocumentKindDescriptor(kind, scope, role);
            })
            .ToFrozenDictionary(descriptor => descriptor.Kind);
}
