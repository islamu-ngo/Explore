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
using Explore.Application.DTOs.TagType;
using Explore.Application.DTOs.TagTypeTags;
using Explore.Application.DTOs.EventTags;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.RegistrationMode;
using Explore.Application.DTOs.Madhab;
using Explore.Application.DTOs.EventStatus;
using Explore.Application.DTOs.EventFormat;
using Explore.Application.DTOs.VisibilityType;
using Explore.Application.DTOs.ActorType;
using Explore.Application.DTOs.DidCustodyType;
using Explore.Application.DTOs.OrganizationRole;
using Explore.Application.DTOs.OrganizationPosition;
using Explore.Application.DTOs.FileType;

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

            // TagType mappings (readonly lookup)
            CreateMap<TagType, TagTypeDto>().ReverseMap();
            CreateMap<TagType, TagTypeListDto>().ReverseMap();

            // TagTypeTags mappings (link table)
            CreateMap<Domain.TagTypeTags, TagTypeTagsDto>()
                .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
                .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null))
                .ForMember(dest => dest.TagTypeFullName, opt => opt.MapFrom(src => src.TagType != null ? src.TagType.FullName : null))
                .ForMember(dest => dest.TagTypeMasterCode, opt => opt.MapFrom(src => src.TagType != null ? src.TagType.MasterCode : null))
                .ReverseMap();
            CreateMap<Domain.TagTypeTags, TagTypeTagsListDto>()
                .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
                .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null))
                .ForMember(dest => dest.TagTypeFullName, opt => opt.MapFrom(src => src.TagType != null ? src.TagType.FullName : null))
                .ForMember(dest => dest.TagTypeMasterCode, opt => opt.MapFrom(src => src.TagType != null ? src.TagType.MasterCode : null))
                .ReverseMap();
            CreateMap<Domain.TagTypeTags, CreateTagTypeTagsDto>().ReverseMap();
            CreateMap<Domain.TagTypeTags, UpdateTagTypeTagsDto>().ReverseMap();

            // EventTags mappings (link table)
            CreateMap<EventTags, EventTagsDto>()
                .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
                .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
                .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null))
                .ReverseMap();
            CreateMap<EventTags, EventTagsListDto>()
                .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
                .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
                .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null))
                .ReverseMap();
            CreateMap<EventTags, CreateEventTagsDto>().ReverseMap();
            CreateMap<EventTags, UpdateEventTagsDto>().ReverseMap();

            // EventCategories mappings (link table)
            CreateMap<EventCategories, EventCategoriesDto>().ReverseMap();
            CreateMap<EventCategories, EventCategoriesListDto>().ReverseMap();
            CreateMap<EventCategories, CreateEventCategoriesDto>().ReverseMap();
            CreateMap<EventCategories, UpdateEventCategoriesDto>().ReverseMap();

            // EventRegistration mappings
            CreateMap<EventRegistration, EventRegistrationDto>()
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.User != null ? src.User.FirstName : null))
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.User != null ? src.User.LastName : null))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
                .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
                .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null))
                .ReverseMap();
            CreateMap<EventRegistration, EventRegistrationListDto>()
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.User != null ? src.User.FirstName : null))
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.User != null ? src.User.LastName : null))
                .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
                .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
                .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null))
                .ReverseMap();
            CreateMap<EventRegistration, CreateEventRegistrationDto>().ReverseMap();
            CreateMap<EventRegistration, UpdateEventRegistrationDto>().ReverseMap();

            // RegistrationMode mappings (readonly lookup)
            CreateMap<RegistrationMode, RegistrationModeDto>().ReverseMap();
            CreateMap<RegistrationMode, RegistrationModeListDto>().ReverseMap();

            // Madhab mappings (readonly lookup)
            CreateMap<Madhab, MadhabDto>().ReverseMap();
            CreateMap<Madhab, MadhabListDto>().ReverseMap();

            // EventStatus mappings (readonly lookup)
            CreateMap<Domain.EventStatus, EventStatusDto>().ReverseMap();
            CreateMap<Domain.EventStatus, EventStatusListDto>().ReverseMap();

            // EventFormat mappings (readonly lookup)
            CreateMap<EventFormat, EventFormatDto>().ReverseMap();
            CreateMap<EventFormat, EventFormatListDto>().ReverseMap();

            // VisibilityType mappings (readonly lookup)
            CreateMap<VisibilityType, VisibilityTypeDto>().ReverseMap();
            CreateMap<VisibilityType, VisibilityTypeListDto>().ReverseMap();

            // ActorType mappings (readonly lookup)
            CreateMap<Domain.ActorType, ActorTypeDto>().ReverseMap();
            CreateMap<Domain.ActorType, ActorTypeListDto>().ReverseMap();

            // DidCustodyType mappings (readonly lookup)
            CreateMap<Domain.DidCustodyType, DidCustodyTypeDto>().ReverseMap();
            CreateMap<Domain.DidCustodyType, DidCustodyTypeListDto>().ReverseMap();

            // OrganizationRole mappings (readonly lookup)
            CreateMap<Domain.OrganizationRole, OrganizationRoleDto>().ReverseMap();
            CreateMap<Domain.OrganizationRole, OrganizationRoleListDto>().ReverseMap();

            // OrganizationPosition mappings (readonly lookup)
            CreateMap<Domain.OrganizationPosition, OrganizationPositionDto>().ReverseMap();
            CreateMap<Domain.OrganizationPosition, OrganizationPositionListDto>().ReverseMap();

            // FileType mappings (readonly lookup)
            CreateMap<Domain.FileType, FileTypeDto>().ReverseMap();
            CreateMap<Domain.FileType, FileTypeListDto>().ReverseMap();
        }
    }
}
