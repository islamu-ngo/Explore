// ABOUTME: Explicit API-owned catalog of DTOs whose HAL OpenAPI schemas are public contract surface.
// ABOUTME: Keeps HAL schema opt-in out of Application DTOs and avoids broad namespace reflection.

namespace Explore.API.OpenApi;

internal static class HalOpenApiSchemaCatalog
{
    public static IReadOnlyList<Type> RegisteredDtoTypes { get; } =
    [
        // Event DTOs
        typeof(Explore.Application.DTOs.Event.EventDto),
        typeof(Explore.Application.DTOs.Event.EventListDto),

        // EventSession DTOs
        typeof(Explore.Application.DTOs.EventSession.EventSessionDto),
        typeof(Explore.Application.DTOs.EventSession.EventSessionListDto),

        // EventSessionGroup DTOs
        typeof(Explore.Application.DTOs.EventSessionGroup.EventSessionGroupDto),
        typeof(Explore.Application.DTOs.EventSessionGroup.EventSessionGroupListDto),

        // Category DTOs
        typeof(Explore.Application.DTOs.Category.CategoryDto),
        typeof(Explore.Application.DTOs.Category.CategoryListDto),

        // Tag DTOs
        typeof(Explore.Application.DTOs.Tag.TagDto),
        typeof(Explore.Application.DTOs.Tag.TagListDto),

        // Location DTOs
        typeof(Explore.Application.DTOs.Location.LocationDto),
        typeof(Explore.Application.DTOs.Location.LocationListDto),

        // Organization DTOs
        typeof(Explore.Application.DTOs.Organization.OrganizationDto),
        typeof(Explore.Application.DTOs.Organization.OrganizationListDto),

        // Actor DTOs
        typeof(Explore.Application.DTOs.Actor.ActorDto),
        typeof(Explore.Application.DTOs.Actor.ActorListDto),

        // Public metadata DTOs
        typeof(Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionDto),
        typeof(Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionListDto),
        typeof(Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionDto),
        typeof(Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionListDto),
        typeof(Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionDto),
        typeof(Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionListDto),
        typeof(Explore.Application.DTOs.EventTemplate.EventTemplateDto),
        typeof(Explore.Application.DTOs.EventTemplate.EventTemplateListDto),
        typeof(Explore.Application.DTOs.EventSessionTemplate.EventSessionTemplateDto),
        typeof(Explore.Application.DTOs.EventSessionTemplate.EventSessionTemplateListDto),
        typeof(Explore.Application.DTOs.Group.GroupDto),
        typeof(Explore.Application.DTOs.Group.GroupListDto),
        typeof(Explore.Application.DTOs.GroupMember.GroupMemberDto),
        typeof(Explore.Application.DTOs.OrganizationMember.OrganizationMemberDto),
        typeof(Explore.Application.DTOs.IndexedDid.IndexedDidDto),
        typeof(Explore.Application.DTOs.IndexedDid.IndexedDidListDto),
        typeof(Explore.Application.DTOs.LocationRoom.LocationRoomDto),
        typeof(Explore.Application.DTOs.LocationRoom.LocationRoomListDto),
        typeof(Explore.Application.DTOs.EventDay.EventDayDto),
        typeof(Explore.Application.DTOs.EventDay.EventDayListDto),
        typeof(Explore.Application.DTOs.EventAgendaItem.EventAgendaItemDto),
        typeof(Explore.Application.DTOs.EventAgendaItem.EventAgendaItemListDto),
        typeof(Explore.Application.DTOs.OrganizationReview.OrganizationReviewDto),

        // EventSessionSpeaker DTOs
        typeof(Explore.Application.DTOs.EventSessionSpeaker.EventSessionSpeakerDto),
        typeof(Explore.Application.DTOs.EventSessionSpeaker.EventSessionSpeakerListDto),

        // EventSessionLanguage DTOs
        typeof(Explore.Application.DTOs.EventSessionLanguage.EventSessionLanguageDto),
        typeof(Explore.Application.DTOs.EventSessionLanguage.EventSessionLanguageListDto),

        // EventAspects DTOs
        typeof(Explore.Application.DTOs.EventAspects.EventIslamicAspectDto),
        typeof(Explore.Application.DTOs.EventAspects.EventTechAspectDto),

        // Template sync DTOs exposed through HAL diff resources
        typeof(Explore.Application.DTOs.EventTemplateSync.TemplateDiffDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.AddedDefinitionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.ModifiedDefinitionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.RetiredDefinitionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.AddedOptionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.ModifiedOptionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.RetiredOptionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.UntouchedLocalDefinitionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.FieldChangeDto),

        // Projection admin DTOs
        typeof(Explore.Application.DTOs.CustomPropertyProjection.ProjectionStatusDto),
        typeof(Explore.Application.DTOs.CustomPropertyProjection.ProjectionDirtyScopeDto),

        // Tenant settings DTOs
        typeof(Explore.Application.DTOs.TenantSettingsDocuments.TenantBrandingSettingsDocumentDto),
    ];

    public static IReadOnlyDictionary<string, Type> DetailResourceMappings { get; } = new Dictionary<string, Type>
    {
        ["HalResourceOfEventDto"] = typeof(Explore.Application.DTOs.Event.EventDto),
        ["HalResourceOfEventListDto"] = typeof(Explore.Application.DTOs.Event.EventListDto),
        ["HalResourceOfEventSessionDto"] = typeof(Explore.Application.DTOs.EventSession.EventSessionDto),
        ["HalResourceOfEventSessionListDto"] = typeof(Explore.Application.DTOs.EventSession.EventSessionListDto),
        ["HalResourceOfCategoryDto"] = typeof(Explore.Application.DTOs.Category.CategoryDto),
        ["HalResourceOfCategoryListDto"] = typeof(Explore.Application.DTOs.Category.CategoryListDto),
        ["HalResourceOfTagDto"] = typeof(Explore.Application.DTOs.Tag.TagDto),
        ["HalResourceOfTagListDto"] = typeof(Explore.Application.DTOs.Tag.TagListDto),
        ["HalResourceOfLocationDto"] = typeof(Explore.Application.DTOs.Location.LocationDto),
        ["HalResourceOfLocationListDto"] = typeof(Explore.Application.DTOs.Location.LocationListDto),
        ["HalResourceOfOrganizationDto"] = typeof(Explore.Application.DTOs.Organization.OrganizationDto),
        ["HalResourceOfOrganizationListDto"] = typeof(Explore.Application.DTOs.Organization.OrganizationListDto),
        ["HalResourceOfActorDto"] = typeof(Explore.Application.DTOs.Actor.ActorDto),
        ["HalResourceOfActorListDto"] = typeof(Explore.Application.DTOs.Actor.ActorListDto),
        ["HalResourceOfEventSessionGroupDto"] = typeof(Explore.Application.DTOs.EventSessionGroup.EventSessionGroupDto),
        ["HalResourceOfEventSessionGroupListDto"] = typeof(Explore.Application.DTOs.EventSessionGroup.EventSessionGroupListDto),
        ["HalResourceOfCustomPropertyDefinitionDto"] = typeof(Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionDto),
        ["HalResourceOfCustomPropertyDefinitionListDto"] = typeof(Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionListDto),
        ["HalResourceOfEventCustomPropertyDefinitionDto"] = typeof(Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionDto),
        ["HalResourceOfEventCustomPropertyDefinitionListDto"] = typeof(Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionListDto),
        ["HalResourceOfEventSessionCustomPropertyDefinitionDto"] = typeof(Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionDto),
        ["HalResourceOfEventSessionCustomPropertyDefinitionListDto"] = typeof(Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionListDto),
        ["HalResourceOfEventTemplateDto"] = typeof(Explore.Application.DTOs.EventTemplate.EventTemplateDto),
        ["HalResourceOfEventTemplateListDto"] = typeof(Explore.Application.DTOs.EventTemplate.EventTemplateListDto),
        ["HalResourceOfEventSessionTemplateDto"] = typeof(Explore.Application.DTOs.EventSessionTemplate.EventSessionTemplateDto),
        ["HalResourceOfEventSessionTemplateListDto"] = typeof(Explore.Application.DTOs.EventSessionTemplate.EventSessionTemplateListDto),
        ["HalResourceOfGroupDto"] = typeof(Explore.Application.DTOs.Group.GroupDto),
        ["HalResourceOfGroupListDto"] = typeof(Explore.Application.DTOs.Group.GroupListDto),
        ["HalResourceOfGroupMemberDto"] = typeof(Explore.Application.DTOs.GroupMember.GroupMemberDto),
        ["HalResourceOfOrganizationMemberDto"] = typeof(Explore.Application.DTOs.OrganizationMember.OrganizationMemberDto),
        ["HalResourceOfIndexedDidDto"] = typeof(Explore.Application.DTOs.IndexedDid.IndexedDidDto),
        ["HalResourceOfIndexedDidListDto"] = typeof(Explore.Application.DTOs.IndexedDid.IndexedDidListDto),
        ["HalResourceOfLocationRoomDto"] = typeof(Explore.Application.DTOs.LocationRoom.LocationRoomDto),
        ["HalResourceOfLocationRoomListDto"] = typeof(Explore.Application.DTOs.LocationRoom.LocationRoomListDto),
        ["HalResourceOfEventDayDto"] = typeof(Explore.Application.DTOs.EventDay.EventDayDto),
        ["HalResourceOfEventDayListDto"] = typeof(Explore.Application.DTOs.EventDay.EventDayListDto),
        ["HalResourceOfEventAgendaItemDto"] = typeof(Explore.Application.DTOs.EventAgendaItem.EventAgendaItemDto),
        ["HalResourceOfEventAgendaItemListDto"] = typeof(Explore.Application.DTOs.EventAgendaItem.EventAgendaItemListDto),
        ["HalResourceOfOrganizationReviewDto"] = typeof(Explore.Application.DTOs.OrganizationReview.OrganizationReviewDto),
        ["HalResourceOfTemplateDiffDto"] = typeof(Explore.Application.DTOs.EventTemplateSync.TemplateDiffDto),
        ["HalResourceOfProjectionStatusDto"] = typeof(Explore.Application.DTOs.CustomPropertyProjection.ProjectionStatusDto),
        ["HalResourceOfProjectionDirtyScopeDto"] = typeof(Explore.Application.DTOs.CustomPropertyProjection.ProjectionDirtyScopeDto),
        ["HalResourceOfTenantBrandingSettingsDocumentDto"] = typeof(Explore.Application.DTOs.TenantSettingsDocuments.TenantBrandingSettingsDocumentDto),
    };

    public static IReadOnlyDictionary<string, string> CollectionEmbeddedItemResourceMappings { get; } = new Dictionary<string, string>
    {
        ["HalCollectionEmbeddedOfActorListDto"] = "HalResourceOfActorListDto",
        ["HalCollectionEmbeddedOfCategoryListDto"] = "HalResourceOfCategoryListDto",
        ["HalCollectionEmbeddedOfCustomPropertyDefinitionListDto"] = "HalResourceOfCustomPropertyDefinitionListDto",
        ["HalCollectionEmbeddedOfEventAgendaItemListDto"] = "HalResourceOfEventAgendaItemListDto",
        ["HalCollectionEmbeddedOfEventCustomPropertyDefinitionListDto"] = "HalResourceOfEventCustomPropertyDefinitionListDto",
        ["HalCollectionEmbeddedOfEventDayListDto"] = "HalResourceOfEventDayListDto",
        ["HalCollectionEmbeddedOfEventListDto"] = "HalResourceOfEventListDto",
        ["HalCollectionEmbeddedOfEventSessionCustomPropertyDefinitionListDto"] = "HalResourceOfEventSessionCustomPropertyDefinitionListDto",
        ["HalCollectionEmbeddedOfEventSessionGroupListDto"] = "HalResourceOfEventSessionGroupListDto",
        ["HalCollectionEmbeddedOfEventSessionListDto"] = "HalResourceOfEventSessionListDto",
        ["HalCollectionEmbeddedOfEventSessionTemplateListDto"] = "HalResourceOfEventSessionTemplateListDto",
        ["HalCollectionEmbeddedOfEventTemplateListDto"] = "HalResourceOfEventTemplateListDto",
        ["HalCollectionEmbeddedOfGroupListDto"] = "HalResourceOfGroupListDto",
        ["HalCollectionEmbeddedOfGroupMemberDto"] = "HalResourceOfGroupMemberDto",
        ["HalCollectionEmbeddedOfOrganizationMemberDto"] = "HalResourceOfOrganizationMemberDto",
        ["HalCollectionEmbeddedOfIndexedDidListDto"] = "HalResourceOfIndexedDidListDto",
        ["HalCollectionEmbeddedOfLocationListDto"] = "HalResourceOfLocationListDto",
        ["HalCollectionEmbeddedOfLocationRoomListDto"] = "HalResourceOfLocationRoomListDto",
        ["HalCollectionEmbeddedOfOrganizationListDto"] = "HalResourceOfOrganizationListDto",
        ["HalCollectionEmbeddedOfOrganizationReviewDto"] = "HalResourceOfOrganizationReviewDto",
        ["HalCollectionEmbeddedOfProjectionStatusDto"] = "HalResourceOfProjectionStatusDto",
        ["HalCollectionEmbeddedOfProjectionDirtyScopeDto"] = "HalResourceOfProjectionDirtyScopeDto",
        ["HalCollectionEmbeddedOfTagListDto"] = "HalResourceOfTagListDto",
    };

    public static bool IsCatalogedDetailResourceSchema(string schemaName)
        => DetailResourceMappings.ContainsKey(schemaName);

    public static bool IsRegisteredDto(Type dtoType)
        => RegisteredDtoTypes.Contains(dtoType);
}
