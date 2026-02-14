using AutoMapper;
using Explore.Application.DTOs.Actor;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.DTOs.ActorType;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.DTOs.AudienceAge;
using Explore.Application.DTOs.AudienceGender;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.DidCustodyType;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.DTOs.EventFormat;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.DTOs.EventStatus;
using Explore.Application.DTOs.EventTags;
using Explore.Application.DTOs.EventType;
using Explore.Application.DTOs.FileType;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.DTOs.Language;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.Madhab;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.OrganizationPosition;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.RegistrationMode;
using Explore.Application.DTOs.StatusType;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.SyncState;
using Explore.Application.DTOs.Tag;
using Explore.Application.DTOs.TagType;
using Explore.Application.DTOs.TagTypeTags;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.DTOs.User;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.DTOs.VisibilityType;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ============================================
        // TENANT MAPPINGS
        // ============================================
        CreateMap<Tenant, TenantDto>().ReverseMap();
        CreateMap<Tenant, TenantListDto>().ReverseMap();
        CreateMap<CreateTenantDto, Tenant>();
        CreateMap<UpdateTenantDto, Tenant>();

        // ============================================
        // TENANT USER MAPPINGS
        // ============================================
        CreateMap<TenantUser, TenantUserDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.FullName : null));
        CreateMap<TenantUser, TenantUserListDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.FullName : null));
        CreateMap<CreateTenantUserDto, TenantUser>();
        CreateMap<UpdateTenantUserDto, TenantUser>();

        // ============================================
        // TENANT SETTINGS MAPPINGS
        // ============================================
        CreateMap<TenantSettings, TenantSettingsDto>()
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null));
        CreateMap<TenantSettings, TenantSettingsListDto>()
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null));
        CreateMap<CreateTenantSettingsDto, TenantSettings>();
        CreateMap<UpdateTenantSettingsDto, TenantSettings>();

        // ============================================
        // AUDIENCE MAPPINGS (Readonly Lookups)
        // ============================================
        CreateMap<AudienceAge, AudienceAgeDto>().ReverseMap();
        CreateMap<AudienceAge, AudienceAgeListDto>().ReverseMap();
        CreateMap<AudienceGender, AudienceGenderDto>().ReverseMap();
        CreateMap<AudienceGender, AudienceGenderListDto>().ReverseMap();

        // ============================================
        // EVENT MAPPINGS
        // ============================================
        CreateMap<Event, EventDto>()
            // Event Type
            .ForMember(dest => dest.EventTypeFullName, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.FullName : null))
            .ForMember(dest => dest.EventTypeMasterCode, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.MasterCode : null))
            // Audience Gender
            .ForMember(dest => dest.AudienceGenderFullName, opt => opt.MapFrom(src => src.AudienceGender != null ? src.AudienceGender.FullName : null))
            .ForMember(dest => dest.AudienceGenderMasterCode, opt => opt.MapFrom(src => src.AudienceGender != null ? src.AudienceGender.MasterCode : null))
            // Audience Age
            .ForMember(dest => dest.AudienceAgeFullName, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.FullName : null))
            .ForMember(dest => dest.AudienceAgeMasterCode, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MasterCode : null))
            .ForMember(dest => dest.AudienceAgeMinAge, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MinAge : (int?)null))
            .ForMember(dest => dest.AudienceAgeMaxAge, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MaxAge : (int?)null))
            // Actor
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorHandle, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Handle : null))
            .ForMember(dest => dest.ActorDid, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Did : null))
            .ForMember(dest => dest.ActorTypeId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ActorTypeId : 0))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ActorType != null ? src.Actor.ActorType.FullName : null))
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null))
            // Featured Image
            .ForMember(dest => dest.FeaturedImageUri, opt => opt.MapFrom(src => src.FeaturedImage != null ? src.FeaturedImage.Uri : null))
            // Event Status
            .ForMember(dest => dest.EventStatusFullName, opt => opt.MapFrom(src => src.EventStatus != null ? src.EventStatus.FullName : null))
            .ForMember(dest => dest.EventStatusMasterCode, opt => opt.MapFrom(src => src.EventStatus != null ? src.EventStatus.MasterCode : null))
            // Visibility Type
            .ForMember(dest => dest.VisibilityTypeFullName, opt => opt.MapFrom(src => src.VisibilityType != null ? src.VisibilityType.FullName : null))
            .ForMember(dest => dest.VisibilityTypeMasterCode, opt => opt.MapFrom(src => src.VisibilityType != null ? src.VisibilityType.MasterCode : null))
            // Event Format
            .ForMember(dest => dest.EventFormatFullName, opt => opt.MapFrom(src => src.EventFormat != null ? src.EventFormat.FullName : null))
            .ForMember(dest => dest.EventFormatMasterCode, opt => opt.MapFrom(src => src.EventFormat != null ? src.EventFormat.MasterCode : null))
            // Madhab
            .ForMember(dest => dest.MadhabFullName, opt => opt.MapFrom(src => src.Madhab != null ? src.Madhab.FullName : null))
            .ForMember(dest => dest.MadhabMasterCode, opt => opt.MapFrom(src => src.Madhab != null ? src.Madhab.MasterCode : null))
            // ATProto Record
            .ForMember(dest => dest.AtprotoRecordUri, opt => opt.MapFrom(src => src.AtprotoRecord != null ? src.AtprotoRecord.Uri : null))
            .ForMember(dest => dest.AtprotoRecordCid, opt => opt.MapFrom(src => src.AtprotoRecord != null ? src.AtprotoRecord.Cid : null))
            // Aspects
            .ForMember(dest => dest.AvailableAspects, opt => opt.MapFrom(src => GetAvailableAspects(src)))
            .ForMember(dest => dest.IslamicAspect, opt => opt.MapFrom(src => src.IslamicAspect))
            .ForMember(dest => dest.TechAspect, opt => opt.MapFrom(src => src.TechAspect));

        CreateMap<Event, EventListDto>()
            // Event Type
            .ForMember(dest => dest.EventTypeFullName, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.FullName : null))
            // Audience Gender
            .ForMember(dest => dest.AudienceGenderFullName, opt => opt.MapFrom(src => src.AudienceGender != null ? src.AudienceGender.FullName : null))
            // Audience Age
            .ForMember(dest => dest.AudienceAgeFullName, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.FullName : null))
            .ForMember(dest => dest.AudienceAgeMinAge, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MinAge : (int?)null))
            .ForMember(dest => dest.AudienceAgeMaxAge, opt => opt.MapFrom(src => src.AudienceAge != null ? src.AudienceAge.MaxAge : (int?)null))
            // Actor
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorTypeId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ActorTypeId : 0))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ActorType != null ? src.Actor.ActorType.FullName : null))
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : (Guid?)null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null))
            // Featured Image
            .ForMember(dest => dest.FeaturedImageUri, opt => opt.MapFrom(src => src.FeaturedImage != null ? src.FeaturedImage.Uri : null))
            // Event Status
            .ForMember(dest => dest.EventStatusFullName, opt => opt.MapFrom(src => src.EventStatus != null ? src.EventStatus.FullName : null))
            // Visibility Type
            .ForMember(dest => dest.VisibilityTypeFullName, opt => opt.MapFrom(src => src.VisibilityType != null ? src.VisibilityType.FullName : null))
            // Event Format
            .ForMember(dest => dest.EventFormatFullName, opt => opt.MapFrom(src => src.EventFormat != null ? src.EventFormat.FullName : null))
            // Madhab
            .ForMember(dest => dest.MadhabFullName, opt => opt.MapFrom(src => src.Madhab != null ? src.Madhab.FullName : null));

        CreateMap<CreateEventDto, Event>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ActorId, opt => opt.Ignore()) // Resolved by handler
            .ForMember(dest => dest.TotalViews, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore()) // Set by handler
            .ForMember(dest => dest.SessionCount, opt => opt.Ignore())
            .ForMember(dest => dest.AtprotoRecordId, opt => opt.Ignore())
            .ForMember(dest => dest.IsUserReported, opt => opt.Ignore()) // Set by handler based on OrganizationId
                                                                         // Convert DateTimeOffset? to DateOnly? for session dates
            .ForMember(dest => dest.FirstSessionDate, opt => opt.MapFrom(src =>
                src.FirstSessionDate.HasValue ? DateOnly.FromDateTime(src.FirstSessionDate.Value.DateTime) : (DateOnly?)null))
            .ForMember(dest => dest.LastSessionDate, opt => opt.MapFrom(src =>
                src.LastSessionDate.HasValue ? DateOnly.FromDateTime(src.LastSessionDate.Value.DateTime) : (DateOnly?)null));
        CreateMap<UpdateEventDto, Event>();

        // ============================================
        // EVENT TYPE MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<EventType, EventTypeListDto>().ReverseMap();

        // ============================================
        // ORGANIZATION MAPPINGS
        // ============================================
        CreateMap<Organization, OrganizationDto>()
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorHandle, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Handle : null))
            // Profile Picture
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null));
        CreateMap<Organization, OrganizationListDto>()
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            // Profile Picture
            .ForMember(dest => dest.ActorProfilePictureId, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureId : null))
            .ForMember(dest => dest.ActorProfilePictureUri, opt => opt.MapFrom(src => src.Actor != null && src.Actor.ProfilePicture != null ? src.Actor.ProfilePicture.Uri : null));
        CreateMap<CreateOrganizationDto, Organization>();
        CreateMap<UpdateOrganizationDto, Organization>();
        CreateMap<UpdateOrganizationApprovalStatusDto, Organization>();

        // ============================================
        // ORGANIZATION MEMBER MAPPINGS
        // ============================================
        CreateMap<OrganizationMember, OrganizationMemberDto>()
            .ForMember(dest => dest.OrganizationFullName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.FullName : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.FullName : null))
            .ForMember(dest => dest.OrganizationPositionFullName, opt => opt.MapFrom(src => src.OrganizationPosition != null ? src.OrganizationPosition.FullName : null));
        CreateMap<AddOrganizationMemberDto, OrganizationMember>();
        CreateMap<UpdateOrganizationMemberRoleDto, OrganizationMember>();

        // Mapping for invitation DTO used by GetMyInvitations
        CreateMap<OrganizationMember, OrganizationInvitationDto>()
            .ForMember(dest => dest.OrganizationId, opt => opt.MapFrom(src => src.OrganizationId))
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.FullName : null))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => (Explore.Domain.Enums.RoleEnum)src.RoleId))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));

        // ============================================
        // APPROVAL STATUS MAPPINGS
        // ============================================
        CreateMap<ApprovalStatus, StatusTypeListDto>().ReverseMap();

        // ============================================
        // USER MAPPINGS
        // ============================================
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorHandle, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Handle : null));
        CreateMap<UpdateUserDto, User>();

        // ============================================
        // ORGANIZATION REVIEW MAPPINGS
        // ============================================
        CreateMap<OrganizationReview, OrganizationReviewDto>()
            .ForMember(dest => dest.OrganizationFullName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.FullName : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null));
        CreateMap<CreateOrganizationReviewDto, OrganizationReview>();

        // ============================================
        // EVENT SESSION MAPPINGS
        // ============================================
        CreateMap<EventSession, EventSessionDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.LocationFullName, opt => opt.MapFrom(src => src.Location != null ? src.Location.FullName : null));
        CreateMap<EventSession, EventSessionListDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.LocationFullName, opt => opt.MapFrom(src => src.Location != null ? src.Location.FullName : null));
        CreateMap<CreateEventSessionDto, EventSession>();
        CreateMap<UpdateEventSessionDto, EventSession>();

        // ============================================
        // LOCATION MAPPINGS
        // ============================================
        CreateMap<Location, LocationDto>().ReverseMap();
        CreateMap<Location, LocationListDto>().ReverseMap();
        CreateMap<CreateLocationDto, Location>();
        CreateMap<UpdateLocationDto, Location>();

        // ============================================
        // CATEGORY MAPPINGS
        // ============================================
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.ParentFullName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : null));
        CreateMap<Category, CategoryListDto>()
            .ForMember(dest => dest.ParentFullName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : null));
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();

        // ============================================
        // TAG MAPPINGS
        // ============================================
        CreateMap<Tag, TagDto>().ReverseMap();
        CreateMap<Tag, TagListDto>();
        CreateMap<CreateTagDto, Tag>();
        CreateMap<UpdateTagDto, Tag>();

        // ============================================
        // EVENT SESSION AGENDA ITEM MAPPINGS
        // ============================================
        CreateMap<EventSessionAgendaItem, EventSessionAgendaItemDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null));
        CreateMap<EventSessionAgendaItem, EventSessionAgendaItemListDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null));
        CreateMap<CreateEventSessionAgendaItemDto, EventSessionAgendaItem>();
        CreateMap<UpdateEventSessionAgendaItemDto, EventSessionAgendaItem>();

        // ============================================
        // EVENT SESSION SPEAKER MAPPINGS
        // ============================================
        CreateMap<EventSessionSpeaker, EventSessionSpeakerDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null));
        CreateMap<EventSessionSpeaker, EventSessionSpeakerListDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null));
        CreateMap<CreateEventSessionSpeakerDto, EventSessionSpeaker>();
        CreateMap<UpdateEventSessionSpeakerDto, EventSessionSpeaker>();

        // ============================================
        // LANGUAGE MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<Language, LanguageDto>().ReverseMap();
        CreateMap<Language, LanguageListDto>().ReverseMap();

        // ============================================
        // EVENT SESSION LANGUAGE MAPPINGS (Link Table)
        // ============================================
        CreateMap<EventSessionLanguage, EventSessionLanguageDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.LanguageFullName, opt => opt.MapFrom(src => src.Language != null ? src.Language.FullName : null))
            .ForMember(dest => dest.LanguageMasterCode, opt => opt.MapFrom(src => src.Language != null ? src.Language.MasterCode : null));
        CreateMap<EventSessionLanguage, EventSessionLanguageListDto>();
        CreateMap<CreateEventSessionLanguageDto, EventSessionLanguage>();
        CreateMap<UpdateEventSessionLanguageDto, EventSessionLanguage>();

        // ============================================
        // TAG TYPE MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<TagType, TagTypeDto>().ReverseMap();
        CreateMap<TagType, TagTypeListDto>().ReverseMap();

        // ============================================
        // TAG TYPE TAGS MAPPINGS (Link Table)
        // ============================================
        CreateMap<Domain.TagTypeTags, TagTypeTagsDto>()
            .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
            .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null))
            .ForMember(dest => dest.TagTypeFullName, opt => opt.MapFrom(src => src.TagType != null ? src.TagType.FullName : null))
            .ForMember(dest => dest.TagTypeMasterCode, opt => opt.MapFrom(src => src.TagType != null ? src.TagType.MasterCode : null));
        CreateMap<Domain.TagTypeTags, TagTypeTagsListDto>()
            .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
            .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null))
            .ForMember(dest => dest.TagTypeFullName, opt => opt.MapFrom(src => src.TagType != null ? src.TagType.FullName : null))
            .ForMember(dest => dest.TagTypeMasterCode, opt => opt.MapFrom(src => src.TagType != null ? src.TagType.MasterCode : null));
        CreateMap<CreateTagTypeTagsDto, Domain.TagTypeTags>();
        CreateMap<UpdateTagTypeTagsDto, Domain.TagTypeTags>();

        // ============================================
        // EVENT TAGS MAPPINGS (Link Table)
        // ============================================
        CreateMap<EventTags, EventTagsDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
            .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null));
        CreateMap<EventTags, EventTagsListDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.TagFullName, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.FullName : null))
            .ForMember(dest => dest.TagMasterCode, opt => opt.MapFrom(src => src.Tag != null ? src.Tag.MasterCode : null));
        CreateMap<CreateEventTagsDto, EventTags>();
        CreateMap<UpdateEventTagsDto, EventTags>();

        // ============================================
        // EVENT CATEGORIES MAPPINGS (Link Table)
        // ============================================
        CreateMap<EventCategories, EventCategoriesDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.CategoryFullName, opt => opt.MapFrom(src => src.Category != null ? src.Category.FullName : null));
        CreateMap<EventCategories, EventCategoriesListDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null))
            .ForMember(dest => dest.CategoryFullName, opt => opt.MapFrom(src => src.Category != null ? src.Category.FullName : null));
        CreateMap<CreateEventCategoriesDto, EventCategories>();
        CreateMap<UpdateEventCategoriesDto, EventCategories>();

        // ============================================
        // EVENT REGISTRATION MAPPINGS
        // ============================================
        CreateMap<EventRegistration, EventRegistrationDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null));
        CreateMap<EventRegistration, EventRegistrationListDto>()
            .ForMember(dest => dest.EventSessionTitle, opt => opt.MapFrom(src => src.EventSession != null ? src.EventSession.Title : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.ApprovalStatusFullName, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.FullName : null))
            .ForMember(dest => dest.ApprovalStatusMasterCode, opt => opt.MapFrom(src => src.ApprovalStatus != null ? src.ApprovalStatus.MasterCode : null));
        CreateMap<CreateEventRegistrationDto, EventRegistration>();
        CreateMap<UpdateEventRegistrationDto, EventRegistration>();

        // ============================================
        // REGISTRATION MODE MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<RegistrationMode, RegistrationModeDto>().ReverseMap();
        CreateMap<RegistrationMode, RegistrationModeListDto>().ReverseMap();

        // ============================================
        // MADHAB MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<Madhab, MadhabDto>().ReverseMap();
        CreateMap<Madhab, MadhabListDto>().ReverseMap();

        // ============================================
        // EVENT STATUS MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<Domain.EventStatus, EventStatusDto>().ReverseMap();
        CreateMap<Domain.EventStatus, EventStatusListDto>().ReverseMap();

        // ============================================
        // EVENT FORMAT MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<EventFormat, EventFormatDto>().ReverseMap();
        CreateMap<EventFormat, EventFormatListDto>().ReverseMap();

        // ============================================
        // VISIBILITY TYPE MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<VisibilityType, VisibilityTypeDto>().ReverseMap();
        CreateMap<VisibilityType, VisibilityTypeListDto>().ReverseMap();

        // ============================================
        // ACTOR TYPE MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<Domain.ActorType, ActorTypeDto>().ReverseMap();
        CreateMap<Domain.ActorType, ActorTypeListDto>().ReverseMap();

        // ============================================
        // DID CUSTODY TYPE MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<Domain.DidCustodyType, DidCustodyTypeDto>().ReverseMap();
        CreateMap<Domain.DidCustodyType, DidCustodyTypeListDto>().ReverseMap();

        // ============================================
        // ACTOR MAPPINGS
        // ============================================
        CreateMap<Domain.Actor, ActorDto>()
            .ForMember(dest => dest.ActorTypeMasterCode, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.MasterCode : null))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.FullName : null))
            .ForMember(dest => dest.DidCustodyTypeMasterCode, opt => opt.MapFrom(src => src.DidCustodyType != null ? src.DidCustodyType.MasterCode : null))
            .ForMember(dest => dest.DidCustodyTypeFullName, opt => opt.MapFrom(src => src.DidCustodyType != null ? src.DidCustodyType.FullName : null));
        CreateMap<Domain.Actor, ActorListDto>()
            .ForMember(dest => dest.ActorTypeMasterCode, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.MasterCode : null))
            .ForMember(dest => dest.ActorTypeFullName, opt => opt.MapFrom(src => src.ActorType != null ? src.ActorType.FullName : null))
            .ForMember(dest => dest.DidCustodyTypeMasterCode, opt => opt.MapFrom(src => src.DidCustodyType != null ? src.DidCustodyType.MasterCode : null))
            .ForMember(dest => dest.DidCustodyTypeFullName, opt => opt.MapFrom(src => src.DidCustodyType != null ? src.DidCustodyType.FullName : null));
        CreateMap<CreateActorDto, Domain.Actor>();
        CreateMap<UpdateActorDto, Domain.Actor>();

        // ============================================
        // ORGANIZATION POSITION MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<Domain.OrganizationPosition, OrganizationPositionDto>().ReverseMap();
        CreateMap<Domain.OrganizationPosition, OrganizationPositionListDto>().ReverseMap();

        // ============================================
        // UNIFIED ROLE MAPPINGS
        // ============================================
        CreateMap<Domain.Role, DTOs.Role.RoleDto>();
        CreateMap<Domain.Role, DTOs.Role.RoleListDto>();

        // ============================================
        // PERMISSION MAPPINGS
        // ============================================
        CreateMap<Domain.Permission, DTOs.Permission.PermissionDto>();
        CreateMap<Domain.Permission, DTOs.Permission.PermissionListDto>();

        // ============================================
        // USER AUTHENTICATION TOKEN MAPPINGS
        // ============================================
        CreateMap<Domain.UserAuthenticationToken, UserAuthenticationTokenDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null));
        CreateMap<Domain.UserAuthenticationToken, UserAuthenticationTokenListDto>();
        CreateMap<CreateUserAuthenticationTokenDto, Domain.UserAuthenticationToken>();
        CreateMap<UpdateUserAuthenticationTokenDto, Domain.UserAuthenticationToken>();

        // ============================================
        // USER EXTERNAL LOGIN MAPPINGS
        // ============================================
        CreateMap<Domain.UserExternalLogin, UserExternalLoginDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null));
        CreateMap<Domain.UserExternalLogin, UserExternalLoginListDto>();
        CreateMap<CreateUserExternalLoginDto, Domain.UserExternalLogin>();
        CreateMap<UpdateUserExternalLoginDto, Domain.UserExternalLogin>();

        // ============================================
        // FILE TYPE MAPPINGS (Readonly Lookup)
        // ============================================
        CreateMap<Domain.FileType, FileTypeDto>().ReverseMap();
        CreateMap<Domain.FileType, FileTypeListDto>().ReverseMap();

        // ============================================
        // STORAGE OBJECT MAPPINGS
        // ============================================
        CreateMap<Domain.StorageObject, StorageObjectDto>()
            .ForMember(dest => dest.FileTypeFullName, opt => opt.MapFrom(src => src.FileType != null ? src.FileType.FullName : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null))
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null));
        CreateMap<Domain.StorageObject, StorageObjectListDto>()
            .ForMember(dest => dest.FileTypeFullName, opt => opt.MapFrom(src => src.FileType != null ? src.FileType.FullName : null));
        CreateMap<CreateStorageObjectDto, Domain.StorageObject>();
        CreateMap<UpdateStorageObjectDto, Domain.StorageObject>();

        // ============================================
        // ACTOR KEY STORE MAPPINGS
        // ============================================
        CreateMap<Domain.ActorKeyStore, ActorKeyStoreDto>()
            .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.DisplayName : null))
            .ForMember(dest => dest.ActorDid, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Did : null))
            .ForMember(dest => dest.TenantFullName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : null));
        CreateMap<Domain.ActorKeyStore, ActorKeyStoreListDto>();
        CreateMap<CreateActorKeyStoreDto, Domain.ActorKeyStore>();
        CreateMap<UpdateActorKeyStoreDto, Domain.ActorKeyStore>();

        // ============================================
        // INDEXED DID MAPPINGS
        // ============================================
        CreateMap<Domain.IndexedDid, IndexedDidDto>().ReverseMap();
        CreateMap<Domain.IndexedDid, IndexedDidListDto>().ReverseMap();
        CreateMap<CreateIndexedDidDto, Domain.IndexedDid>();
        CreateMap<UpdateIndexedDidDto, Domain.IndexedDid>();

        // ============================================
        // SYNC STATE MAPPINGS
        // ============================================
        CreateMap<Domain.SyncState, SyncStateDto>().ReverseMap();
        CreateMap<Domain.SyncState, SyncStateListDto>().ReverseMap();
        CreateMap<CreateSyncStateDto, Domain.SyncState>();
        CreateMap<UpdateSyncStateDto, Domain.SyncState>();

        // ============================================
        // ATPROTO RECORD MAPPINGS
        // ============================================
        CreateMap<Domain.AtprotoRecord, AtprotoRecordDto>().ReverseMap();
        CreateMap<Domain.AtprotoRecord, AtprotoRecordListDto>();
        CreateMap<CreateAtprotoRecordDto, Domain.AtprotoRecord>();
        CreateMap<UpdateAtprotoRecordDto, Domain.AtprotoRecord>();

        // ============================================
        // ASPECT MAPPINGS
        // ============================================
        CreateMap<EventIslamicAspect, EventIslamicAspectDto>()
            .ForMember(dest => dest.MadhabName, opt => opt.MapFrom(src => src.Madhab != null ? src.Madhab.FullName : null))
            .ForMember(dest => dest.PrimaryLanguageName, opt => opt.MapFrom(src => src.PrimaryLanguage != null ? src.PrimaryLanguage.FullName : null));

        CreateMap<CreateUpdateIslamicAspectDto, EventIslamicAspect>();

        CreateMap<EventTechAspect, EventTechAspectDto>();

        CreateMap<CreateUpdateTechAspectDto, EventTechAspect>();
    }

    /// <summary>
    /// Gets the list of available aspect types for an event.
    /// </summary>
    private static List<string> GetAvailableAspects(Event src)
    {
        var aspects = new List<string>();
        if (src.IslamicAspect != null) aspects.Add("Islamic");
        if (src.TechAspect != null) aspects.Add("Tech");
        return aspects;
    }
}
