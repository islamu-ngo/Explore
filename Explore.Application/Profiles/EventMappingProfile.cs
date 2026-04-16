// ABOUTME: AutoMapper profile for Event, EventSeries, EventDay, EventAgendaItem, EventTags, EventCategories, and Aspect entities.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using AutoMapper;
using Explore.Application.DTOs.AudienceAge;
using Explore.Application.DTOs.AudienceGender;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.DTOs.EventDay;
using Explore.Application.DTOs.EventTags;
using Explore.Application.DTOs.EventType;
using Explore.Domain;
using EventSeriesNS = Explore.Application.DTOs.EventSeries;

namespace Explore.Application.Profiles;

public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        // Audience Lookups
        CreateMap<AudienceAge, AudienceAgeDto>().ReverseMap();
        CreateMap<AudienceAge, AudienceAgeListDto>().ReverseMap();
        CreateMap<AudienceGender, AudienceGenderDto>().ReverseMap();
        CreateMap<AudienceGender, AudienceGenderListDto>().ReverseMap();

        // Event → EventDto
        CreateMap<Event, EventDto>()
            // Event Type
            .ForMember(dest => dest.EventTypeFullName, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.FullName : null))
            .ForMember(dest => dest.EventTypeMasterCode, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.MasterCode : null))
            // Audience Gender
            .ForMember(dest => dest.AudienceGenderFullName, opt => opt.MapFrom(src => src.AudienceGender != null ? src.AudienceGender.FullName : null))
            .ForMember(dest => dest.AudienceGenderMasterCode, opt => opt.MapFrom(src => src.AudienceGender != null ? src.AudienceGender.MasterCode : null))
            // Audience Age
            .ForMember(dest => dest.AudienceAgeFullName, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.FullName : null))
            .ForMember(dest => dest.AudienceAgeMasterCode, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MasterCode : null))
            .ForMember(dest => dest.AudienceAgeMinAge, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MinAge : (int?)null))
            .ForMember(dest => dest.AudienceAgeMaxAge, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MaxAge : (int?)null))
            // Actor
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorHandle, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Handle : null))
            .ForMember(dest => dest.ActorDid, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Did : null))
            .ForMember(dest => dest.ActorTypeId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ActorTypeId : 0))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ActorType != null ? src.Actor.ActorType.FullName : null))
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null))
            // Featured Image
            .ForMember(dest => dest.FeaturedImageUri, opt => opt.MapFrom(src => src.FeaturedImage != null ? src.FeaturedImage.Uri : null))
            // Background Image
            .ForMember(dest => dest.BackgroundImageUri, opt => opt.MapFrom(src => src.BackgroundImage != null ? src.BackgroundImage.Uri : null))
            // Event Status
            .ForMember(dest => dest.EventStatusFullName, opt => opt.MapFrom(src => src.EventStatus != null ? src.EventStatus.FullName : null))
            .ForMember(dest => dest.EventStatusMasterCode, opt => opt.MapFrom(src => src.EventStatus != null ? src.EventStatus.MasterCode : null))
            // Visibility Type
            .ForMember(dest => dest.VisibilityTypeFullName, opt => opt.MapFrom(src => src.VisibilityType != null ? src.VisibilityType.FullName : null))
            .ForMember(dest => dest.VisibilityTypeMasterCode, opt => opt.MapFrom(src => src.VisibilityType != null ? src.VisibilityType.MasterCode : null))
            // Event Format
            .ForMember(dest => dest.EventFormatFullName, opt => opt.MapFrom(src => src.EventFormat != null ? src.EventFormat.FullName : null))
            .ForMember(dest => dest.EventFormatMasterCode, opt => opt.MapFrom(src => src.EventFormat != null ? src.EventFormat.MasterCode : null))
            // Madhab
            .ForMember(dest => dest.MadhabFullName, opt => opt.MapFrom(src => src.Madhab != null ? src.Madhab.FullName : null))
            .ForMember(dest => dest.MadhabMasterCode, opt => opt.MapFrom(src => src.Madhab != null ? src.Madhab.MasterCode : null))
            // ATProto Record
            .ForMember(dest => dest.AtprotoRecordUri, opt => opt.MapFrom(src => src.AtprotoRecord != null ? src.AtprotoRecord.Uri : null))
            .ForMember(dest => dest.AtprotoRecordCid, opt => opt.MapFrom(src => src.AtprotoRecord != null ? src.AtprotoRecord.Cid : null))
            // Registration Policy
            .ForMember(dest => dest.RegistrationPolicyFullName, opt => opt.MapFrom(src => src.RegistrationPolicy != null ? src.RegistrationPolicy.FullName : null))
            .ForMember(dest => dest.RegistrationPolicyMasterCode, opt => opt.MapFrom(src => src.RegistrationPolicy != null ? src.RegistrationPolicy.MasterCode : null))
            // Aspects
            .ForMember(dest => dest.AvailableAspects, opt => opt.MapFrom(src => GetAvailableAspects(src)))
            .ForMember(dest => dest.IslamicAspect, opt => opt.MapFrom(src => src.IslamicAspect))
            .ForMember(dest => dest.TechAspect, opt => opt.MapFrom(src => src.TechAspect));

        // Event → EventListDto
        CreateMap<Event, EventListDto>()
            .ForMember(dest => dest.EventTypeFullName, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.FullName : null))
            .ForMember(dest => dest.AudienceGenderFullName, opt => opt.MapFrom(src => src.AudienceGender != null ? src.AudienceGender.FullName : null))
            .ForMember(dest => dest.AudienceAgeFullName, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.FullName : null))
            .ForMember(dest => dest.AudienceAgeMinAge, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MinAge : (int?)null))
            .ForMember(dest => dest.AudienceAgeMaxAge, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MaxAge : (int?)null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorTypeId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ActorTypeId : 0))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ActorType != null ? src.Actor.ActorType.FullName : null))
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : (Guid?)null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null))
            .ForMember(dest => dest.FeaturedImageUri, opt => opt.MapFrom(src => src.FeaturedImage != null ? src.FeaturedImage.Uri : null))
            .ForMember(dest => dest.EventStatusFullName, opt => opt.MapFrom(src => src.EventStatus != null ? src.EventStatus.FullName : null))
            .ForMember(dest => dest.VisibilityTypeFullName, opt => opt.MapFrom(src => src.VisibilityType != null ? src.VisibilityType.FullName : null))
            .ForMember(dest => dest.EventFormatFullName, opt => opt.MapFrom(src => src.EventFormat != null ? src.EventFormat.FullName : null))
            .ForMember(dest => dest.MadhabFullName, opt => opt.MapFrom(src => src.Madhab != null ? src.Madhab.FullName : null))
            .ForMember(dest => dest.EventSeriesTitle, opt => opt.MapFrom(src => src.EventSeries != null ? src.EventSeries.Title : null))
            .ForMember(dest => dest.RegistrationPolicyFullName, opt => opt.MapFrom(src => src.RegistrationPolicy != null ? src.RegistrationPolicy.FullName : null))
            .ForMember(dest => dest.IsPast, opt => opt.MapFrom(src => src.LastSessionStartUtc != null && src.LastSessionStartUtc <= DateTimeOffset.UtcNow));

        // Event Series
        CreateMap<EventSeries, EventSeriesNS.EventSeriesListDto>()
            .ForMember(d => d.FeaturedImageUri, opt => opt.MapFrom(s => s.FeaturedImage != null ? s.FeaturedImage.Uri : null))
            .ForMember(d => d.ActorDisplayName, opt => opt.MapFrom(s => s.Actor != null && s.Actor.Pii != null ? s.Actor.Pii.DisplayName : null))
            .ForMember(d => d.EventCount, opt => opt.MapFrom(s => s.Events != null ? s.Events.Count : 0));

        CreateMap<EventSeries, EventSeriesNS.EventSeriesDto>()
            .ForMember(d => d.FeaturedImageUri, opt => opt.MapFrom(s => s.FeaturedImage != null ? s.FeaturedImage.Uri : null))
            .ForMember(d => d.ActorDisplayName, opt => opt.MapFrom(s => s.Actor != null && s.Actor.Pii != null ? s.Actor.Pii.DisplayName : null));

        CreateMap<EventSeriesNS.CreateEventSeriesDto, EventSeries>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.TotalViews, opt => opt.Ignore())
            .ForMember(dest => dest.VisibilityTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.VisibilityType, opt => opt.Ignore())
            .ForMember(dest => dest.StartDateUtc, opt => opt.Ignore())
            .ForMember(dest => dest.EndDateUtc, opt => opt.Ignore())
            .ForMember(dest => dest.Events, opt => opt.Ignore())
            .ForMember(dest => dest.Actor, opt => opt.Ignore())
            .ForMember(dest => dest.FeaturedImage, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore());

        CreateMap<EventSeriesNS.UpdateEventSeriesDto, EventSeries>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.ActorId, opt => opt.Ignore())
            .ForMember(dest => dest.TotalViews, opt => opt.Ignore())
            .ForMember(dest => dest.VisibilityTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.VisibilityType, opt => opt.Ignore())
            .ForMember(dest => dest.StartDateUtc, opt => opt.Ignore())
            .ForMember(dest => dest.EndDateUtc, opt => opt.Ignore())
            .ForMember(dest => dest.Events, opt => opt.Ignore())
            .ForMember(dest => dest.Actor, opt => opt.Ignore())
            .ForMember(dest => dest.FeaturedImage, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore());

        // CreateEvent / UpdateEvent
        CreateMap<CreateEventDto, Event>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ActorId, opt => opt.Ignore())
            .ForMember(dest => dest.TotalViews, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.SessionCount, opt => opt.Ignore())
            .ForMember(dest => dest.AtprotoRecordId, opt => opt.Ignore())
            .ForMember(dest => dest.IsUserReported, opt => opt.Ignore())
            .ForMember(dest => dest.FirstSessionDate, opt => opt.MapFrom(src =>
                src.FirstSessionDate.HasValue ? DateOnly.FromDateTime(src.FirstSessionDate.Value.DateTime) : (DateOnly?)null))
            .ForMember(dest => dest.LastSessionDate, opt => opt.MapFrom(src =>
                src.LastSessionDate.HasValue ? DateOnly.FromDateTime(src.LastSessionDate.Value.DateTime) : (DateOnly?)null))
            .ForMember(dest => dest.FirstSessionStartUtc, opt => opt.MapFrom(src => src.FirstSessionStartUtc ?? src.FirstSessionDate))
            .ForMember(dest => dest.LastSessionStartUtc, opt => opt.MapFrom(src => src.LastSessionStartUtc ?? src.LastSessionDate))
            .ForMember(dest => dest.EventTimeZoneId, opt => opt.MapFrom(src => src.EventTimeZoneId ?? src.Timezone))
            .ForMember(dest => dest.Timezone, opt => opt.MapFrom(src => src.Timezone ?? src.EventTimeZoneId));

        CreateMap<UpdateEventDto, Event>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ActorId, opt => opt.Ignore())
            .ForMember(dest => dest.TotalViews, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.FirstSessionStartUtc, opt => opt.MapFrom(src => src.FirstSessionStartUtc))
            .ForMember(dest => dest.LastSessionStartUtc, opt => opt.MapFrom(src => src.LastSessionStartUtc))
            .ForMember(dest => dest.EventTimeZoneId, opt => opt.MapFrom(src => src.EventTimeZoneId ?? src.Timezone))
            .ForMember(dest => dest.Timezone, opt => opt.MapFrom(src => src.Timezone ?? src.EventTimeZoneId));

        // Event Type
        CreateMap<EventType, EventTypeListDto>().ReverseMap();

        // Event Day
        CreateMap<EventDay, EventDayDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null));
        CreateMap<EventDay, EventDayListDto>();
        CreateMap<CreateEventDayDto, EventDay>();
        CreateMap<UpdateEventDayDto, EventDay>();

        // Event Agenda Item
        CreateMap<EventAgendaItem, EventAgendaItemDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.KindFullName, opt => opt.MapFrom(src => src.Kind != null ? src.Kind.FullName : null));
        CreateMap<EventAgendaItem, EventAgendaItemListDto>()
            .ForMember(dest => dest.KindFullName, opt => opt.MapFrom(src => src.Kind != null ? src.Kind.FullName : null));
        CreateMap<CreateEventAgendaItemDto, EventAgendaItem>();
        CreateMap<UpdateEventAgendaItemDto, EventAgendaItem>();

        // Event Tags
        CreateMap<EventTags, EventTagsDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
            .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null));
        CreateMap<EventTags, EventTagsListDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
            .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null));
        CreateMap<CreateEventTagsDto, EventTags>();
        CreateMap<UpdateEventTagsDto, EventTags>();

        // Event Categories
        CreateMap<EventCategories, EventCategoriesDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.CategoryFullName, opt => opt.MapFrom(src => src.Category != null ? src.Category.FullName : null));
        CreateMap<EventCategories, EventCategoriesListDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.CategoryFullName, opt => opt.MapFrom(src => src.Category != null ? src.Category.FullName : null));
        CreateMap<CreateEventCategoriesDto, EventCategories>();
        CreateMap<UpdateEventCategoriesDto, EventCategories>();

        // Aspects
        CreateMap<EventIslamicAspect, EventIslamicAspectDto>()
            .ForMember(dest => dest.MadhabName, opt => opt.MapFrom(src => src.Madhab != null ? src.Madhab.FullName : null))
            .ForMember(dest => dest.PrimaryLanguageName, opt => opt.MapFrom(src => src.PrimaryLanguage != null ? src.PrimaryLanguage.FullName : null));
        CreateMap<CreateUpdateIslamicAspectDto, EventIslamicAspect>();
        CreateMap<EventTechAspect, EventTechAspectDto>();
        CreateMap<CreateUpdateTechAspectDto, EventTechAspect>();
    }

    private static List<string> GetAvailableAspects(Event src)
    {
        var aspects = new List<string>();
        if (src.IslamicAspect != null) aspects.Add("Islamic");
        if (src.TechAspect != null) aspects.Add("Tech");
        return aspects;
    }
}
