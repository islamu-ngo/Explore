// ABOUTME: Represents the opaque capability used to access one guest registration order.
// ABOUTME: Keeps the bearer value inside the typed client service boundary without browser persistence.

namespace Explore.Blazor.Client.Contracts.Services;

public sealed record GuestRegistrationOrderCapability
{
    internal GuestRegistrationOrderCapability(string value) => Value = value;

    internal string Value { get; }
}
