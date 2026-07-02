// ABOUTME: Wrapper DTO for PATCH-based EventSession updates using nullable logical groups.
// ABOUTME: Route ID targets the session while groups express independent field update intent.

using Explore.Application.Models.Common;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventSession;

public class UpdateEventSessionDto
{
    public UpdateEventSessionEventDto? Event { get; set; }
    public UpdateEventSessionScheduleDto? Schedule { get; set; }
    public UpdateEventSessionLocationDto? Location { get; set; }
    public UpdateEventSessionFeaturedImageDto? FeaturedImage { get; set; }
    public UpdateEventSessionRoomDto? Room { get; set; }
    public UpdateEventSessionSortOrderDto? SortOrder { get; set; }
    public UpdateEventSessionTitleDto? Title { get; set; }
    public UpdateEventSessionKindDto? Kind { get; set; }
    public UpdateEventSessionDescriptionDto? Description { get; set; }
    public UpdateEventSessionSlugDto? Slug { get; set; }
    public UpdateEventSessionMaxAudienceAttendeesDto? MaxAudienceAttendees { get; set; }
    public UpdateEventSessionRegistrationModeDto? RegistrationMode { get; set; }
    public UpdateEventSessionPriceDto? Price { get; set; }
    public UpdateEventSessionCurrencyCodeDto? CurrencyCode { get; set; }
    public UpdateEventSessionIslamicAspectUpdateDto? IslamicAspect { get; set; }
}

public class UpdateEventSessionEventDto
{
    public Guid EventId { get; set; }
}

public class UpdateEventSessionScheduleDto
{
    public OptionalUpdate<DateTimeOffset?> StartTime { get; set; } = OptionalUpdate<DateTimeOffset?>.Unspecified();
    public OptionalUpdate<DateTimeOffset?> EndTime { get; set; } = OptionalUpdate<DateTimeOffset?>.Unspecified();
    public OptionalUpdate<SessionEndTimeType> EndTimeType { get; set; } = OptionalUpdate<SessionEndTimeType>.Unspecified();
}

public class UpdateEventSessionLocationDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventSessionFeaturedImageDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventSessionRoomDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventSessionSortOrderDto
{
    public int Value { get; set; }
}

public class UpdateEventSessionTitleDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventSessionKindDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventSessionDescriptionDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventSessionSlugDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventSessionMaxAudienceAttendeesDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventSessionRegistrationModeDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventSessionPriceDto
{
    public OptionalUpdate<decimal?> Value { get; set; } = OptionalUpdate<decimal?>.Unspecified();
}

public class UpdateEventSessionCurrencyCodeDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventSessionIslamicAspectUpdateDto
{
    public OptionalUpdate<EventSessionIslamicAspectDto?> Value { get; set; } = OptionalUpdate<EventSessionIslamicAspectDto?>.Unspecified();
}
