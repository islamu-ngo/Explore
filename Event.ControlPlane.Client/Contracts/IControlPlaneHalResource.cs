// ABOUTME: Defines the minimal HAL-resource contract shared by control-plane client models.
// ABOUTME: Allows components to gate affordances on server-provided links without knowing transport DTOs.

namespace Event.ControlPlane.Client.Contracts;

public interface IControlPlaneHalResource
{
    IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; }
}
