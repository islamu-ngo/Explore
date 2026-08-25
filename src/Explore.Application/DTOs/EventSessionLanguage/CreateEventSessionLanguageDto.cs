using System;

namespace Explore.Application.DTOs.EventSessionLanguage;

public sealed record CreateEventSessionLanguageDto
{
    public Guid EventSessionId { get; init; }
    public int LanguageId { get; init; }
}
