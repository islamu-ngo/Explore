// ABOUTME: AutoMapper profile for Actor, ActorKeyStore, StorageObject, IndexedDid, SyncState, and AtprotoRecord entities.
// ABOUTME: Split from monolithic MappingProfile.cs for domain-cohesion.

using AutoMapper;
using Explore.Application.DTOs.Actor;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.DTOs.IndexedDid;
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
            .ForMember(dest => dest.DidCustodyTypeMasterCode, opt => opt.MapFrom(src => src.DidCustodyType != null ? src.DidCustodyType.MasterCode : null))
            .ForMember(dest => dest.DidCustodyTypeFullName, opt => opt.MapFrom(src => src.DidCustodyType != null ? src.DidCustodyType.FullName : null))
            .ForMember(dest => dest.BackgroundImageUri, opt => opt.MapFrom(src => src.BackgroundImage != null ? src.BackgroundImage.Uri : null));
        CreateMap<Domain.Actor, ActorListDto>()
            .ForMember(dest => dest.ActorTypeMasterCode, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.MasterCode : null))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.FullName : null))
            .ForMember(dest => dest.DidCustodyTypeMasterCode, opt => opt.MapFrom(src => src.DidCustodyType != null ? src.DidCustodyType.MasterCode : null))
            .ForMember(dest => dest.DidCustodyTypeFullName, opt => opt.MapFrom(src => src.DidCustodyType != null ? src.DidCustodyType.FullName : null))
            .ForMember(dest => dest.BackgroundImageUri, opt => opt.MapFrom(src => src.BackgroundImage != null ? src.BackgroundImage.Uri : null));
        CreateMap<CreateActorDto, Domain.Actor>();
        CreateMap<UpdateActorDto, Domain.Actor>();

        CreateMap<Domain.ActorKeyStore, ActorKeyStoreDto>()
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorDid, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Did : null))
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
        CreateMap<CreateStorageObjectDto, Domain.StorageObject>();
        CreateMap<UpdateStorageObjectDto, Domain.StorageObject>();

        CreateMap<Domain.IndexedDid, IndexedDidDto>().ReverseMap();
        CreateMap<Domain.IndexedDid, IndexedDidListDto>().ReverseMap();
        CreateMap<CreateIndexedDidDto, Domain.IndexedDid>();
        CreateMap<UpdateIndexedDidDto, Domain.IndexedDid>();

        CreateMap<Domain.SyncState, SyncStateDto>().ReverseMap();
        CreateMap<Domain.SyncState, SyncStateListDto>().ReverseMap();
        CreateMap<CreateSyncStateDto, Domain.SyncState>();
        CreateMap<UpdateSyncStateDto, Domain.SyncState>();

        CreateMap<Domain.AtprotoRecord, AtprotoRecordDto>().ReverseMap();
        CreateMap<Domain.AtprotoRecord, AtprotoRecordListDto>();
        CreateMap<CreateAtprotoRecordDto, Domain.AtprotoRecord>();
        CreateMap<UpdateAtprotoRecordDto, Domain.AtprotoRecord>();
    }
}
