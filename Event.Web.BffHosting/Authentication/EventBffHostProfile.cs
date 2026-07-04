// ABOUTME: Defines explicit browser-BFF host profiles used by Event web hosts.
// ABOUTME: Keeps profile selection centralized without coupling to UI or business projects.

namespace Event.Web.BffHosting.Authentication;

public enum EventBffHostProfile
{
    PublicWeb = 1,
    ControlPlane = 2
}
