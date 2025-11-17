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

namespace Explore.Application.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AudienceAge, AudienceAgeListDto>().ReverseMap();
            CreateMap<AudienceGender, AudienceGenderListDto>().ReverseMap();
            CreateMap<EducationType, EducationTypeListDto>().ReverseMap();
            //CreateMap<Education, CreatEducationDto>().ReverseMap();
            CreateMap<Education, EducationDto>().ReverseMap();
            CreateMap<Education, EducationSpecificDto>().ReverseMap();
            CreateMap<Event, EventDto>().ReverseMap();
            CreateMap<Event, EventSpecificDto>().ReverseMap();
            CreateMap<Event, EventListDto>().ReverseMap();
            CreateMap<Event, CreateEventDto>().ReverseMap();
            CreateMap<EventType, EventTypeListDto>().ReverseMap();
            CreateMap<Organization, OrganizationDto>().ReverseMap();
            CreateMap<Organization, OrganizationListDto>().ReverseMap();
            CreateMap<Organization, CreateOrganizationDto>().ReverseMap();
            CreateMap<Organization, UpdateOrganizationStatusTypeDto>().ReverseMap();
            CreateMap<Program, ProgramDto>().ReverseMap();
            CreateMap<Program, ProgramListDto>().ReverseMap();
            CreateMap<Program, CreateProgramDto>().ReverseMap();
            CreateMap<ProgramType, ProgramTypeListDto>().ReverseMap();
            CreateMap<StatusType, StatusTypeListDto>().ReverseMap();
        }
    }
}
