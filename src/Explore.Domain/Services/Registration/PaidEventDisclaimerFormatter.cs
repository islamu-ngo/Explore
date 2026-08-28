// ABOUTME: Formats the canonical white-label directory disclaimer shown for paid events.
// ABOUTME: Keeps one exact legal statement across public discovery and checkout acceptance surfaces.

namespace Explore.Domain.Services.Registration;

public static class PaidEventDisclaimerFormatter
{
    public const string DefaultBrandDisplayName = "ISLAMU";

    public static string Format(string? brandDisplayName)
    {
        string brand = string.IsNullOrWhiteSpace(brandDisplayName)
            ? DefaultBrandDisplayName
            : brandDisplayName.Trim();

        return $"{brand} provides an event discovery and management directory only. " +
               $"{brand} does not process ticket sales or act as event organizer. " +
               "Any financial transaction or contract is strictly between the attendee and the external organizer.";
    }
}
