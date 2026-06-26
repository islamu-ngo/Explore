// ABOUTME: AutoMapper profile for EventRegistration, EventRegistrationIntent, RegistrationScope, RegistrationPolicy, ScheduleItemKind, RegistrationMode.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using AutoMapper;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventRegistrationIntent;
using Explore.Application.DTOs.EventRegistrationPolicy;
using Explore.Application.DTOs.EventSessionKind;
using Explore.Application.DTOs.RegistrationMode;
using Explore.Application.DTOs.RegistrationScope;
using Explore.Application.DTOs.ScheduleItemKind;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class RegistrationMappingProfile : Profile
{
    public RegistrationMappingProfile()
    {
        CreateMap<EventRegistration, EventRegistrationDto>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.EventSession != null && src.EventSession.Event != null ? src.EventSession.Event.Title : null))
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null));
        CreateMap<EventRegistration, EventRegistrationListDto>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.EventSession != null && src.EventSession.Event != null ? src.EventSession.Event.Title : null))
            .ForMember(dest => dest.EventFeaturedImageUri, opt => opt.MapFrom(src => src.EventSession != null && src.EventSession.Event != null && src.EventSession.Event.FeaturedImage != null ? src.EventSession.Event.FeaturedImage.Uri : null))
            .ForMember(dest => dest.EventStartTime, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.StartTime : null))
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null));
        CreateMap<UpdateEventRegistrationDto, EventRegistration>();

        CreateMap<EventRegistrationIntent, EventRegistrationIntentDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.RegistrationScopeFullName, opt => opt.MapFrom(src => src.RegistrationScope != null ? src.RegistrationScope.FullName : null))
            .ForMember(dest => dest.RegistrationScopeMasterCode, opt => opt.MapFrom(src => src.RegistrationScope != null ? src.RegistrationScope.MasterCode : null))
            .ForMember(dest => dest.SelectedEventDayLabel, opt => opt.MapFrom(src => src.SelectedEventDay != null ? src.SelectedEventDay.Label : null))
            .ForMember(dest => dest.RegistrationPolicySnapshotFullName, opt => opt.MapFrom(src => src.RegistrationPolicySnapshot != null ? src.RegistrationPolicySnapshot.FullName : null))
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null));
        CreateMap<EventRegistrationIntent, EventRegistrationIntentListDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.RegistrationScopeFullName, opt => opt.MapFrom(src => src.RegistrationScope != null ? src.RegistrationScope.FullName : null))
            .ForMember(dest => dest.RegistrationScopeMasterCode, opt => opt.MapFrom(src => src.RegistrationScope != null ? src.RegistrationScope.MasterCode : null))
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null));

        CreateMap<RegistrationScope, RegistrationScopeDto>().ReverseMap();
        CreateMap<RegistrationScope, RegistrationScopeListDto>().ReverseMap();

        CreateMap<EventRegistrationPolicy, EventRegistrationPolicyDto>().ReverseMap();
        CreateMap<EventRegistrationPolicy, EventRegistrationPolicyListDto>().ReverseMap();

        CreateMap<EventSessionKind, EventSessionKindDto>().ReverseMap();
        CreateMap<EventSessionKind, EventSessionKindListDto>().ReverseMap();

        CreateMap<ScheduleItemKind, ScheduleItemKindDto>().ReverseMap();
        CreateMap<ScheduleItemKind, ScheduleItemKindListDto>().ReverseMap();

        CreateMap<RegistrationMode, RegistrationModeDto>().ReverseMap();
        CreateMap<RegistrationMode, RegistrationModeListDto>().ReverseMap();
    }
}
