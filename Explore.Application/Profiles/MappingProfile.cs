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
        }
    }
}
