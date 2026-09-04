// ABOUTME: Immutable credential and profile request for local Identity registration.
// ABOUTME: Keeps password input at the application boundary while Identity owns password persistence.

namespace Explore.Application.Features.Authentication.Local.Models;

public sealed record LocalRegistrationRequestDto(
    string Email,
    string Password,
    string FirstName,
    string LastName);
