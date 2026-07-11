// ABOUTME: AutoMapper profile for Organization, Group, GroupMember, OrganizationMember, ApprovalStatus, and OrganizationReview entities.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using AutoMapper;
using Explore.Application.DTOs.Group;
using Explore.Application.DTOs.GroupMember;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.StatusType;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class OrganizationMappingProfile : Profile
{
    public OrganizationMappingProfile()
    {
        // Group
        CreateMap<Group, GroupDto>()
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorHandle, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Handle : null))
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundColor : null))
            .ForMember(dest => dest.ActorBackgroundEffect, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundEffect : null))
            .ForMember(dest => dest.ActorBannerColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerColor : null))
            .ForMember(dest => dest.ActorBannerPictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerPictureId : null))
            .ForMember(dest => dest.ActorBannerPictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BannerPicture != null ? src.Actor.BannerPicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundImageId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundImageId : null))
            .ForMember(dest => dest.ActorBackgroundImageUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BackgroundImage != null ? src.Actor.BackgroundImage.Uri : null));
        CreateMap<Group, GroupListDto>()
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundColor : null))
            .ForMember(dest => dest.ActorBackgroundEffect, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundEffect : null))
            .ForMember(dest => dest.ActorBannerColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerColor : null))
            .ForMember(dest => dest.ActorBannerPictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerPictureId : null))
            .ForMember(dest => dest.ActorBannerPictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BannerPicture != null ? src.Actor.BannerPicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundImageId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundImageId : null))
            .ForMember(dest => dest.ActorBackgroundImageUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BackgroundImage != null ? src.Actor.BackgroundImage.Uri : null));
        CreateMap<CreateGroupDto, Group>();
        CreateMap<UpdateGroupApprovalStatusDto, Group>();

        // Group Member
        CreateMap<GroupMember, GroupMemberDto>()
            .ForMember(dest => dest.GroupFullName, opt => opt.MapFrom(src => src.Group != null ? src.Group.FullName : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.FullName : null))
            .ForMember(dest => dest.GroupPositionFullName, opt => opt.MapFrom(src => src.GroupPosition != null ? src.GroupPosition.FullName : null));
        CreateMap<GroupMember, GroupMemberListDto>()
            .ForMember(dest => dest.GroupFullName, opt => opt.MapFrom(src => src.Group != null ? src.Group.FullName : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.FullName : null))
            .ForMember(dest => dest.GroupPositionFullName, opt => opt.MapFrom(src => src.GroupPosition != null ? src.GroupPosition.FullName : null));
        CreateMap<AddGroupMemberDto, GroupMember>();
        CreateMap<UpdateGroupMemberRoleDto, GroupMember>();

        // Organization
        CreateMap<Organization, OrganizationDto>()
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorHandle, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Handle : null))
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundColor : null))
            .ForMember(dest => dest.ActorBackgroundEffect, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundEffect : null))
            .ForMember(dest => dest.ActorBannerColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerColor : null))
            .ForMember(dest => dest.ActorBannerPictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerPictureId : null))
            .ForMember(dest => dest.ActorBannerPictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BannerPicture != null ? src.Actor.BannerPicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundImageId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundImageId : null))
            .ForMember(dest => dest.ActorBackgroundImageUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BackgroundImage != null ? src.Actor.BackgroundImage.Uri : null));
        CreateMap<Organization, OrganizationListDto>()
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundColor : null))
            .ForMember(dest => dest.ActorBackgroundEffect, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundEffect : null))
            .ForMember(dest => dest.ActorBannerColor, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerColor : null))
            .ForMember(dest => dest.ActorBannerPictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BannerPictureId : null))
            .ForMember(dest => dest.ActorBannerPictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BannerPicture != null ? src.Actor.BannerPicture.Uri : null))
            .ForMember(dest => dest.ActorBackgroundImageId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.BackgroundImageId : null))
            .ForMember(dest => dest.ActorBackgroundImageUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.BackgroundImage != null ? src.Actor.BackgroundImage.Uri : null));
        CreateMap<CreateOrganizationDto, Organization>()
            .ConstructUsing(src => new Organization
            {
                Pii = new OrganizationPii { FullName = src.FullName },
                ApprovalStatus = null!,
                Tenant = null!
            });
        CreateMap<UpdateOrganizationApprovalStatusDto, Organization>();

        // Organization Member
        CreateMap<OrganizationMember, OrganizationMemberDto>()
            .ForMember(dest => dest.OrganizationFullName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.FullName : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.FullName : null))
            .ForMember(dest => dest.OrganizationPositionFullName, opt => opt.MapFrom(src => src.OrganizationPosition != null ? src.OrganizationPosition.FullName : null));
        CreateMap<AddOrganizationMemberDto, OrganizationMember>();
        CreateMap<UpdateOrganizationMemberRoleDto, OrganizationMember>();

        CreateMap<OrganizationMember, OrganizationInvitationDto>()
            .ForMember(dest => dest.OrganizationId, opt => opt.MapFrom(src => src.OrganizationId))
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.FullName : null))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => (Explore.Domain.Enums.RoleEnum)src.RoleId))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));

        // Approval Status
        CreateMap<ApprovalStatus, StatusTypeListDto>().ReverseMap();

        // Organization Review
        CreateMap<OrganizationReview, OrganizationReviewDto>()
            .ForMember(dest => dest.OrganizationFullName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.FullName : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null));
        CreateMap<CreateOrganizationReviewDto, OrganizationReview>();
    }
}
