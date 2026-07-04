// ABOUTME: Represents a HAL link emitted by control-plane API adapters.
// ABOUTME: Keeps shared UI affordances tied to server-provided link relations and methods.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneHalLink(
    string Href,
    string? Method = null,
    string? Title = null,
    bool? Templated = null);
