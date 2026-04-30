// ABOUTME: AutoMapper profile for EventSession, EventSessionAgendaItem, EventSessionSpeaker, and EventSessionLanguage entities.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using AutoMapper;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class EventSessionMappingProfile : Profile
{
    public EventSessionMappingProfile()
    {
        // Event Session Islamic Aspect
        CreateMap<EventSessionIslamicAspect, EventSessionIslamicAspectDto>().ReverseMap();

        // Event Session
        CreateMap<EventSession, EventSessionDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.LocationFullName, opt => opt.MapFrom(src => src.Location != null ? src.Location.FullName : null))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room != null ? src.Room.Name : null))
            .ForMember(dest => dest.FeaturedImageUri, opt => opt.MapFrom(src => src.FeaturedImage != null ? src.FeaturedImage.Uri : null));
        CreateMap<EventSession, EventSessionListDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.LocationFullName, opt => opt.MapFrom(src => src.Location != null ? src.Location.FullName : null))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room != null ? src.Room.Name : null))
            .ForMember(dest => dest.FeaturedImageUri, opt => opt.MapFrom(src => src.FeaturedImage != null ? src.FeaturedImage.Uri : null));
        CreateMap<CreateEventSessionDto, EventSession>()
            .ForMember(dest => dest.IslamicAspect, opt => opt.Ignore());
        CreateMap<UpdateEventSessionDto, EventSession>()
            .ForMember(dest => dest.IslamicAspect, opt => opt.Ignore());

        // Event Session Agenda Item
        CreateMap<EventSessionAgendaItem, EventSessionAgendaItemDto>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventSession.EventId))
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null));
        CreateMap<EventSessionAgendaItem, EventSessionAgendaItemListDto>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventSession.EventId))
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null));
        CreateMap<CreateEventSessionAgendaItemDto, EventSessionAgendaItem>();
        CreateMap<UpdateEventSessionAgendaItemDto, EventSessionAgendaItem>();

        // Event Session Speaker
        CreateMap<EventSessionSpeaker, EventSessionSpeakerDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null));
        CreateMap<EventSessionSpeaker, EventSessionSpeakerListDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null));
        CreateMap<CreateEventSessionSpeakerDto, EventSessionSpeaker>();
        CreateMap<UpdateEventSessionSpeakerDto, EventSessionSpeaker>();

        // Event Session Language
        CreateMap<EventSessionLanguage, EventSessionLanguageDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.LanguageFullName, opt => opt.MapFrom(src => src.Language != null ? src.Language.FullName : null))
            .ForMember(dest => dest.LanguageMasterCode, opt => opt.MapFrom(src => src.Language != null ? src.Language.MasterCode : null));
        CreateMap<EventSessionLanguage, EventSessionLanguageListDto>();
        CreateMap<CreateEventSessionLanguageDto, EventSessionLanguage>();
        CreateMap<UpdateEventSessionLanguageDto, EventSessionLanguage>();
    }
}
