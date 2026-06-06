// ABOUTME: AutoMapper profile for EventSession, EventSessionAgendaItem, EventSessionSpeaker, and EventSessionLanguage entities.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using System.Linq;
using AutoMapper;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class EventSessionMappingProfile : Profile
{
    public EventSessionMappingProfile()
    {
        // Event Session Islamic Aspect
        CreateMap<EventSessionIslamicAspect, EventSessionIslamicAspectDto>();

        // Event Session
        CreateMap<EventSessionGroupSession, EventSessionGroupAssignmentDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.EventSessionGroup.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.EventSessionGroup.Slug))
            .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.EventSessionGroup.Color));
        CreateMap<EventSessionGroup, EventSessionGroupDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.FullName : null))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room != null ? src.Room.Name : null));
        CreateMap<EventSessionGroup, EventSessionGroupListDto>()
            .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.FullName : null))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room != null ? src.Room.Name : null));
        CreateMap<CreateEventSessionGroupRequestDto, EventSessionGroup>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Event, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore())
            .ForMember(dest => dest.Room, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Sessions, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore());
        CreateMap<EventSession, EventSessionDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.EventSessionKindFullName, opt => opt.MapFrom(src => src.EventSessionKind != null ? src.EventSessionKind.FullName : null))
            .ForMember(dest => dest.EventSessionKindMasterCode, opt => opt.MapFrom(src => src.EventSessionKind != null ? src.EventSessionKind.MasterCode : null))
            .ForMember(dest => dest.LocationFullName, opt => opt.MapFrom(src => src.Location != null ? src.Location.FullName : null))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room != null ? src.Room.Name : null))
            .ForMember(dest => dest.FeaturedImageUri, opt => opt.MapFrom(src => src.FeaturedImage != null ? src.FeaturedImage.Uri : null))
            .ForMember(dest => dest.SessionGroups, opt => opt.MapFrom(src => src.SessionGroups
                .Where(assignment => assignment.EventSessionGroup.IsPublished)
                .OrderByDescending(assignment => assignment.IsPrimary)
                .ThenBy(assignment => assignment.SortOrder)));
        CreateMap<EventSession, EventSessionListDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.EventSessionKindFullName, opt => opt.MapFrom(src => src.EventSessionKind != null ? src.EventSessionKind.FullName : null))
            .ForMember(dest => dest.EventSessionKindMasterCode, opt => opt.MapFrom(src => src.EventSessionKind != null ? src.EventSessionKind.MasterCode : null))
            .ForMember(dest => dest.LocationFullName, opt => opt.MapFrom(src => src.Location != null ? src.Location.FullName : null))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room != null ? src.Room.Name : null))
            .ForMember(dest => dest.FeaturedImageUri, opt => opt.MapFrom(src => src.FeaturedImage != null ? src.FeaturedImage.Uri : null))
            .ForMember(dest => dest.SessionGroups, opt => opt.MapFrom(src => src.SessionGroups
                .Where(assignment => assignment.EventSessionGroup.IsPublished)
                .OrderByDescending(assignment => assignment.IsPrimary)
                .ThenBy(assignment => assignment.SortOrder)));
        CreateMap<CreateEventSessionDto, EventSession>()
            .ForMember(dest => dest.StartTime, opt => opt.Ignore())
            .ForMember(dest => dest.EndTime, opt => opt.Ignore())
            .ForMember(dest => dest.IslamicAspect, opt => opt.Ignore());
        CreateMap<UpdateEventSessionDto, EventSession>()
            .ForMember(dest => dest.StartTime, opt => opt.Ignore())
            .ForMember(dest => dest.EndTime, opt => opt.Ignore())
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
