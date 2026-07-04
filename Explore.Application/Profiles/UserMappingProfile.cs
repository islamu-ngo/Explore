// ABOUTME: AutoMapper profile for User, UserAuthenticationToken, and UserExternalLogin entities.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using AutoMapper;
using Explore.Application.DTOs.User;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorHandle, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Handle : null))
            .ForMember(dest => dest.ActorBackgroundColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundColor : null))
            .ForMember(dest => dest.ActorBackgroundEffect, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundEffect : null))
            .ForMember(dest => dest.ActorBannerColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerColor : null))
            .ForMember(dest => dest.ActorBannerPictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerPictureId : null))
            .ForMember(dest => dest.ActorBannerPictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BannerPicture != null ? src.Actor.BannerPicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundImageId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundImageId : null))
            .ForMember(dest => dest.ActorBackgroundImageUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BackgroundImage != null ? src.Actor.BackgroundImage.Uri : null));
        CreateMap<UpdateUserDto, User>();
        CreateMap<UpdateUserNamesDto, User>();

        CreateMap<Domain.UserAuthenticationToken, UserAuthenticationTokenDto>();
        CreateMap<Domain.UserAuthenticationToken, UserAuthenticationTokenListDto>();
        CreateMap<CreateUserAuthenticationTokenDto, Domain.UserAuthenticationToken>();
        CreateMap<UpdateUserAuthenticationTokenDto, Domain.UserAuthenticationToken>();

        CreateMap<Domain.UserExternalLogin, UserExternalLoginDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null));
        CreateMap<Domain.UserExternalLogin, UserExternalLoginListDto>();
        CreateMap<CreateUserExternalLoginDto, Domain.UserExternalLogin>();
        CreateMap<UpdateUserExternalLoginDto, Domain.UserExternalLogin>();
    }
}
