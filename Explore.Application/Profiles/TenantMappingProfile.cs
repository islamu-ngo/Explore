// ABOUTME: AutoMapper profile for Tenant, TenantNavigationLink, TenantMember, and Footer entities.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using AutoMapper;
using Explore.Application.DTOs.Footer;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.TenantMember;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class TenantMappingProfile : Profile
{
    public TenantMappingProfile()
    {
        CreateMap<Tenant, TenantDto>().ReverseMap();
        CreateMap<Tenant, TenantListDto>().ReverseMap();
        CreateMap<CreateTenantDto, Tenant>();
        CreateMap<UpdateTenantDto, Tenant>();

        CreateMap<TenantNavigationLink, TenantNavigationLinkDto>();
        CreateMap<CreateTenantNavigationLinkDto, TenantNavigationLink>();
        CreateMap<UpdateTenantNavigationLinkDto, TenantNavigationLink>();

        CreateMap<TenantMember, TenantMemberDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.FullName : null));
        CreateMap<TenantMember, TenantMemberListDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.FullName : null));
        CreateMap<CreateTenantMemberDto, TenantMember>();
        CreateMap<UpdateTenantMemberDto, TenantMember>();

        CreateMap<TenantFooterLink, FooterLinkItemDto>();

        CreateMap<TenantFooterLinkGroup, FooterLinkGroupDto>()
            .ForMember(d => d.Links, opt => opt.MapFrom(s => s.Links));

        CreateMap<TenantFooterLinkGroup, FooterLinkGroupListDto>()
            .ForMember(d => d.LinkCount, opt => opt.MapFrom(s => s.Links.Count));

        CreateMap<TenantFooterLinkGroup, FooterLinkGroupDetailsDto>()
            .ForMember(d => d.Links, opt => opt.MapFrom(s => s.Links));
    }
}
