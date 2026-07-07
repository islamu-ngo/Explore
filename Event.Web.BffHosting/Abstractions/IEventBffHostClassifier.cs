// ABOUTME: Classifies browser-BFF request hosts against static host configuration.
// ABOUTME: Lets public hosts recognize dedicated admin hosts without referencing app layers.

namespace Event.Web.BffHosting.Abstractions;

public interface IEventBffHostClassifier
{
    bool IsAdminHost(HttpContext httpContext);

    bool IsAdminHost(string? host);
}
