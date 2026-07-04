<!-- ABOUTME: Phase 0 audit of compatibility-looking tests for the improved overall testing workstream. -->
<!-- ABOUTME: Records which tests were deleted, retained, or deferred after behavior-level review. -->

# Obsolete Test Audit

Last Updated: 2026-07-04 Europe/Brussels

## Audit Rule

Do not delete a test because it uses words such as legacy, retired, alias, or compatibility. Delete or rewrite it only when it protects behavior the product no longer wants. Keep tests that prove current fail-closed behavior, active deployment key mapping, HAL affordance preservation, or migration safety.

## Deleted Or Rewritten

| File | Action | Reason |
|---|---|---|
| `Explore.Blazor.Client.Tests/Services/LocationServiceTests.cs` | Deleted `GetLocations_ReturnsLocations_WhenApiSucceeds`. | `LocationService.GetLocations()` was a compatibility alias with no production callers. The canonical method is `GetAllLocationsAsync()`. |
| `Explore.Blazor.Client/Services/LocationService.cs` | Removed `ILocationService.GetLocations()` and its implementation. | The alias existed only for compatibility and was not part of any current UI workflow. |

## Reviewed And Retained

| File | Decision | Reason |
|---|---|---|
| `Event.API.IntegrationTests/Features/AdminMigrationEndpointRetirementTests.cs` | Retain. | The test protects the current desired state: retired runtime migration execution must stay unmapped from HTTP and return `404`. |
| `Explore.Infrastructure.Tests/Infrastructure/CompositeOutboxMessageDispatcherTests.cs` | Retain. | The retired broker event test proves fail-closed behavior for unexpected persisted outbox rows; it does not preserve legacy delivery. |
| `Event.API.IntegrationTests/Features/ConfigurationExtensionsTests.cs` | Retain. | The tested Infisical-style keys remain active deployment inputs that map into canonical .NET configuration sections. This is operational compatibility, not obsolete API behavior. |
| `Event.Application.UnitTests/Behaviors/AuthorizationBehaviorTests.cs` | Retain for now. | `IAuthorizedRequest` is marked obsolete and has zero production usages, but `AuthorizationBehavior` still contains the bridge. Removing the tests without removing the bridge would reduce coverage. A future cleanup should delete the bridge and tests together after confirming no generated or pending handlers use it. |
| `Explore.Blazor.Client.Tests/Services/GroupServiceTests.cs` | Retain. | The compatibility-named member test still covers a production service method used by route guards. |
| `Explore.Blazor.Client.Tests/Services/OrganizationMemberServiceTests.cs` | Retain. | The compatibility-named member test covers an active service method and HAL affordance preservation. |
| `Explore.Blazor.Client.Tests/Services/TagServiceTests.cs` and `TagServiceCrudErrorHandlingTests.cs` | Retain for now. | `GetAllTagsAsync()` is still called by event list/create/edit and tag category UI flows. It should be renamed only with the production callers in the same slice. |

## Follow-Up Candidates

| Candidate | Next Step |
|---|---|
| `Explore.Application.Authorization.IAuthorizedRequest` bridge | Remove bridge, test doubles, and obsolete pragma only after a dedicated grep/build confirms no handlers or generated requests still depend on the interface. |
| `ITagService.GetAllTagsAsync()` | Consider renaming UI callers to `GetTagsAsync()` if service API cleanup becomes part of a Blazor client maintenance slice. |
