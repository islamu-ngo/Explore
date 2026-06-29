// ABOUTME: PATCH wrapper DTO for property-level Event shell updates using nullable logical groups.
// ABOUTME: Route ID owns identity; nullable fields use OptionalUpdate for explicit set-or-clear semantics.

using System;
using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Event;

public class UpdateEventDto
{
    public UpdateEventTitleDto? Title { get; set; }
    public UpdateEventSubtitleDto? Subtitle { get; set; }
    public UpdateEventDescriptionDto? Description { get; set; }
    public UpdateEventContentDto? Content { get; set; }
    public UpdateEventSlugDto? Slug { get; set; }
    public UpdateEventEventTypeDto? EventType { get; set; }
    public UpdateEventAudienceGenderDto? AudienceGender { get; set; }
    public UpdateEventAudienceAgeDto? AudienceAge { get; set; }
    public UpdateEventPriceDto? Price { get; set; }
    public UpdateEventCurrencyCodeDto? CurrencyCode { get; set; }
    public UpdateEventFeaturedImageDto? FeaturedImage { get; set; }
    public UpdateEventRegistrationRequiredDto? RegistrationRequired { get; set; }
    public UpdateEventExternalRegistrationUrlDto? ExternalRegistrationUrl { get; set; }
    public UpdateEventVisibilityDto? Visibility { get; set; }
    public UpdateEventFormatDto? Format { get; set; }
    public UpdateEventMadhabDto? Madhab { get; set; }
    public UpdateEventTimezoneDto? Timezone { get; set; }
    public UpdateEventEventTimeZoneDto? EventTimeZone { get; set; }
    public UpdateEventUrlDto? EventUrl { get; set; }
    public UpdateEventBackgroundColorDto? BackgroundColor { get; set; }
    public UpdateEventBackgroundEffectDto? BackgroundEffect { get; set; }
    public UpdateEventBackgroundImageDto? BackgroundImage { get; set; }
    public UpdateEventSourceTemplateDto? SourceTemplate { get; set; }
    public UpdateEventSeriesMembershipDto? SeriesMembership { get; set; }
    public UpdateEventSeriesOrderDto? SeriesOrder { get; set; }
    public UpdateEventRegistrationPolicyDto? RegistrationPolicy { get; set; }
}

public class UpdateEventTitleDto
{
    public required string Value { get; set; }
}

public class UpdateEventSubtitleDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventDescriptionDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventContentDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventSlugDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventEventTypeDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventAudienceGenderDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventAudienceAgeDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventPriceDto
{
    public OptionalUpdate<decimal?> Value { get; set; } = OptionalUpdate<decimal?>.Unspecified();
}

public class UpdateEventCurrencyCodeDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventFeaturedImageDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventRegistrationRequiredDto
{
    public bool Value { get; set; }
}

public class UpdateEventExternalRegistrationUrlDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventVisibilityDto
{
    public int Value { get; set; }
}

public class UpdateEventFormatDto
{
    public int Value { get; set; }
}

public class UpdateEventMadhabDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventTimezoneDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventEventTimeZoneDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventUrlDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventBackgroundColorDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventBackgroundEffectDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventBackgroundImageDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventSourceTemplateDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventSeriesMembershipDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventSeriesOrderDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateEventRegistrationPolicyDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}
