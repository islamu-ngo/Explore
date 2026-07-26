// ABOUTME: AutoMapper profile for Actor, ActorKeyStore, StorageObject, IndexedDid, and SyncState entities.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using AutoMapper;
using Explore.Application.DTOs.Actor;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.SyncState;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class ActorFederationMappingProfile : Profile
{
    public ActorFederationMappingProfile()
    {
        CreateMap<Domain.Actor, ActorDto>()
            .ForMember(dest => dest.ActorTypeMasterCode, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.MasterCode : null))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.FullName : null))
            .ForMember(dest => dest.Did, opt => opt.MapFrom(src => src.AtprotoIdentities.Select(identity => identity.Did).FirstOrDefault()))
            .ForMember(dest => dest.Handle, opt => opt.MapFrom(src => src.AtprotoIdentities.Select(identity => identity.Handle).FirstOrDefault()))
            .ForMember(dest => dest.PdsHost, opt => opt.MapFrom(src => src.AtprotoIdentities.Select(identity => identity.PdsHost).FirstOrDefault()))
            .ForMember(dest => dest.IndexedAt, opt => opt.MapFrom(src => src.AtprotoIdentities.Select(identity => (DateTime?)identity.LastResolvedAt).FirstOrDefault()))
            .ForMember(dest => dest.DidCustodyTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.DidCustodyTypeMasterCode, opt => opt.Ignore())
            .ForMember(dest => dest.DidCustodyTypeFullName, opt => opt.Ignore())
            .ForMember(dest => dest.ProfilePictureId, opt => opt.Ignore())
            .ForMember(dest => dest.BannerPictureId, opt => opt.Ignore())
            .ForMember(dest => dest.BackgroundImageId, opt => opt.Ignore())
            .ForMember(dest => dest.BackgroundImageUri, opt => opt.Ignore());
        CreateMap<Domain.Actor, ActorListDto>()
            .ForMember(dest => dest.ActorTypeMasterCode, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.MasterCode : null))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.FullName : null))
            .ForMember(dest => dest.Did, opt => opt.MapFrom(src => src.AtprotoIdentities.Select(identity => identity.Did).FirstOrDefault()))
            .ForMember(dest => dest.Handle, opt => opt.MapFrom(src => src.AtprotoIdentities.Select(identity => identity.Handle).FirstOrDefault()))
            .ForMember(dest => dest.PdsHost, opt => opt.MapFrom(src => src.AtprotoIdentities.Select(identity => identity.PdsHost).FirstOrDefault()))
            .ForMember(dest => dest.IndexedAt, opt => opt.MapFrom(src => src.AtprotoIdentities.Select(identity => (DateTime?)identity.LastResolvedAt).FirstOrDefault()))
            .ForMember(dest => dest.DidCustodyTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.DidCustodyTypeMasterCode, opt => opt.Ignore())
            .ForMember(dest => dest.DidCustodyTypeFullName, opt => opt.Ignore())
            .ForMember(dest => dest.ProfilePictureId, opt => opt.Ignore())
            .ForMember(dest => dest.BannerPictureId, opt => opt.Ignore())
            .ForMember(dest => dest.BackgroundImageId, opt => opt.Ignore())
            .ForMember(dest => dest.BackgroundImageUri, opt => opt.Ignore());
        CreateMap<CreateActorDto, Domain.Actor>();

        CreateMap<Domain.ActorKeyStore, ActorKeyStoreDto>()
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorDid, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.AtprotoIdentities.Select(identity => identity.Did).FirstOrDefault() : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null));
        CreateMap<Domain.ActorKeyStore, ActorKeyStoreListDto>();
        CreateMap<CreateActorKeyStoreDto, Domain.ActorKeyStore>();
        CreateMap<UpdateActorKeyStoreDto, Domain.ActorKeyStore>();

        CreateMap<Domain.StorageObject, StorageObjectDto>()
            .ForMember(dest => dest.FileTypeFullName, opt => opt.MapFrom(src => src.FileType != null ? src.FileType.FullName : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null));
        CreateMap<Domain.StorageObject, StorageObjectListDto>()
            .ForMember(dest => dest.FileTypeFullName, opt => opt.MapFrom(src => src.FileType != null ? src.FileType.FullName : null));
        CreateMap<CreateStorageObjectDto, Domain.StorageObject>()
            .ForMember(dest => dest.SafeDisplayName, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.SafeDisplayName) ? src.FullName : src.SafeDisplayName));
        CreateMap<UpdateStorageObjectDto, Domain.StorageObject>()
            .ForMember(dest => dest.SafeDisplayName, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.SafeDisplayName) ? src.FullName : src.SafeDisplayName));

        CreateMap<Domain.SyncState, SyncStateDto>().ReverseMap();
        CreateMap<Domain.SyncState, SyncStateListDto>().ReverseMap();
        CreateMap<CreateSyncStateDto, Domain.SyncState>();
        CreateMap<UpdateSyncStateDto, Domain.SyncState>();
    }
}
