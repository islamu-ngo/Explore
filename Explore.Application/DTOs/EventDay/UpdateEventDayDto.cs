// ABOUTME: Grouped PATCH DTOs for updating EventDay fields independently.
// ABOUTME: Nullable groups represent client intent; OptionalUpdate fields disambiguate explicit clears.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.EventDay;

public class UpdateEventDayDto
{
    public UpdateEventDayEventDto? Event { get; init; }
    public UpdateEventDayLocalDateDto? LocalDate { get; init; }
    public UpdateEventDayLabelDto? Label { get; init; }
    public UpdateEventDayDescriptionDto? Description { get; init; }
    public UpdateEventDayBannerTextDto? BannerText { get; init; }
    public UpdateEventDayBannerImageDto? BannerImage { get; init; }
    public UpdateEventDayPublicationDto? Publication { get; init; }
    public UpdateEventDaySortOrderDto? SortOrder { get; init; }
    public UpdateEventDayRegistrationDto? Registration { get; init; }
}

public sealed class UpdateEventDayEventDto
{
    public Guid EventId { get; init; }
}

public sealed class UpdateEventDayLocalDateDto
{
    public DateOnly Value { get; init; }
}

public sealed class UpdateEventDayLabelDto
{
    public OptionalUpdate<string?> Value { get; init; } = OptionalUpdate<string?>.Unspecified();
}

public sealed class UpdateEventDayDescriptionDto
{
    public OptionalUpdate<string?> Value { get; init; } = OptionalUpdate<string?>.Unspecified();
}

public sealed class UpdateEventDayBannerTextDto
{
    public OptionalUpdate<string?> Value { get; init; } = OptionalUpdate<string?>.Unspecified();
}

public sealed class UpdateEventDayBannerImageDto
{
    public OptionalUpdate<Guid?> Value { get; init; } = OptionalUpdate<Guid?>.Unspecified();
}

public sealed class UpdateEventDayPublicationDto
{
    public bool IsPublished { get; init; }
}

public sealed class UpdateEventDaySortOrderDto
{
    public int Value { get; init; }
}

public sealed class UpdateEventDayRegistrationDto
{
    public bool AllowsDayScopeRegistration { get; init; }
}
