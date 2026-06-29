#!/bin/bash
set -e

# Make sure we start clean (unstage everything)
git reset

# Commit 1
git add README.md
git commit -m "docs(config): document security fixes policy for forks"

# Commit 2
git add dev/active/event-lifecycle-validation-policy/
git add dev/active/full-property-update-sub-dto/
git add dev/next/event-lifecycle-validation-policy/
git add dev/next/full-property-update-sub-dto/
git commit -m "docs(dev): move validation policy and DTO planning to next queue"

# Commit 3
git add Explore.Domain/Enums/AiAdministrativeContextScopeEnum.cs \
        Explore.Domain/Enums/AiConsentGrantStatusEnum.cs \
        Explore.Domain/Enums/AiContextDisclosureRuleEnum.cs \
        Explore.Domain/Enums/AiContextSensitivityEnum.cs \
        Explore.Domain/Enums/AiProviderTrustTierEnum.cs \
        Explore.Domain/Enums/AiViewerScopeEnum.cs
git commit -m "feat(domain/ai): introduce AI context sensitivity and trust enums"

# Commit 4
git add Explore.Domain/Actor.cs \
        Explore.Domain/Category.cs \
        Explore.Domain/EventCategories.cs \
        Explore.Domain/EventRegistration.cs \
        Explore.Domain/EventSessionLanguage.cs \
        Explore.Domain/EventSessionSpeaker.cs \
        Explore.Domain/EventTags.cs \
        Explore.Domain/Location.cs \
        Explore.Domain/User.cs \
        Explore.Persistence/Configurations/Entities/ActorConfiguration.cs \
        Explore.Persistence/Configurations/Entities/CategoryConfiguration.cs \
        Explore.Persistence/Configurations/Entities/EventCategoriesConfiguration.cs \
        Explore.Persistence/Configurations/Entities/EventRegistrationConfiguration.cs \
        Explore.Persistence/Configurations/Entities/EventSessionLanguageConfiguration.cs \
        Explore.Persistence/Configurations/Entities/EventSessionSpeakerConfiguration.cs \
        Explore.Persistence/Configurations/Entities/EventTagsConfiguration.cs \
        Explore.Persistence/Configurations/Entities/LocationConfiguration.cs \
        Explore.Persistence/Configurations/Entities/UserConfiguration.cs \
        Explore.Persistence/ExploreDbContext.DbSets.cs \
        Explore.Persistence/ExploreDbContext.QueryFilters.cs \
        Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs \
        Explore.Persistence/PersistenceServicesRegistration.cs \
        Explore.Persistence/Repositories/EventCategoriesRepository.cs \
        Explore.Persistence/Repositories/EventRepository.cs \
        Explore.Persistence/Repositories/EventSessionLanguageRepository.cs \
        Explore.Persistence/Repositories/EventSessionRepository.cs \
        Explore.Persistence/Repositories/EventSessionSpeakerRepository.cs \
        Explore.Persistence/Repositories/EventTagsRepository.cs \
        Explore.Persistence/Migrations/20260627163325_AddUserConcurrencyStamp.Designer.cs \
        Explore.Persistence/Migrations/20260627163325_AddUserConcurrencyStamp.cs \
        Explore.Persistence/Migrations/20260627171506_AddActorConcurrencyStamp.Designer.cs \
        Explore.Persistence/Migrations/20260627171506_AddActorConcurrencyStamp.cs \
        Explore.Persistence/Migrations/20260627173748_AddCategoryConcurrencyStamp.Designer.cs \
        Explore.Persistence/Migrations/20260627173748_AddCategoryConcurrencyStamp.cs \
        Explore.Persistence/Migrations/20260627175930_AddLocationAuditAndConcurrencyStamp.Designer.cs \
        Explore.Persistence/Migrations/20260627175930_AddLocationAuditAndConcurrencyStamp.cs \
        Explore.Persistence/Migrations/20260628001608_AddEventRegistrationConcurrencyStamp.Designer.cs \
        Explore.Persistence/Migrations/20260628001608_AddEventRegistrationConcurrencyStamp.cs \
        Explore.Persistence/Migrations/20260628003400_AddEventSessionLanguageConcurrencyStamp.Designer.cs \
        Explore.Persistence/Migrations/20260628003400_AddEventSessionLanguageConcurrencyStamp.cs \
        Explore.Persistence/Migrations/20260628005046_AddEventCategoryAndTagConcurrencyStamps.Designer.cs \
        Explore.Persistence/Migrations/20260628005046_AddEventCategoryAndTagConcurrencyStamps.cs \
        Explore.Persistence/Migrations/20260628005759_AddEventSessionSpeakerConcurrencyStamp.Designer.cs \
        Explore.Persistence/Migrations/20260628005759_AddEventSessionSpeakerConcurrencyStamp.cs
git commit -m "feat(persistence/concurrency): add concurrency stamps to core entities and configurations"

# Commit 5
git add Explore.Domain/AiConsentGrant.cs \
        Explore.Application/Contracts/Persistence/IAiConsentGrantRepository.cs \
        Explore.Application/Features/AiAssistant/Disclosure/ \
        Explore.Persistence/EntityTypeConfigurations/ \
        Explore.Persistence/Repositories/AiConsentGrantRepository.cs \
        Event.Architecture.Tests/AiContextDisclosureSchemaTests.cs \
        Event.Architecture.Tests/AiContextGatewayBypassTests.cs \
        dev/active/ai-context-disclosure-policy/ \
        docs/AI_CONTEXT_SECURITY.md \
        docs/adr/ADR-012-ai-context-disclosure-policy.md
git commit -m "feat(app/ai): implement AI context disclosure policy and gateway"

# Commit 6
git add Explore.Application/Features/AiAssistant/Handlers/Commands/ProcessAiRunCommandHandler.cs \
        Event.Application.UnitTests/Features/AiAssistant/Commands/ProcessAiRunCommandHandlerTests.cs
git commit -m "feat(app/ai): route selected event reference context through AI gateway"

# Commit 7
git add Explore.Application/Features/Groups/GroupParentTarget.cs \
        Explore.Application/Features/Groups/Handlers/Commands/UpdateGroupCommandHandler.cs
git commit -m "refactor(app/groups): extract GroupParentTarget helper struct"

# Commit 8
git add Event.Architecture.Tests/NamingConventionTests.cs
git commit -m "test(test/architecture): check validator naming convention for generic classes"

# Commit 9
git add Explore.Application/DTOs/User/ \
        Explore.Application/Features/Users/ \
        Explore.Blazor.Client/Services/UserService.cs \
        Explore.Blazor.Client/Pages/User/ \
        Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs \
        Explore.API/Controllers/UserController.cs \
        Event.Application.UnitTests/Features/Users/ \
        Explore.Blazor.Client.Tests/Services/UserServiceTests.cs
git commit -m "refactor(app/users): implement PATCH updates for User details"

# Commit 10
git add Explore.Application/DTOs/Actor/ \
        Explore.Application/Features/Actors/ \
        Explore.Application/Profiles/ActorFederationMappingProfile.cs \
        Explore.API/Controllers/ActorController.cs \
        Explore.API/Hateoas/Policies/ActorLinkPolicy.cs \
        Explore.Blazor.Client/Services/AdminService.cs \
        Event.API.IntegrationTests/Features/ActorControllerTests.cs \
        Event.Application.UnitTests/Features/Actors/ \
        Explore.Blazor.Client.Tests/Services/AdminServiceTests.cs
git commit -m "refactor(app/actors): implement PATCH updates for Actor details"

# Commit 11
git add Explore.Application/DTOs/Category/ \
        Explore.Application/Features/Categories/ \
        Explore.Blazor.Client/Services/CategoryService.cs \
        Explore.Blazor.Client/Validators/UpdateCategoryDtoValidator.cs \
        Explore.Blazor.Client/Pages/Admin/Dialogs/EditCategoryDialog.razor \
        Explore.API/Controllers/CategoryController.cs \
        Explore.API/Hateoas/Policies/CategoryLinkPolicy.cs \
        Event.API.IntegrationTests/Features/CategoryControllerTests.cs \
        Event.Application.UnitTests/Features/Categories/ \
        Explore.Blazor.Client.Tests/Services/CategoryServiceTests.cs
git commit -m "refactor(app/categories): implement PATCH updates for Category details"

# Commit 12
git add Explore.Application/DTOs/Organization/ \
        Explore.Application/Features/Organizations/ \
        Explore.Application/Profiles/OrganizationMappingProfile.cs \
        Explore.API/Controllers/OrganizationController.cs \
        Explore.Blazor.Client/Services/OrganizationService.cs \
        Explore.Blazor.Client/Pages/Admin/Organization/ \
        Explore.Blazor.Client/Pages/Organizations/ \
        Event.API.IntegrationTests/Features/OrganizationControllerTests.cs \
        Event.Application.UnitTests/Features/Organizations/ \
        Explore.Blazor.Client.Tests/Services/OrganizationServiceTests.cs
git commit -m "refactor(app/organizations): implement PATCH updates for Organization approval and details"

# Commit 13
git add Explore.Application/DTOs/Group/ \
        Explore.Application/Features/Groups/ \
        Explore.API/Controllers/GroupController.cs \
        Explore.Blazor.Client/Services/GroupService.cs \
        Explore.Blazor.Client/Pages/Admin/Group/ \
        Event.API.IntegrationTests/Features/GroupControllerTests.cs \
        Event.Application.UnitTests/Features/Groups/
git commit -m "refactor(app/groups): implement PATCH updates for Group profile and hierarchy"

# Commit 14
git add Explore.Application/DTOs/Location/ \
        Explore.Application/DTOs/LocationRoom/ \
        Explore.Application/Features/Locations/ \
        Explore.Application/Features/LocationRooms/ \
        Explore.Blazor.Client/Services/LocationService.cs \
        Explore.Blazor.Client/Services/LocationRoomService.cs \
        Explore.Blazor.Client/Contracts/Services/Events/ILocationRoomService.cs \
        Explore.Blazor.Client/Validators/UpdateLocationDtoValidator.cs \
        Explore.Blazor.Client/Pages/Admin/Dialogs/EditLocationDialog.razor \
        Explore.Blazor.Client/Pages/Events/Components/LocationRoomEditorDialog.razor \
        Explore.Blazor.Client/Pages/Events/Components/LocationRoomManager.razor \
        Explore.API/Controllers/LocationController.cs \
        Explore.API/Controllers/LocationRoomController.cs \
        Explore.API/Hateoas/Policies/LocationLinkPolicy.cs \
        Explore.API/Hateoas/Policies/LocationRoomLinkPolicy.cs \
        Event.API.IntegrationTests/Features/LocationControllerTests.cs \
        Event.API.IntegrationTests/Features/LocationRoomControllerTests.cs \
        Event.Application.UnitTests/Features/Locations/ \
        Event.Application.UnitTests/Features/LocationRooms/ \
        Explore.Blazor.Client.Tests/Services/LocationServiceTests.cs \
        Explore.Blazor.Client.Tests/Services/LocationRoomServiceTests.cs
git commit -m "refactor(app/locations): implement PATCH updates for Location and LocationRoom"

# Commit 15
git add Explore.Application/DTOs/Event/ \
        Explore.Application/DTOs/EventSeries/ \
        Explore.Application/DTOs/EventDay/ \
        Explore.Application/DTOs/EventSession/ \
        Explore.Application/DTOs/EventAgendaItem/ \
        Explore.Application/Profiles/EventMappingProfile.cs \
        Explore.Application/Profiles/EventSessionMappingProfile.cs \
        Explore.Application/Profiles/CustomPropertyMappingProfile.cs \
        Explore.Application/Features/Events/ \
        Explore.Application/Features/EventSeries/ \
        Explore.Application/Features/EventDays/ \
        Explore.Application/Features/EventSessions/ \
        Explore.Application/Features/EventAgendaItems/ \
        Explore.API/Controllers/EventController.cs \
        Explore.API/Controllers/EventSeriesController.cs \
        Explore.API/Controllers/EventDayController.cs \
        Explore.API/Controllers/EventSessionController.cs \
        Explore.API/Controllers/EventAgendaItemController.cs \
        Explore.API/Hateoas/Policies/EventLinkPolicy.cs \
        Explore.API/Hateoas/Policies/EventDayLinkPolicy.cs \
        Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs \
        Explore.API/Hateoas/Policies/EventAgendaItemLinkPolicy.cs \
        Explore.API/Hateoas/RouteNames.cs \
        Explore.Blazor.Client/Contracts/Services/Events/IEventAgendaItemService.cs \
        Explore.Blazor.Client/Contracts/Services/Events/IEventDayService.cs \
        Explore.Blazor.Client/Contracts/Services/Events/IEventSeriesService.cs \
        Explore.Blazor.Client/Models/EventSessions/UpdateEventSessionRequest.cs \
        Explore.Blazor.Client/Pages/Events/Components/EventAgendaItemEditorDialog.razor \
        Explore.Blazor.Client/Pages/Events/Components/EventDayEditorDialog.razor \
        Explore.Blazor.Client/Pages/Events/EventDetail.razor \
        Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs \
        Explore.Blazor.Client/Pages/Events/EventEdit.razor.cs \
        Explore.Blazor.Client/Pages/Events/Sessions/EventSessionFormModelMapper.cs \
        Explore.Blazor.Client/Services/EventService.cs \
        Explore.Blazor.Client/Services/EventSeriesService.cs \
        Explore.Blazor.Client/Services/EventDayService.cs \
        Explore.Blazor.Client/Services/EventAgendaItemService.cs \
        Event.API.IntegrationTests/Features/EventControllerRealRuntimeTests.cs \
        Event.API.IntegrationTests/Features/EventSessionControllerTests.cs \
        Event.API.IntegrationTests/Features/EventAgendaItemControllerTests.cs \
        Event.API.IntegrationTests/Features/EventDayControllerTests.cs \
        Event.API.IntegrationTests/Features/EventSeriesControllerTests.cs \
        Event.API.IntegrationTests/Features/Hateoas/EventAgendaItemLinkPolicyTests.cs \
        Event.API.IntegrationTests/Features/Hateoas/EventDayLinkPolicyTests.cs \
        Event.Application.UnitTests/Features/Events/ \
        Event.Application.UnitTests/Features/EventDays/ \
        Event.Application.UnitTests/Features/EventSessions/ \
        Event.Application.UnitTests/Features/EventAgendaItems/ \
        Event.Application.UnitTests/Features/EventSeries/ \
        Explore.Blazor.Client.Tests/Pages/Event/EventEditTests.cs \
        Explore.Blazor.Client.Tests/Services/EventServiceTests.cs \
        Explore.Blazor.Client.Tests/Services/EventSeriesServiceTests.cs \
        Explore.Blazor.Client.Tests/Services/EventDayServiceTests.cs \
        Explore.Blazor.Client.Tests/Services/EventAgendaItemServiceTests.cs
git commit -m "refactor(app/events): implement PATCH updates for Event core details, series, days, and sessions"

# Commit 16
git add Explore.Application/DTOs/EventRegistration/ \
        Explore.Application/DTOs/EventSessionLanguage/ \
        Explore.Application/DTOs/EventSessionSpeaker/ \
        Explore.Application/DTOs/EventTags/ \
        Explore.Application/DTOs/EventCategories/ \
        Explore.Application/Profiles/RegistrationMappingProfile.cs \
        Explore.Application/Profiles/LookupMappingProfile.cs \
        Explore.Application/Features/EventRegistrations/ \
        Explore.Application/Features/EventSessionLanguages/ \
        Explore.Application/Features/EventSessionSpeakers/ \
        Explore.Application/Features/EventTags/ \
        Explore.Application/Features/EventCategories/ \
        Explore.API/Controllers/EventRegistrationController.cs \
        Explore.API/Controllers/EventSessionLanguageController.cs \
        Explore.API/Hateoas/Policies/EventRegistrationLinkPolicy.cs \
        Explore.Blazor.Client/Pages/Events/Dialogs/RegistrationManagerDialog.razor \
        Explore.Blazor.Client/Pages/Events/Components/EventCard.razor \
        Explore.Blazor.Client/Pages/Events/Components/EventCard.razor.cs \
        Explore.Blazor.Client/Pages/Events/Components/EventCard.razor.css \
        Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor \
        Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor.cs \
        Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor.css \
        Explore.Blazor.Client/Components/Events/EventDetailsSidebar.razor \
        Explore.Blazor.Client/Components/Events/EventDetailsSidebar.razor.cs \
        Explore.Blazor.Client/Services/EventRegistrationService.cs \
        Event.API.IntegrationTests/Features/EventRegistrationControllerTests.cs \
        Event.API.IntegrationTests/Features/EventSessionLanguageControllerTests.cs \
        Event.Application.UnitTests/DTOs/EventRegistration/ \
        Event.Application.UnitTests/Features/EventRegistrations/ \
        Event.Application.UnitTests/Features/EventSessionLanguages/ \
        Event.Application.UnitTests/DTOs/EventCategories/ \
        Event.Application.UnitTests/DTOs/EventSessionLanguage/ \
        Event.Application.UnitTests/DTOs/EventSessionSpeaker/ \
        Event.Application.UnitTests/DTOs/EventTags/ \
        Event.Application.UnitTests/Features/EventCategories/ \
        Event.Application.UnitTests/Features/EventSessionSpeakers/ \
        Event.Application.UnitTests/Features/EventTags/ \
        Explore.Blazor.Client.Tests/Components/Event/EventCardTests.cs \
        Explore.Blazor.Client.Tests/Components/Event/EventFilterBarTests.cs \
        Explore.Blazor.Client.Tests/Components/Event/EventDetailsSidebarTests.cs \
        Explore.Blazor.Client.Tests/Services/EventRegistrationServiceTests.cs
git commit -m "refactor(app/events): implement PATCH updates for registrations, speakers, languages, tags, and categories"

# Commit 17
git add Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor \
        Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor.css \
        Explore.Blazor.Client/wwwroot/js/ai-assistant-rail.js \
        Explore.Blazor.Client/Services/Ai/AiAssistantClientService.cs \
        Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor \
        Explore.Application/Features/AiAssistant/Prompting/AiPromptContextBuilder.cs \
        Explore.Application/Features/AiAssistant/Prompting/AiSystemPromptFactory.cs \
        Explore.Application/ApplicationServicesRegistration.cs \
        Event.Application.UnitTests/Features/AiAssistant/Prompting/AiPromptContextBuilderTests.cs \
        Explore.Blazor.Client.Tests/Components/Shell/AiAssistantRailTests.cs \
        Explore.Blazor.Client.Tests/Services/AiAssistantClientServiceTests.cs
git commit -m "feat(blazor/ai): refresh AI assistant rail and layout"

# Commit 18
git add Explore.Blazor.Client/Models/Events/ \
        Explore.Application/Models/Common/
git commit -m "feat(blazor/ai): define extra client models and settings"

# Commit 19
git add Explore.Blazor.Client/Clients/EventApiClient.g.cs \
        Explore.Blazor.Client/Clients/SchedulingDtos.cs \
        schemas/openapi.json \
        docs/API_CONTRACT_INVENTORY.md \
        docs/API_CHANGELOG.md
git commit -m "docs(api/contracts): refresh generated API surface"

# Commit 20
git add Event.API.IntegrationTests/Features/AuthFamilyEventControllerTests.cs \
        Event.API.IntegrationTests/Features/AuthorizationIntegrationTests.cs \
        Event.API.IntegrationTests/Features/UserControllerTests.cs \
        Event.Application.UnitTests/Behaviors/AuthorizationBehaviorTests.cs
git commit -m "test(api/auth): check authorization integration tests"

# Commit any remaining files
git add .
git commit -m "chore(config): commit remaining uncommitted updates" || true

echo "All commits created successfully!"
