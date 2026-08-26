// ABOUTME: Defines the closed effective modes governing manual address creation.
// ABOUTME: Uses a deny-first zero value so missing or malformed policy remains disabled.

namespace Explore.Domain.Enums;

public enum AddressCreationMode
{
    Disabled = 0,
    AdminOnly = 1,
    OrganizationGoverned = 2,
    OpenWithModeration = 3
}
