// ABOUTME: Lookup describing the three legacy registration scopes retained as workflow vocabulary.
// ABOUTME: Enforced against EventRegistrationPolicy on the parent Event without owning registration authority.

namespace Explore.Domain;

public class RegistrationScope
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
