using AutoMapper;
using Explore.Domain;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using Explore.Application.DTOs.AudienceAge;
using Explore.Application.DTOs.AudienceGender;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventType;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.StatusType;
using Explore.Application.DTOs.User;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.Tag;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.DTOs.Language;
using Explore.Application.DTOs.EventSessionLanguage;

namespace Explore.Application.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AudienceAge, AudienceAgeListDto>().ReverseMap();
            CreateMap<AudienceGender, AudienceGenderListDto>().ReverseMap();
            CreateMap<Event, CreateEventDto>().ReverseMap();
            CreateMap<UpdateEventDto, Event>().ReverseMap();
            CreateMap<Event, EventDto>().ReverseMap();
            CreateMap<EventType, EventTypeListDto>().ReverseMap();
            CreateMap<Organization, OrganizationDto>().ReverseMap();
            CreateMap<Organization, OrganizationListDto>();
            CreateMap<Organization, CreateOrganizationDto>().ReverseMap();
            CreateMap<Organization, UpdateOrganizationApprovalStatusDto>().ReverseMap();
            CreateMap<OrganizationMember, OrganizationMemberDto>().ReverseMap();
            //CreateMap<OrganizationMember, OrganizationMemberDto>()
            //    .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
            //    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null && !string.IsNullOrEmpty(src.User.Email) ? src.User.Email : src.Email))
            //    .ReverseMap();
            CreateMap<ApprovalStatus, StatusTypeListDto>().ReverseMap();
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<User, UpdateUserDto>().ReverseMap();
            CreateMap<OrganizationReview, OrganizationReviewDto>().ReverseMap();
            CreateMap<OrganizationReview, CreateOrganizationReviewDto>().ReverseMap();

            // EventSession mappings
            CreateMap<EventSession, EventSessionDto>().ReverseMap();

            CreateMap<EventSession, EventSessionListDto>().ReverseMap();

            CreateMap<EventSession, CreateEventSessionDto>().ReverseMap();
            CreateMap<EventSession, UpdateEventSessionDto>().ReverseMap();

            // Location mappings
            CreateMap<Location, LocationDto>().ReverseMap();
            CreateMap<Location, LocationListDto>().ReverseMap();
            CreateMap<Location, CreateLocationDto>().ReverseMap();
            CreateMap<Location, UpdateLocationDto>().ReverseMap();

            // Category mappings
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.ParentFullName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : null))
                .ReverseMap();
            CreateMap<Category, CategoryListDto>()
                .ForMember(dest => dest.ParentFullName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : null));
            CreateMap<CreateCategoryDto, Category>();
            CreateMap<UpdateCategoryDto, Category>();

            // Tag mappings
            CreateMap<Tag, TagDto>().ReverseMap();
            CreateMap<Tag, TagListDto>();
            CreateMap<CreateTagDto, Tag>();
            CreateMap<UpdateTagDto, Tag>();

            // EventSessionAgendaItem mappings
            CreateMap<EventSessionAgendaItem, EventSessionAgendaItemDto>().ReverseMap();
            CreateMap<EventSessionAgendaItem, EventSessionAgendaItemListDto>().ReverseMap();
            CreateMap<EventSessionAgendaItem, CreateEventSessionAgendaItemDto>().ReverseMap();
            CreateMap<EventSessionAgendaItem, UpdateEventSessionAgendaItemDto>().ReverseMap();

            // EventSessionSpeaker mappings
            CreateMap<EventSessionSpeaker, EventSessionSpeakerDto>().ReverseMap();
            CreateMap<EventSessionSpeaker, EventSessionSpeakerListDto>().ReverseMap();
            CreateMap<EventSessionSpeaker, CreateEventSessionSpeakerDto>().ReverseMap();
            CreateMap<EventSessionSpeaker, UpdateEventSessionSpeakerDto>().ReverseMap();

            // Language mappings (readonly lookup)
            CreateMap<Language, LanguageDto>().ReverseMap();
            CreateMap<Language, LanguageListDto>().ReverseMap();

            // EventSessionLanguage mappings (link table)
            CreateMap<EventSessionLanguage, EventSessionLanguageDto>()
                .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
                .ForMember(dest => dest.LanguageFullName, opt => opt.MapFrom(src => src.Language != null ? src.Language.FullName : null))
                .ForMember(dest => dest.LanguageMasterCode, opt => opt.MapFrom(src => src.Language != null ? src.Language.MasterCode : null))
                .ReverseMap();
            CreateMap<EventSessionLanguage, EventSessionLanguageListDto>().ReverseMap();
            CreateMap<EventSessionLanguage, CreateEventSessionLanguageDto>().ReverseMap();
            CreateMap<EventSessionLanguage, UpdateEventSessionLanguageDto>().ReverseMap();
        }
    }
}
