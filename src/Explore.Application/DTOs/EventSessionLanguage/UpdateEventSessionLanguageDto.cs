// ABOUTME: Grouped PATCH payload for updating event-session language assignments.
// ABOUTME: Uses nullable groups so clients can update the session or language independently.

namespace Explore.Application.DTOs.EventSessionLanguage;

public class UpdateEventSessionLanguageDto
{
    public UpdateEventSessionLanguageSessionDto? Session { get; set; }
    public UpdateEventSessionLanguageLanguageDto? Language { get; set; }
}

public class UpdateEventSessionLanguageSessionDto
{
    public Guid EventSessionId { get; set; }
}

public class UpdateEventSessionLanguageLanguageDto
{
    public int LanguageId { get; set; }
}
