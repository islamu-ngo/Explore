// ABOUTME: Grouped PATCH payload for updating event-session language assignments.
// ABOUTME: Uses nullable groups so clients can update the session or language independently.

namespace Explore.Application.DTOs.EventSessionLanguage;

public sealed record UpdateEventSessionLanguageDto
{
    public UpdateEventSessionLanguageSessionDto? Session { get; init; }
    public UpdateEventSessionLanguageLanguageDto? Language { get; init; }
}

public sealed record UpdateEventSessionLanguageSessionDto
{
    public Guid EventSessionId { get; init; }
}

public sealed record UpdateEventSessionLanguageLanguageDto
{
    public int LanguageId { get; init; }
}
