// ABOUTME: Explicit, versioned household-consent payload for private home classification and ownership.
// ABOUTME: Both fields are required so an omitted body can never read as silent agreement.

namespace Explore.Application.DTOs.Location;

/// <summary>
/// Consent supplied by the authenticated actor who is becoming the owner of a private home.
/// </summary>
/// <param name="ConsentAcknowledged">
/// Must be explicitly true. A missing or false value is a refusal, never a default.
/// </param>
/// <param name="ConsentVersion">
/// Identifier of the consent statement the actor was shown, so a later policy change is auditable.
/// </param>
public sealed record PrivateHomeOwnershipConsentDto(
    bool ConsentAcknowledged,
    string ConsentVersion);
