// ABOUTME: EF configuration for participant data collection mode lookup rows.
// ABOUTME: Uses the shared runtime-seeded lookup mapping contract.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class ParticipantDataCollectionModeConfiguration : LookupConfiguration<ParticipantDataCollectionMode>
{
    protected override string TableName => "participant_data_collection_modes";
}
