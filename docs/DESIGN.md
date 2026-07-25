ABOUTME: Root design-system contract for the ISLAMU Event Blazor experience.
ABOUTME: Codifies existing MudBlazor tokens, wrapper components, and support-access UI primitives.

# ISLAMU Event Design System

## 1. Atmosphere & Identity

ISLAMU Event is a quiet operational command center for event teams, platform administrators, and support staff. The signature is trustworthy density: clear surfaces, restrained status color, and explicit action affordances that make administrative work feel accountable rather than flashy.

## 2. Color

Colors flow through MudBlazor palette variables and the `--isl-*` semantic aliases defined in `Explore.Blazor/wwwroot/css/tokens.css`.

| Role | Token | Source | Usage |
|------|-------|--------|-------|
| Primary action | `--isl-color-primary` | `--mud-palette-primary` | Filled buttons, focus rings, links |
| Secondary action | `--isl-color-secondary` | `--mud-palette-secondary` | Secondary controls |
| Surface | `--isl-color-surface` | `--mud-palette-surface` | Cards, panels, dialogs |
| Background | `--isl-color-background` | `--mud-palette-background` | Page canvas |
| Text primary | `--isl-color-text` | `--mud-palette-text-primary` | Headings and body |
| Text secondary | `--isl-color-text-secondary` | `--mud-palette-text-secondary` | Metadata and supporting labels |
| Border | `--isl-color-border` | `--mud-palette-lines-default` | Card and panel outlines |
| Divider | `--isl-color-divider` | `--mud-palette-divider` | Section separators |
| Error | `--isl-color-error` | `--mud-palette-error` | Destructive states |
| Success | `--isl-color-success` | `--mud-palette-success` | Completed states |
| Warning | `--isl-color-warning` | `--mud-palette-warning` | Risk, support-access active state |
| Info | `--isl-color-info` | `--mud-palette-info` | Neutral informative states |

Rules:
- Use semantic tokens in component CSS; raw palette values live only in token/theme sources.
- Color cannot be the only signal for status. Pair warning/error/success with text and iconography.
- Support-access active UI uses warning semantics, not primary branding, because it represents elevated risk.

## 3. Typography

Typography follows the fluid scale in `tokens.css`.

| Level | Token | Usage |
|-------|-------|-------|
| H1 | `--isl-typography-h1-*` | Page titles |
| H2 | `--isl-typography-h2-*` | Major sections |
| H3 | `--isl-typography-h3-*` | Panel headings |
| H4-H6 | `--isl-typography-h4-*` through `--isl-typography-h6-*` | Compact section headings |
| Body | `--isl-typography-body1-*` | Primary copy |
| Body small | `--isl-typography-body2-*` | Secondary copy and dense rows |
| Button | `--isl-typography-button-*` | Button labels |
| Caption | `--isl-typography-caption-*` | Metadata |
| Overline | `--isl-typography-overline-*` | Rare labels |

Font stack:
- Primary: `--isl-font-family-primary`
- Secondary: `--isl-font-family-secondary`

Rules:
- Every page has one structural `h1`.
- Body text stays at or above the existing body scale.
- Overline labels are rare; status banners should use direct prose rather than decorative small caps.

## 4. Spacing & Layout

Spacing uses the 4px grid from `--isl-space-1` through `--isl-space-16`.

| Token | Value | Usage |
|-------|-------|-------|
| `--isl-space-1` | 4px | Tight icon or status gaps |
| `--isl-space-2` | 8px | Inline groups |
| `--isl-space-3` | 12px | Compact padding |
| `--isl-space-4` | 16px | Standard component padding |
| `--isl-space-5` | 20px | Comfortable inline blocks |
| `--isl-space-6` | 24px | Page and panel padding |
| `--isl-space-8` | 32px | Section spacing |
| `--isl-space-10` | 40px | Large section separation |
| `--isl-space-16` | 64px | Major page rhythm |

Layout rules:
- Use CSS Grid or MudBlazor layout primitives for multi-column work.
- Component CSS uses logical properties for RTL support.
- Page sections are full-width or constrained layouts; nested cards are avoided.

## 5. Components

### AppButton
- Structure: wrapper over `MudButton`.
- Variants: filled, outlined, text.
- Spacing: `--isl-button-padding-x`, `--isl-button-padding-y`.
- States: default, hover, active, focus, disabled.
- Accessibility: visible focus, real button/link semantics, optional icon text stays readable.
- Motion: tokenized background, border, shadow, and opacity transitions.

### AppCard
- Structure: wrapper over `MudCard`.
- Variants: flat default, outlined when separation is required.
- Spacing: caller-owned content padding via tokenized layout classes or component CSS.
- States: default, hover only when clickable.
- Accessibility: cards are not interactive unless rendered with real button/link semantics.
- Motion: no decorative movement.

### WebPushSubscriptionPanel
- Structure: one outlined settings panel with a compact heading, privacy copy, browser state, and one explicit command.
- Variants: unavailable, unsupported, denied, ready, enabled, and error.
- States: permission is requested only from the Enable command; denied state has no repeated prompt affordance.
- Accessibility: state changes use visible text plus polite live-region announcements; icons are supplementary.
- Motion: none beyond the parent settings surface transition.

### AppTextField
- Structure: wrapper over `MudTextField`.
- Variants: outlined default.
- Spacing: label above input, helper/error text below.
- States: default, focus, disabled, error.
- Accessibility: labels are mandatory; placeholder-only labels are not allowed.
- Motion: MudBlazor focus and validation transitions only.

### AppIconButton
- Structure: wrapper over `MudIconButton`.
- Variants: default, primary, error, warning.
- Spacing: minimum target size follows `--isl-target-min`.
- States: default, hover, active, focus, disabled.
- Accessibility: icon-only actions require `aria-label`.
- Motion: tokenized hover/active feedback.

### SupportAccessBanner
- Structure: shell-level status banner with warning icon, active target summary, expiry, mode, and stop action.
- Variants: read-only active, write active, stopping, error.
- Spacing: `--isl-space-3` mobile padding, `--isl-space-4` desktop padding, `--isl-space-2` inline gaps.
- States: active, loading stop, stop failed, expired/cleared.
- Accessibility: `role="status"` for normal updates, assertive announcement only when stop fails or session expires.
- Motion: opacity/transform entry only; no repeated animation.

### SupportAccessStatusChip
- Structure: compact icon/text chip used inside admin surfaces.
- Variants: read-only, write, expired, revoked.
- Spacing: `--isl-space-1` icon gap, `--isl-space-2` inline padding.
- States: default, focus when interactive, disabled.
- Accessibility: status text must remain visible; color is supplementary.
- Motion: none.

### PublicDiscoveryAreaControl
- Structure: one compact heading row inside the active `HomeDiscoveryHero` inner grid, spanning both columns above the poster and event copy. The only visible heading copy is “Browsing events in” followed by a disclosure trigger that names the active area, “all areas,” or “online events.” The same header-only inner remains when the filtered hero is empty.
- Variants: active area and online mode.
- States: loading, selected, locating, location denied/unavailable/no-match, and disabled only while the explicit location action is running.
- Actions: the disclosed surface contains active tenant areas, “Use my current location,” and “Browse online events.” Browser geolocation is never requested during load.
- Accessibility: the trigger exposes its expanded state and controlled surface, every choice is a native button, Escape closes the surface, status/error text stays visible, context changes are announced politely, and focus returns to the trigger after selection.
- Privacy: persist only stable area ID and mode; never render, persist, log, trace, or analyze the browser origin.

### HomeDiscoveryHero
- Reference contract: the composition follows the live MangaDex home banner inspected on 2026-07-17 for layout grammar only; ISLAMU retains its own content, palette tokens, typography, actions, and assets.
- Structure: featured-event carousel with one active slide, a full-bleed backdrop, a separate inset 7:10 event poster, event copy, previous/next buttons, swipe support, and a visible position counter. The active slide surface is one real event link; its linked actor identity and optional external-platform action are independent secondary links. The persistent context header sits outside the animated slide track, and carousel controls remain an independent interactive region above it.
- Variants: image, image-fallback, and compact empty. A tenant may expose up to ten slides; a single slide omits previous/next controls, while zero slides retain the context control and explain that no featured event matches.
- States: active, previous/next focus and disabled edges, pointer drag/swipe, external-platform action, and reduced-motion.
- Accessibility: controls are real labeled buttons; the slide-wide, actor-profile, and external-platform links have explicit accessible names and visible focus rings; external navigation opens a new tab with opener isolation; slide changes are politely announced; title, schedule, and location/online context remain readable without the image.
- Motion: automatically advances to the next event every nine seconds and loops from the final slide to the first. Manual and automatic slide changes use only tokenized opacity/transform motion and become effectively instant under `prefers-reduced-motion`.
- Geometry: the banner bleeds through the home page gutter to fill the shell's complete main-content width. Its block size is 25.5rem on desktop, 23rem on tablet, and 15.75rem on narrow screens so the composition ends with the inset poster instead of reserving a separate control row; it has no card radius or outer shadow.
- Backdrop: the active event image fills the banner at 150% of banner block size with `object-fit: cover` and a top-biased focal point so the upper image remains visible as the viewport changes. The backdrop and tokenized readability scrim share a vertical mask that stays opaque through the upper composition and reaches full transparency at the lower edge, revealing the page background without a banner boundary.
- Poster: the same event image is rendered as meaningful 7:10 cover art inset from the inline edge, capped at 13.5rem wide on tablet/desktop and 7rem on narrow screens. It remains visible at every supported breakpoint.
- Content: poster and copy form a top-aligned two-column grid with tokenized spacing. The title consumes the copy column before wrapping and reserves only the top-right external-action lane when that action exists; compact black, white-text uppercase event-type and event-format badges follow it. A linked organization/group avatar and display name follow the description. Description progressively collapses on narrow screens, while event title, metadata, and actor identity remain visible. There is no nested “View event” button because the slide surface is the event link. A safe `EventUrl`, or server-owned federated HAL `source` redirect, adds the same top-right “Open” action used by `EventCard`: pointer-capable layouts reveal its surface treatment on slide hover or focus-within, while narrow or coarse-pointer layouts keep a background-free action visible. Controls share the poster's lower horizontal lane: the visible counter is plain uppercase `NO. n` text and previous/next buttons are transparent at rest with hover and focus feedback.
- Images: the active backdrop and poster are high priority (two eager image elements sharing one URL/cache entry); every inactive slide image is lazy. Media has explicit dimensions or aspect ratio, the backdrop is decorative, and the poster has meaningful alternative text.
- Context filtering: the active area/online selection is the source of truth for the hero and contextual sections. Online-capable inventory includes both Digital and Hybrid events; area inventory uses the selected public area's locations. Curated filters intersect with this context rather than widening it.

### EventCard
- Structure: the production event card is the only card used by home discovery and `/events`; card-body navigation is a real link or keyboard-equivalent target, while nested share/edit/delete controls remain independent.
- Variants: `DetailedList`, `SingleRow`, and `CompactGrid`.
- States: image, generated mesh image-fallback, hover, focus-visible, loading placeholder, external-platform action, and server-provided management affordances.
- Accessibility: Enter and Space activate card-body navigation without activating nested controls; focus remains visible; images have meaningful alternative text; metadata is not color-only.
- Authorization: edit/delete and other management actions render only when the matching HAL relation is present. Layout mode never changes authority.
- Schedule: card metadata uses `ddd, MMM dd, h:mm tt` with the month uppercased; the year is omitted for dates in the current year and included for every other year.
- Fallback artwork: missing or failed event images use a local SVG whose title-stable hash selects a duo-tone linear gradient and radial mesh positions. The SVG uses the application's browser-native `color-scheme` to choose a softer light-theme palette or the deeper dark-theme palette and repaints when the active theme changes. The artwork contains no duplicate title text; the surrounding image element retains the event title as its accessible alternative.
- External platform: a non-empty, absolute HTTP(S) `EventUrl`, or the server-owned HAL `source` redirect for a federated discovery event, adds an independent “Open” link at the image's block-start/inline-end corner. It opens a new tab with opener isolation. Pointer-capable desktop layouts reveal the theme-aware surface button on card hover or focus-within; narrow or coarse-pointer layouts keep a background-free, high-contrast text action visible without hover.

### EventDetailsSidebar
- Structure: the event preview keeps the internal “Event Page” action as the primary navigation path and places an external “Open” action at the inline end of the header action group only when the selected event has a safe `EventUrl` or a server-owned federated HAL `source` redirect.
- States: internal-only, internal plus external platform, image, generated mesh image-fallback, loading, and HAL-provided management or registration affordances.
- Accessibility: the external action explicitly announces that it opens in a new tab, uses native link semantics, and preserves the ISLAMU Event browser tab.

### EventHorizontalRail
- Structure: a titled section followed by a native horizontal overflow region containing production `EventCard` instances.
- Variants: compact-grid shelf and optional evidence-backed single-row spotlight.
- States: loading, populated, empty (section omitted), and bounded section failure.
- Accessibility: native scrolling remains available to keyboard, wheel, touch, and assistive technology; content order matches DOM order; no drag-only control is required.
- Motion: CSS scroll snap may guide resting positions, but no autoplay or JavaScript carousel dependency is allowed.

### UpcomingEventList
- Structure: a home-discovery-only update list made from columns of at most six compact event links. Each row contains a 7:10 thumbnail, one-line title, schedule/format metadata, and organizer context; it is not an `EventCard` variant.
- Variants: one column on narrow screens, two on tablet, and three on wide layouts. Columns retain top-to-bottom source order before continuing in the next column.
- States: image, generated local image fallback, hover, focus-visible, external-platform action, populated, and section-owned empty/failure messaging. A safe `EventUrl`, or the server-owned HAL `source` redirect for a federated event, adds an independent top-right “Open” link with new-tab isolation. Pointer-capable desktop layouts reveal the tokenized surface action on row hover or focus-within; narrow or coarse-pointer layouts keep its background-free theme-aware label visible at the row's top-right.
- Accessibility: every row is one native event link with an event-specific accessible name and visible focus ring. Adjacent text names the event, so its thumbnail is decorative.
- Motion: no autoplay or repeated animation; hover feedback uses the existing interaction-state tokens.

### EventImageLightbox
- Structure: one real button overlays the displayed event image and opens a portal-backed modal containing that image at its largest viewport-safe size.
- Variants: event-detail cover and shared event-preview sidebar, including generated fallback artwork.
- States: default, hover-dimmed with centered fullscreen icon, focus-visible, active, open, and dismissed.
- Accessibility: the trigger has an action-oriented label; the modal traps focus, closes on Escape/backdrop/outside-image activation, and restores focus to the trigger.
- Motion: only tokenized opacity, background-color, and transform feedback; reduced-motion mode makes transitions effectively instant.

## 6. Public Home Discovery

### Page Order And Content Truth

`/home` is the tenant-aware discovery surface for anonymous and authenticated visitors, and `/events` keeps its existing visual composition. There is no separate marketing-page surface.

The page order is:

1. existing public shell and organization remediation/encoding branch;
2. `HomeDiscoveryHero`, with `PublicDiscoveryAreaControl` as the first row inside its inner grid;
3. “Upcoming in {Area}” using `UpcomingEventList`;
4. optional evidence-backed spotlight using `SingleRow`;
5. compact `EventHorizontalRail` sections;
6. existing tenant footer.

Current-release labels describe only facts supported by the API. Approved labels are “Upcoming in {Area},” “Most viewed in {Area},” “Most viewed online,” an explicit tenant-curated label, and “Recently added.” Do not use “near you,” distance, “trending,” “recommended,” “free,” or unsupported community/grassroots language.

There is no advertisement, CTA substitute, spacer, or reserved ad gap anywhere in the composition. Empty and failed optional sections collapse completely instead of leaving holes.

### Layout Modes

| Mode | Home use | Responsive behavior |
|---|---|---|
| `UpcomingEventList` | “Upcoming in {Area}” | Compact rows flow top-to-bottom in columns of six. Responsive disclosure shows 6 items in one narrow column, 12 across two tablet columns, and 18 across three wide columns. |
| `SingleRow` | Optional spotlight only when the response contains evidence-backed spotlight content. | One readable row; content progressively reduces without hiding the event title or primary link. |
| `CompactGrid` | “Most viewed in {Area},” “Most viewed online,” explicit curation, and “Recently added.” | Fixed readable card width inside native horizontal overflow with a clipped next-card cue. |

Do not add another `EventCard` mode for `/home`. `UpcomingEventList` is a separate semantic update-list primitive; spotlight and horizontal-rail card rendering stay in the production `EventCard`.

### Responsive, Direction, And Motion

- Verify the complete page at 375px, 768px, and 1280px. Content must not produce viewport-level horizontal overflow.
- Component CSS uses the existing spacing/type/color tokens and logical properties. Rails, icon placement, control order, and text alignment must remain usable in RTL without physical left/right declarations.
- Touch targets follow `--isl-target-min`; hero and rail controls never depend on hover.
- The hero carousel automatically advances every nine seconds; other discovery components do not autoplay, pulse, or repeatedly animate. Reduced-motion mode removes non-essential transitions while preserving every action and state change.

### Loading, Empty, Failure, And Hydration

- Preserve the existing PublicSeo render policy and prerendered shell. Persist prerendered discovery state so hydration does not repeat the initial composite request.
- Loading placeholders preserve the final section geometry and image aspect ratios to avoid layout shift.
- An empty optional section is omitted. A required empty result explains that no matching events are available and keeps area/online controls usable.
- A bounded section failure shows safe localized copy for that section; it does not erase successful sections. A composite failure keeps the shell, area control, footer, and a retry action.
- Dynamic load, selection, and error outcomes use the existing accessibility announcer service. Every page still has one structural `h1` and sequential section headings.

### Performance Contract

- Initial discovery uses one composite home API call beyond existing shell/bootstrap reads.
- At most two hero images load eagerly and their initial transfer budget is 500 KiB. Remaining hero and offscreen card images are lazy and sized.
- The raw composite response stays at or below 256 KiB and the compressed response at or below 120 KiB.
- Each server section is bounded to one second and the composite request to three seconds. Target p95 is 800ms uncached and 200ms cached; page LCP target is 2.5 seconds.
- The controlled LCP profile is a fresh-cache 375×844 Chromium page with 4× CPU slowdown, 100ms latency, 4,000 Kbit/s download, and 1,500 Kbit/s upload. The Aspire browser lane records the measured API p95 and LCP values in `performance.json` beside its screenshots.

## 7. Motion & Interaction

| Type | Duration | Easing | Usage |
|------|----------|--------|-------|
| Micro | 150ms | ease | Button, chip, and hover feedback |
| Standard | 250-300ms | ease-in-out | Drawer/panel transitions |
| Dock | `--isl-dock-motion-duration` | `--isl-dock-motion-easing` | Shell dock panels |

Rules:
- Animate only `transform`, `opacity`, `filter`, color, border-color, or box-shadow.
- Respect `prefers-reduced-motion`.
- Motion must communicate state or affordance; support-access risk indicators do not pulse or distract.

## 8. Depth & Surface

Strategy: mixed, with flat cards by default and tokenized shadows only for overlays, menus, and genuinely elevated surfaces.

| Level | Token | Usage |
|-------|-------|-------|
| Border | `--isl-card-border` | Ordinary cards and panels |
| Hover border | `--isl-card-hover-border-color` | Clickable cards |
| Subtle shadow | `--isl-shadow-xs` / `--isl-shadow-sm` | Light elevation |
| Dialog/popover | `--isl-shadow-md` / `--isl-shadow-lg` | Overlays and menus |

Rules:
- Ordinary admin content should prefer borders and tonal separation over heavy shadows.
- Support-access banner uses warning-toned surface treatment plus border, not high elevation.
- MudBlazor `.mud-*` overrides remain limited to `mudblazor-overrides.css`.
