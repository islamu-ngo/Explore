// ABOUTME: Stable lookup row for whether ticket holders must provide participant details.
// ABOUTME: Separates participant-data requirements from the later registration workflow model.

namespace Explore.Domain;

public sealed class ParticipantDataCollectionMode
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
