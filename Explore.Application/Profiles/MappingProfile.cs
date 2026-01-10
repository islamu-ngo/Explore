using AutoMapper;
using Explore.Application.DTOs.Program;
using Explore.Domain;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using Explore.Application.DTOs.AudienceAge;
using Explore.Application.DTOs.AudienceGender;
using Explore.Application.DTOs.Education;
using Explore.Application.DTOs.EducationType;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventType;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.ProgramType;
using Explore.Application.DTOs.StatusType;
using Explore.Application.DTOs.User;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.OrganizationMember;

namespace Explore.Application.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AudienceAge, AudienceAgeListDto>().ReverseMap();
            CreateMap<AudienceGender, AudienceGenderListDto>().ReverseMap();
            CreateMap<EducationType, EducationTypeListDto>().ReverseMap();
            CreateMap<Education, CreateProgramDto>().ReverseMap();
            CreateMap<Education, EducationDto>().ReverseMap();
            CreateMap<Event, CreateProgramDto>().ReverseMap();
            CreateMap<Event, CreateEventDto>().ReverseMap();
            CreateMap<UpdateEventDto, Event>().ReverseMap();
            CreateMap<Event, EventDto>().ReverseMap();
            CreateMap<EventType, EventTypeListDto>().ReverseMap();
            CreateMap<Organization, OrganizationDto>().ReverseMap();
            CreateMap<Organization, OrganizationListDto>();
            CreateMap<Organization, CreateOrganizationDto>().ReverseMap();
            CreateMap<Organization, UpdateOrganizationStatusTypeDto>().ReverseMap();
            CreateMap<Program, ProgramDto>().ReverseMap();
            CreateMap<Program, ProgramListDto>().ReverseMap();
            CreateMap<Program, CreateProgramDto>().ReverseMap();
            CreateMap<ProgramType, ProgramTypeListDto>().ReverseMap();
            CreateMap<StatusType, StatusTypeListDto>().ReverseMap();
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<User, UpdateUserDto>().ReverseMap();
            CreateMap<OrganizationReview, OrganizationReviewDto>()
                .ForMember(dest => dest.ProgramTitle, opt => opt.MapFrom(src => src.Program != null ? src.Program.Title : string.Empty))
                .ReverseMap();
            CreateMap<OrganizationReview, CreateOrganizationReviewDto>().ReverseMap();
            
            // OrganizationMember mappings
            CreateMap<OrganizationMember, OrganizationMemberDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ReverseMap();
            CreateMap<OrganizationMember, AddOrganizationMemberDto>().ReverseMap();
            CreateMap<OrganizationMember, UpdateOrganizationMemberRoleDto>().ReverseMap();
            CreateMap<OrganizationMember, OrganizationInvitationDto>()
                .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.FullName : string.Empty))
                .ReverseMap();
        }
    }
}
