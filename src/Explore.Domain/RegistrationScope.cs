// ABOUTME: Lookup describing the three registration-intent scopes (Event, Day, SessionSelection) a user can register under.
// ABOUTME: Referenced by EventRegistrationIntent.RegistrationScopeId and enforced against EventRegistrationPolicy on the parent Event.

namespace Explore.Domain;

public class RegistrationScope
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
