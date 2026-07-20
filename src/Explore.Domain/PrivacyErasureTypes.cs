// ABOUTME: Defines closed platform privacy-erasure subject and reason vocabularies.
// ABOUTME: Exposes only executable User erasure values and no free-form instruction channel.

namespace Explore.Domain;

public enum PrivacyErasureSubjectKind
{
    User = 1
}

public enum PrivacyErasureReasonCode
{
    AccountDeletion = 1,
    SubjectErasureRequest = 2,
    PrivacyIncidentRemediation = 3
}
