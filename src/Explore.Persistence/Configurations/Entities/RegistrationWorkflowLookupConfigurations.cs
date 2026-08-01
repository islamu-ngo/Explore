// ABOUTME: Maps the four normalized Task 7.1 registration-workflow lookup families.
// ABOUTME: Reuses the provider-neutral lookup contract for stable integer IDs and bounded metadata.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationRequirementCriticalityConfiguration : LookupConfiguration<RegistrationRequirementCriticality>
{
    protected override string TableName => "registration_requirement_criticalities";
}

public sealed class RegistrationRequirementCompletionEffectConfiguration : LookupConfiguration<RegistrationRequirementCompletionEffect>
{
    protected override string TableName => "registration_requirement_completion_effects";
}

public sealed class RegistrationAnswerSyncModeConfiguration : LookupConfiguration<RegistrationAnswerSyncMode>
{
    protected override string TableName => "registration_answer_sync_modes";
}

public sealed class RegistrationRequirementSubjectTypeConfiguration : LookupConfiguration<RegistrationRequirementSubjectType>
{
    protected override string TableName => "registration_requirement_subject_types";
}
