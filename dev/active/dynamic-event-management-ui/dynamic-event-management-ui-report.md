# Executive recommendation

Your direction is correct: **ISLAMU Event should become a workspace-based application with a Plane-style primary rail and a contextual secondary navigation panel.**

The target desktop shell should be:

```text
┌──────────┬──────────────────────┬───────────────────────────────┬─────────────────────┐
│ App rail │ Workspace navigation │ Main workspace canvas         │ AI / inspector dock │
│ 56–64 px │ 0 / 72 / 240–280 px │ Events, Studio, AI, Settings  │ 360–480 px          │
└──────────┴──────────────────────┴───────────────────────────────┴─────────────────────┘
```

In logical-direction terminology:

```text
Inline start                                              Inline end
App rail → contextual navigation → main canvas → contextual dock
```

That gives you exactly the separation visible across the supplied screenshots:

* The current ISLAMU Event screen is a discovery-oriented experience with a full event-seeker navigation.
* Plane uses a compact global application rail followed by navigation specific to the active product area.
* Your existing AI assistant already behaves like an inline-end contextual dock.

The most important design decision is this:

> **The compact app rail selects a workspace. The secondary navigation navigates inside that workspace. The right-side dock provides contextual tools without changing workspace.**

Do not treat these as three versions of the same sidebar.

---

# 1. The four global destinations

I would retain your four destinations, with Settings placed at the bottom of the rail rather than beside the three main workspaces.

| Rail item                 | Product meaning                           | Main behavior                                     | Secondary navigation               |
| ------------------------- | ----------------------------------------- | ------------------------------------------------- | ---------------------------------- |
| **Events** or **Explore** | Event-seeker experience                   | Browse, search, save and register                 | Discovery navigation               |
| **Studio**                | Event-organizer workbench                 | Operate organizations, events and attendees       | Organizer navigation               |
| **AI**                    | Full AI-native workspace                  | Long-running conversations, history and artifacts | Conversation navigation            |
| **Settings**              | Personal and administrative configuration | Opens the settings scope hub                      | Scope-specific settings navigation |

The rail should contain links with icons, accessible labels and tooltips. It should not contain unlabeled icon buttons.

## Events versus Explore

“Events” is understandable, but there is a naming collision because Studio will also contain an “Events” section.

A clearer product vocabulary would be:

* **Explore** in the primary rail.
* **Events** inside Studio.

However, keeping **Events** in the rail is still acceptable when the tooltip says “Explore events.”

## Settings is a utility destination

Settings belongs at the bottom, separated by flexible space, because it is not part of the normal Events → Studio → AI workflow.

The profile dropdown can continue to contain:

* View profile.
* Personal settings.
* Sign out.

That does not conflict with the rail. The profile-menu link should go directly to personal settings, while the rail Settings item opens the broader settings hub and remembers the last authorized scope.

---

# 2. AI should have two deliberate modes

You should not replace your existing AI panel with the full AI workspace. You need both.

## Full AI workspace

Clicking **AI** in the primary rail should navigate to something like:

```text
/ai
/ai/chats/{conversationId}
```

This is the ChatGPT-like environment for:

* New conversations.
* Conversation history.
* Saved outputs.
* Generated event drafts.
* Reports and analysis.
* Longer workflows.
* Future background tasks and automations.

The AI workspace can have its own secondary navigation:

```text
New conversation
Recent
Saved
Organizer workflows
Event discovery workflows
```

## Contextual AI dock

The existing sparkle button in the top bar should remain a separate action:

> **Open AI assistant panel**

It should open `shell.ai-assistant` on the inline-end side without navigating away from the active page.

Examples:

* While viewing an event: “Summarize this event.”
* While configuring tickets: “Check this pricing structure.”
* While viewing analytics: “Explain this drop in conversion.”
* While searching: “Find something suitable for a family with children.”

The contextual panel and full workspace must share:

* The same conversation service.
* The same conversation IDs.
* The same actor context.
* The same permissions.
* The same history.

The panel header should contain **Open in AI workspace**, and the full workspace should be able to return to the originating page.

Do not maintain one conversation history for the panel and another for `/ai`.

Also, do not expose an infrastructure name such as `Gemma-4-E2B-Uncensored` as the primary user-facing title. The product should say something stable such as **ISLAMU AI** or **AI Assistant**. Provider and model details belong in an information or diagnostics view.

Your governance system already has AI enablement and tenant-lock concepts, so workspace and dock availability can use the same effective configuration.

---

# 3. The primary rail should not be a normal dock panel

This is particularly important for your existing implementation.

Your Dock Layout architecture already supports:

* Shell and workspace scopes.
* Start, end and bottom sides.
* Docked, overlay, temporary, inspector and collapsed modes.
* Descriptor-owned width and persistence policy.
* Snapshots intended for future user preference storage.

It also currently registers `shell.left-nav` and `shell.ai-assistant`, with compatibility bridges through `SidebarState` and `AiAssistantState`.

However, the current same-side stacking model is tab-oriented. Two ordinary `DockSide.Start` panels would not naturally become:

```text
narrow rail + full secondary sidebar
```

They would become competing panels in the same side group.

## Recommended shell composition

Make the primary workspace rail permanent shell chrome, not a closable/resizable dock panel:

```razor
<ShellLayoutHost>
    <AppWorkspaceRail />

    <DockLayoutHost Scope="DockScope.Shell">
        @Body
    </DockLayoutHost>
</ShellLayoutHost>
```

The outer shell grid owns the app rail. The dock layout continues to own:

* The contextual workspace navigation at inline start.
* The AI assistant at inline end.
* Future inspectors and tool panels.

This is not a page compensation hack. It is a shell-owned permanent track.

Suggested components:

```text
AppWorkspaceRail.razor
WorkspaceNavigationHost.razor
EventsWorkspaceNavigation.razor
StudioWorkspaceNavigation.razor
AiWorkspaceNavigation.razor
SettingsWorkspaceNavigation.razor
```

Suggested descriptor migration:

```text
shell.left-nav       → shell.workspace-nav
shell.ai-assistant   → retain
```

The existing `AppSideNav` content can initially become `EventsWorkspaceNavigation`, after which the host chooses the appropriate provider for the active workspace.

Do not remove `SidebarState` and `AiAssistantState` in the first change. Your current Dock Layout documentation already requires that compatibility services remain until all consumers and tests have been migrated.

---

# 4. Separate four concepts that are currently easy to conflate

The shell should be composed from four independent concepts.

## A. Experience profile

This describes what kind of tenant experience is being offered.

Initially, two profiles are sufficient:

```text
Marketplace
OrganizationHub
```

A future `BackofficeOnly` profile can be introduced when there is a real requirement.

## B. Workspace

```text
Events
Studio
AI
Settings
```

The workspace comes from the route and is the source of truth for the active rail item.

## C. Acting actor

This is the entity on whose behalf the authenticated user is operating:

```text
Personal actor
Organization actor
Group actor
```

Your AI interface already exposes the beginning of this concept through “Acting as User.” That context should become a shared application-level actor context rather than an AI-only dropdown.

## D. Capability context

This answers:

* Can this user enter Studio?
* Which organizations can they manage?
* Can they create an event?
* Can they access tenant settings?
* Can they access instance settings?
* Can they use AI?
* Can they manage the current event’s attendees?

A role can contribute to a capability, but the UI should ultimately consume capabilities.

This prevents errors such as:

```text
Instance administrator → automatically show Studio
```

Your own authority model says an instance administrator is an infrastructure operator and does not automatically have access to tenant business data. Organization administrators manage events; tenant and instance authority are separate.

---

# 5. The dynamic shell decision model

Do not implement this as one enormous `if/else` block in `NavMenu.razor`.

Use a small shell composition policy:

```text
Final shell =
    Route policy
  + Effective experience policy
  + Server-authoritative capabilities
  + Active actor/resource context
  + User layout preferences
  + Viewport projection
```

A workspace should be visible only when all relevant conditions are true:

```text
Visible(workspace) =
    FeatureAvailable
    AND ExperienceProfileAllows
    AND AuthenticationRequirementSatisfied
    AND ServerCapabilityAllows
```

## Precedence

Use this order:

1. **Minimal/hidden-chrome route rules**
2. **Security and authorization capability**
3. **Instance-enforced governance**
4. **Tenant experience policy**
5. **Organization-specific default**
6. **User layout preference**
7. **Responsive viewport projection**

A user preference must never reopen a workspace or panel that is no longer allowed.

## Secondary-navigation mode

A precise rule can be:

```text
EffectiveNavigationMode =
    Hidden
        when the route requires hidden chrome
    Hidden
        when the active workspace has no secondary navigation
    TenantForcedMode
        when user override is disabled
    UserPreference
        when a valid user preference exists
    TenantDefault
        otherwise
```

Then responsive behavior is applied without changing the durable state:

```text
RenderedNavigationMode =
    Project EffectiveNavigationMode through viewport policy
```

For example, a user may have Studio navigation stored as `Docked`, while a narrow tablet temporarily renders it as `Overlay`.

Your Dock Layout policy already explicitly says that viewport-driven changes must not autosave durable layout preferences.

---

# 6. Recommended behavior by scenario

| Scenario                                          | Primary rail                                                     | Secondary navigation                       | Default destination                     | Settings scopes                                            |
| ------------------------------------------------- | ---------------------------------------------------------------- | ------------------------------------------ | --------------------------------------- | ---------------------------------------------------------- |
| Anonymous marketplace visitor                     | Events; AI only when anonymous AI is permitted; limited Settings | Discovery navigation                       | Events                                  | Appearance, language, privacy/cookies                      |
| Authenticated event seeker                        | Events, AI, Settings; Studio only when eligible                  | Discovery navigation                       | Last valid workspace or Events          | Personal                                                   |
| Organizer or organization member                  | Events, Studio, AI, Settings                                     | Organizer nav in Studio                    | Last valid workspace; optionally Studio | Personal + authorized organizations/groups                 |
| Organization-centric public visitor               | Usually hidden rail or Events-only public shell                  | Branded organization navigation            | Organization events/home                | Public preferences only                                    |
| Organization-centric organizer                    | Events, Studio, AI, Settings                                     | Studio navigation with pinned organization | Studio                                  | Personal + Organization; Tenant when separately authorized |
| Tenant administrator without organizer capability | Events, AI, Settings                                             | No Studio navigation                       | Events or Settings                      | Personal + Tenant                                          |
| Instance administrator only                       | Events, AI when allowed, Settings                                | No Studio merely because of instance role  | Settings or Events                      | Personal + Instance                                        |
| User holding several roles                        | Union of authorized workspaces                                   | Contextual                                 | Last valid workspace                    | Union of every explicitly authorized scope                 |

Two details matter:

1. **Studio eligibility is not equivalent to authentication.**
2. **Instance administration must not implicitly grant organizer access.**

---

# 7. Organization-centric experience

Your organization-centric scenario should be modeled as an explicit tenant experience policy, not inferred from the existence of only one organization.

Suggested policy:

```text
ExperienceProfile = OrganizationHub
PrimaryOrganizationId = ...
PublisherPolicy = PrimaryOrganizationOnly
PublicNavigationProfile = Organization
PrimaryRailVisibility = AuthenticatedOnly
OrganizerDefaultWorkspace = Studio
StudioActorSelectorMode = Pinned
```

## The organization is not itself a tenant administrator

An organization is a domain actor. Human users or machine principals hold administrative roles.

Model these separately:

```text
Tenant
  └── Primary publisher organization
        └── Organization members with event-management permissions
```

The same human may be both:

* Tenant administrator.
* Organization administrator.

But those are two separate authority grants.

That separation will save you from serious authorization ambiguity later.

## Public organization experience

For an organization-centric tenant, the Events navigation should become something like:

```text
Home
Upcoming events
Calendar
Past events
Venues
About
Contact
```

Remove or hide marketplace concepts that do not make sense:

```text
All organizations
Random
Organization discovery
Public organizer registration
```

“Community Guidelines,” legal information and low-frequency support links generally belong in the footer or a Help area rather than occupying prime navigation space.

## Authenticated organizer experience

When an authorized organization member enters Studio:

* The organization is preselected.
* The actor selector may be visually locked or reduced to a simple organization identity header.
* “Create event” automatically creates for the primary organization.
* A **View public site** action returns to Events.
* The Studio dashboard shows organization-specific data.

The Events workspace should still exist. Organizers need to preview the public experience.

## Public rail visibility

For an organization-branded public website, a SaaS-like four-icon rail may feel unnecessarily operational to anonymous visitors.

I recommend this default:

```text
OrganizationHub + anonymous public route
    → branded public shell without app rail

OrganizationHub + authenticated application route
    → full workspace rail
```

A tenant can choose to show the rail publicly, but `AuthenticatedOnly` is the stronger default.

## Publishing enforcement

The visual experience profile must not enforce “only this organization may publish.”

That must be a server-side publishing policy based on the event actor and current principal. Hiding the Studio item or Add Event button is only UX.

---

# 8. Studio information architecture

Studio should operate at two levels.

## Actor-level Studio

This is the organization, group or personal organizer dashboard:

```text
Overview
Events
Templates
Attendees
Check-in
Tickets and orders
Communications
Analytics
Team
Integrations
```

The top of the secondary navigation should show:

```text
[Organization logo]
Organization name
Actor switcher, when more than one actor is available
```

The actor switcher should list only actors the API says the user may operate.

## Event-level Studio

When an event is opened, replace the secondary navigation content rather than adding a third sidebar:

```text
← All events

Event title
Draft / Published / Cancelled

Overview
Details
Schedule
Sessions and agenda
Registration
Tickets and pricing
Attendees
Check-in
Communications
Analytics
Integrations
Publication
Event settings
```

This is the YouTube Studio pattern: the global Studio workspace remains active while the secondary navigation becomes resource-specific.

Never create:

```text
App rail + Studio sidebar + Event sidebar
```

Two inline-start tracks are already enough.

---

# 9. Studio navigation must reflect the event’s management model

This becomes particularly important because ISLAMU Event now supports events that are:

* Fully managed with native registration.
* Redirected to an external registration platform.
* Externally managed with optional synchronization.
* Listing-only.
* User-reported by someone who is not the organizer.
* Published without any registration or redirect action.

Studio must not show dead sections.

| Event model                                   | Studio sections that should appear                                                                                     |
| --------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Native ISLAMU registration                    | Registration, tickets, attendees, approvals, waitlist, check-in, communications, operational analytics                 |
| External registration with synchronization    | Integration status, synchronized attendees according to granted capabilities, referral analytics                       |
| External registration without synchronization | External registration configuration and referral/discovery analytics; no native attendee or ticket-management sections |
| Organizer listing with no action              | Content, schedule, publication and discovery analytics only                                                            |
| User-reported listing                         | Submission status, source and permitted corrections only; no organizer analytics, attendees, tickets or check-in       |

A user who reports an event must not become an organizer simply because they created the listing.

At most, provide a limited **My submissions** experience under Events or a restricted Studio contribution view. Do not expose organizer operations.

## Capability-driven event navigation

The event navigation provider should consume server-generated capabilities or HATEOAS relations.

For example:

```text
_links.edit
_links.configure-registration
_links.view-attendees
_links.manage-tickets
_links.manage-check-in
_links.view-analytics
```

Your repository already establishes that resource mutation affordances should be driven by returned HATEOAS links rather than duplicated client-side role checks. Role inspection is acceptable for broad menu/page eligibility, but per-resource operations must follow API capabilities.

This also gives you a natural answer for dynamic Studio navigation:

```text
Show Attendees only when the event response contains view-attendees.
Show Ticketing only when it contains manage-tickets.
Show Check-in only when it contains manage-check-in.
```

---

# 10. Settings scope architecture

Your proposed role-aware Settings behavior is good, but it should include organization and group scopes as well.

The settings scope list should come from the API:

```text
Personal
Organization: ISLAMU
Organization: Another organization
Group: Youth team
Tenant: ISLAMU Belgium
Instance
```

## Resolution rules

| Authority                                               | Available settings scopes                 |
| ------------------------------------------------------- | ----------------------------------------- |
| Authenticated user                                      | Personal                                  |
| Organization administrator                              | Personal + each authorized organization   |
| Group administrator                                     | Personal + each authorized group          |
| Tenant administrator                                    | Personal + current tenant                 |
| Instance administrator                                  | Personal + Instance                       |
| Instance administrator who is also tenant administrator | Personal + Instance + Tenant              |
| User with several memberships                           | Union of all explicitly authorized scopes |

Do not infer:

```text
Instance administrator → every tenant settings scope
```

## Settings routing

Use deep-linkable routes:

```text
/settings/personal/profile
/settings/personal/appearance
/settings/organization/{organizationId}/branding
/settings/organization/{organizationId}/team
/settings/tenant/{tenantId}/navigation
/settings/tenant/{tenantId}/event-policies
/settings/instance/security
/settings/instance/infrastructure
```

The active scope selector sits at the top of the Settings secondary navigation.

## Single-tenant presentation

In single-tenant deployments, exposing both “Instance” and “Tenant” to the same site operator can reveal implementation complexity.

A better presentation is:

```text
Personal
Site administration
Organization
```

“Site administration” can internally compose instance and default-tenant sections according to authorization. The actual API boundaries remain intact.

## Settings should not become a dumping ground

Configuration belongs in Settings:

* Branding.
* Domains.
* Event defaults.
* AI configuration.
* Storage.
* Authentication.
* Navigation.

Operational work does not naturally belong there:

* Moderation queues.
* Attendee operations.
* Check-in.
* Failed webhook deliveries.
* Event approvals.

Those belong in Studio or a future Admin/Control workspace.

Your four-item rail is appropriate now, but do not hard-code “exactly four forever.” Use a workspace registry that permits a future admin workspace when the operational surface justifies it.

---

# 11. Tenant defaults versus user state

This distinction is essential.

## Tenant administrator controls

The tenant administrator should control defaults and availability:

```text
Experience profile
Primary organization
Whether Studio is enabled
Whether AI is enabled
Whether the primary rail appears publicly
Default secondary-navigation mode per workspace
Whether users may override that mode
Available discovery navigation items
Custom links
```

## User controls

The user should control their personal working layout:

```text
Events navigation expanded or collapsed
Studio navigation expanded or collapsed
Navigation width
AI panel open/closed
AI panel width
Last valid workspace
Last selected managed actor
```

A tenant administrator should not save:

```text
Amir currently has the Studio sidebar open
```

The tenant supplies the default. Amir supplies Amir’s current preference.

The existing settings hierarchy already recognizes user, group, organization, tenant and instance scopes, with higher-level lock behavior.

However, not every setting should be allowed at every scope. Define allowed scopes explicitly:

```text
experience.profile
    → Instance/Tenant only

ui.navigation.allow_user_override
    → Instance/Tenant only

ui.shell.layout
    → User only
```

Do not allow the generic hierarchy to imply that a user may override the tenant’s experience profile.

---

# 12. Persisting layout state

Your proposal to retain the state in the database is correct for authenticated users.

The current schema already has tenant-scoped user preferences with a unique `(tenant_id, user_id, setting_key)` key, which is a suitable initial persistence mechanism.

Your Dock Layout implementation already has a versionable `DockLayoutSnapshot`, descriptor-level `PersistState`, width clamping and normalization behavior. It is explicitly described as the foundation for future user preference storage.

## Suggested preference keys

```text
ui.shell.layout.desktop.v1
ui.shell.last_workspace.v1
ui.studio.last_actor.v1
```

Example value:

```json
{
  "version": 1,
  "lastWorkspace": "studio",
  "workspaceNavigation": {
    "events": {
      "mode": "collapsed",
      "width": 232
    },
    "studio": {
      "mode": "docked",
      "width": 268
    },
    "ai": {
      "mode": "docked",
      "width": 252
    },
    "settings": {
      "mode": "docked",
      "width": 260
    }
  },
  "assistant": {
    "mode": "overlay",
    "width": 408,
    "isOpen": false
  },
  "lastActorId": "..."
}
```

## Persistence rules

* Persist only descriptors with `PersistState=true`.
* Persist user-triggered changes.
* Do not persist responsive forced overlays or closures.
* Save a resize on pointer release, not on every pointer movement.
* Debounce collapse/open changes.
* Clamp restored widths through the descriptor.
* Ignore unknown or removed panel IDs.
* Version the JSON.
* If permissions revoke the stored actor or workspace, discard that part of the preference.

## Anonymous users

For anonymous visitors:

* Store layout preferences locally, scoped by tenant.
* Do not create database rows.
* On login, use the database preference when one exists.
* When none exists, optionally promote the local preference.

## Cross-device behavior

Database persistence gives the user a consistent layout across devices, but mobile should not reuse raw desktop width values. Persist semantic state and allow the Dock Layout engine to clamp or project it per viewport.

---

# 13. Navigation customization model

Your current `tenant_navigation_links` model contains a label, URL, icon, order, new-tab flag and active state. That is suitable for basic custom links, but it is not sufficient to control core workspace navigation safely.

Do not let tenants replace the entire core navigation tree with arbitrary URL records.

Use two layers.

## Code-owned definitions

Core product items are registered by stable keys:

```text
events.browse
events.search
events.nearby
events.saved
events.registrations

studio.dashboard
studio.events
studio.attendees
studio.analytics

settings.personal.profile
settings.tenant.navigation
```

Each definition owns:

* Stable key.
* Default translation key.
* Icon.
* Route.
* Workspace.
* Section.
* Capability requirement.
* Whether it can be hidden.
* Whether it can be reordered.

## Database-owned overrides

Persist only allowed overrides:

```text
Scope
ScopeId
WorkspaceKey
NavigationItemKey
IsVisible
Order
LabelOverride
SectionOverride
```

Authorization expressions should not be stored in the database.

The server or code-owned registry decides that `studio.attendees` requires the attendee capability. A tenant cannot configure a string such as:

```text
required_role = "Admin"
```

and thereby redefine security.

## Custom links

Custom links remain separate:

```text
WorkspaceKey
SectionKey
Label
URL
Icon
Order
OpenInNewTab
IsActive
```

This supports tenant links without making URLs the identity of core features.

## Tenant navigation editor

The tenant settings page should offer:

1. Experience-profile preset.
2. Public rail visibility.
3. Default secondary-navigation mode.
4. User override permission.
5. Reordering of optional core items.
6. Visibility toggles for optional items.
7. Custom links.
8. Preview as anonymous visitor.
9. Preview as authenticated seeker.
10. Preview as organizer.

Preview mode must not grant permissions. It only previews composition.

---

# 14. Recommended Blazor architecture

## Shell state

```text
IUiShellState
UiShellState
```

Responsibilities:

* Active workspace derived from route.
* Active shell profile.
* Effective secondary-navigation mode.
* Current actor context.
* Last route per workspace.
* Current shell bootstrap.
* Preference synchronization.

## Workspace registry

```text
IWorkspaceRegistry
WorkspaceRegistry
WorkspaceDescriptor
```

A descriptor can contain:

```csharp
public sealed record WorkspaceDescriptor(
    string Key,
    string LabelKey,
    string Icon,
    string BaseRoute,
    bool RequiresAuthentication,
    Func<UiShellContext, bool> IsAvailable,
    IWorkspaceNavigationProvider NavigationProvider);
```

This is compile-time product registration, not runtime plugin loading.

## Navigation providers

```text
EventsWorkspaceNavigationProvider
StudioWorkspaceNavigationProvider
AiWorkspaceNavigationProvider
SettingsWorkspaceNavigationProvider
```

The Studio provider can select between:

```text
StudioActorNavigation
StudioEventNavigation
StudioContributionNavigation
```

based on route and resource capabilities.

## Actor context

```text
IActingActorContext
ActingActorContext
```

It should contain:

```text
TenantId
UserId
ActorId
ActorType
DisplayName
```

The client-selected actor is a request hint. The API still validates the relationship and action.

## API shell bootstrap

Provide one authoritative authenticated endpoint:

```text
GET /api/me/ui-shell-context
```

Example response:

```json
{
  "experienceProfile": "OrganizationHub",
  "workspaces": [
    { "key": "events", "isAvailable": true },
    { "key": "studio", "isAvailable": true },
    { "key": "ai", "isAvailable": true },
    { "key": "settings", "isAvailable": true }
  ],
  "managedActors": [
    {
      "actorId": "...",
      "actorType": "Organization",
      "displayName": "ISLAMU"
    }
  ],
  "settingsScopes": [
    { "scope": "Personal" },
    { "scope": "Organization", "id": "..." },
    { "scope": "Tenant", "id": "..." }
  ],
  "navigationDefaults": {
    "events": "Collapsed",
    "studio": "Docked"
  }
}
```

Anonymous shell defaults can remain in the public experience bootstrap. Do not expose private memberships through the anonymous endpoint.

## Preference endpoint

Use a batch operation:

```text
GET /api/me/ui-preferences
PUT /api/me/ui-preferences
```

Do not send one API request for each individual panel property.

---

# 15. Route model

The route must determine the workspace. Do not store the active workspace only as a boolean in a scoped service.

Suggested structure:

```text
/events
/events/search
/events/saved
/events/registrations

/studio
/studio/actors/{actorId}
/studio/actors/{actorId}/events
/studio/events/{eventId}/overview
/studio/events/{eventId}/schedule
/studio/events/{eventId}/registration
/studio/events/{eventId}/attendees
/studio/events/{eventId}/analytics

/ai
/ai/chats/{conversationId}

/settings/personal/profile
/settings/organization/{organizationId}/branding
/settings/tenant/{tenantId}/navigation
/settings/instance/security
```

The user may switch workspaces and return to their last route inside each workspace:

```text
Events → Studio → AI → Events
```

The return to Events can restore `/events/search?...`.

Keep the full last-route map in session state. Persist only the last workspace and actor unless there is a strong reason to synchronize detailed route history across devices.

---

# 16. Workspace-aware top bar

The top bar should respond to the active workspace.

| Active workspace | Search behavior                             | Primary action             |
| ---------------- | ------------------------------------------- | -------------------------- |
| Events           | Search public events                        | Add event or Suggest event |
| Studio           | Search managed events, attendees and orders | Create                     |
| AI               | Search conversations or hide global search  | New conversation           |
| Settings         | Search settings                             | Usually none               |

The current **Add Event** button should resolve through eligibility:

* Authorized organizer: Create event in Studio.
* User allowed to create personally: Create personal event.
* User allowed only to contribute a reported listing: Suggest event.
* No capability: Hide the action.

In Studio, the active actor should be visible near the action so users understand for whom they are creating the event.

---

# 17. Responsive behavior

## Large desktop

```text
App rail + docked workspace nav + canvas + optional AI dock
```

## Constrained desktop and tablet

Keep the app rail. Project the workspace navigation or AI panel into overlay mode when the canvas becomes too narrow.

Your Dock Layout already preserves a center-content floor and can project panels to overlays without overwriting durable dock state.

For Studio, a global minimum of 375 pixels may be too small for tables, schedules and attendee management. Add a workspace-specific canvas requirement:

```text
Events:   375 px
AI:       520 px
Settings: 560 px
Studio:   720–800 px
```

This allows Studio to force side panels into overlay mode sooner without changing the behavior of public event pages.

## Mobile

Do not render two left-side panels.

Transform the primary app rail into a bottom navigation:

```text
Events | Studio | AI | Settings
```

Then:

* Workspace navigation opens as a temporary drawer.
* AI panel becomes full-screen temporary chrome.
* Studio event navigation opens through a workspace menu button.
* The main action can become a floating or top-bar action.

Responsive projection must not modify the user’s stored desktop preference.

---

# 18. Accessibility and RTL

The shell should expose separately named navigation landmarks:

```html
<nav aria-label="Application workspaces">
<nav aria-label="Event discovery navigation">
<nav aria-label="Studio navigation">
<nav aria-label="Settings navigation">
```

Only the currently rendered secondary navigation needs to exist.

Rail links should include:

* Accessible text.
* Tooltip.
* `aria-current="page"` or the appropriate active indication.
* Visible focus styling.
* More than color alone for active state.

When switching workspaces:

* Update the document title.
* Move focus to the page `h1` or main landmark.
* Announce the new workspace when necessary.
* Restore focus when the AI dock closes.

Your current shell contract already includes skip navigation, main/header/nav landmarks, ARIA live regions and navigation focus management.

Use logical inline-start and inline-end positioning. In Arabic, the app rail and secondary navigation should move to the right, and the AI dock should move to the left. The Dock Layout documentation already requires logical sides rather than physical left/right state.

---

# 19. AI authorization and safety boundaries

The AI’s “Acting as” selector must never grant authority.

Changing from:

```text
Acting as User
```

to:

```text
Acting as Organization
```

only changes the requested operating context. Every tool call still goes through normal API authorization.

For AI writes:

1. The model proposes an action.
2. The server checks authorization.
3. The user sees a structured preview.
4. Destructive or external actions require explicit confirmation.
5. The final action is audited.
6. The API remains authoritative.

Examples requiring confirmation:

* Publish event.
* Cancel event.
* Send communication.
* Change ticket price.
* Export attendees.
* Delete registration data.

The AI must never receive data from another tenant merely because an instance administrator is using the assistant.

---

# 20. Important anti-patterns to avoid

1. **Do not keep one static event-seeker sidebar across every route.** Its content is irrelevant in Studio and Settings.

2. **Do not register the permanent app rail as another ordinary start-side dock panel.** The current stack model would treat same-side panels as competing/tabbed content.

3. **Do not let the tenant store each user’s live open/closed state.** Tenant policy defines defaults; user preferences define personal state.

4. **Do not show Studio based only on `IsInRole("Admin")`.** Use server-provided eligibility and capabilities.

5. **Do not let an instance administrator automatically operate tenant events.**

6. **Do not let a reported event unlock organizer analytics, attendees or ticketing.**

7. **Do not add a third sidebar when an event is selected in Studio.** Replace the secondary navigation content.

8. **Do not store arbitrary authorization expressions in navigation records.**

9. **Do not build separate AI conversation systems for the full workspace and contextual panel.**

10. **Do not save a database preference on every resize pointer event.**

11. **Do not permanently close panels because of a temporary viewport restriction.**

12. **Do not place moderation queues, check-in and webhook delivery operations indiscriminately under “Settings.”**

---

# 21. Implementation sequence

## Phase 1 — Shell vocabulary and ADR

Document:

* Workspace.
* App rail.
* Workspace navigation.
* Contextual dock.
* Experience profile.
* Acting actor.
* Settings scope.
* Policy versus preference.

Add an ADR for workspace-shell composition.

## Phase 2 — Permanent app rail

Implement:

```text
AppWorkspaceRail
WorkspaceRouteClassifier
WorkspaceRegistry
```

Initially expose only the existing Events workspace while preserving behavior.

## Phase 3 — Migrate existing event navigation

Move current `AppSideNav` content into:

```text
EventsWorkspaceNavigation
```

Rename the shell panel concept to `shell.workspace-nav`.

Keep compatibility bridges until all existing toggle consumers are migrated.

## Phase 4 — Studio shell

Implement:

* Studio route group.
* Studio actor selector.
* Organizer eligibility endpoint.
* Actor-level Studio dashboard.
* Event-level navigation.
* Capability-based section visibility.

Start with dashboard, events, attendees and analytics shells before implementing every feature.

## Phase 5 — Scope-aware settings hub

Implement:

* Settings scope endpoint.
* Personal/organization/group/tenant/instance routes.
* Scope selector.
* Single-tenant “Site administration” composition.

## Phase 6 — AI dual experience

Retain the contextual assistant panel and add:

* Full `/ai` workspace.
* Shared conversation state.
* Open-in-workspace behavior.
* Shared actor/resource context.
* Tool confirmation and audit path.

## Phase 7 — Tenant experience profiles

Add:

```text
Marketplace
OrganizationHub
```

Then add:

* Primary organization selection.
* Public shell behavior.
* Publisher policy.
* Navigation presets.
* Organizer default workspace.

## Phase 8 — Persistence and customization

Connect `DockLayoutSnapshot` to user preferences.

Add tenant navigation defaults and code-owned navigation overrides.

## Phase 9 — Responsive, RTL and accessibility hardening

Test:

* Desktop with both navigation and AI open.
* Constrained widths.
* Mobile bottom navigation.
* Arabic RTL.
* Keyboard-only usage.
* Screen-reader landmarks and focus.
* Reduced motion.
* Permission revocation while Studio is active.

## Phase 10 — Scenario-matrix tests

Make the scenario table a real table-driven test suite:

```text
Profile × Authentication × Roles × Capabilities × Workspace × Viewport
```

Test that each combination produces the expected:

* Rail destinations.
* Secondary navigation.
* Settings scopes.
* Default route.
* Panel mode.
* Fallback when authorization changes.

---

# Final architectural decision

Use the following model:

```text
Permanent shell app rail
    Events / Studio / AI
    Settings at bottom

One contextual secondary navigation
    Events discovery navigation
    Studio organizer navigation
    AI conversation navigation
    Settings scope navigation

One contextual inline-end dock
    AI assistant
    Inspectors
    Future previews

Tenant policy
    decides experience profile, availability and defaults

User preference
    decides personal collapsed/open state and width

Server capabilities
    decide what the user may enter or operate

Route
    decides the active workspace

Actor context
    decides on whose behalf Studio and AI are operating
```

For the organization-centric profile, hide or simplify the public application chrome, pin the primary organization in Studio, retain Events as the public preview experience, and enforce single-publisher behavior in the API rather than through navigation visibility.

This gives ISLAMU Event a coherent product architecture: **an event marketplace for seekers, a professional Studio for organizers, an AI-native workspace, and a role-aware administration surface—without fragmenting the application into separate products.**
