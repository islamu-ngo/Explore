// ABOUTME: Maps normalized registration-form status, field-type, and organizer-visibility lookups.
// ABOUTME: Reuses the shared provider-neutral lookup contract and runtime seeding model.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationFormStatusConfiguration : LookupConfiguration<RegistrationFormStatus>
{
    protected override string TableName => "registration_form_statuses";
}

public sealed class RegistrationFieldTypeConfiguration : LookupConfiguration<RegistrationFieldType>
{
    protected override string TableName => "registration_field_types";
}

public sealed class RegistrationOrganizerVisibilityConfiguration : LookupConfiguration<RegistrationOrganizerVisibility>
{
    protected override string TableName => "registration_organizer_visibilities";
}
