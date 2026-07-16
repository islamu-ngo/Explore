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
- Structure: one labeled disclosure directly above the home hero; the trigger summarizes either “Browsing events in {Area}” or “Browsing online events.”
- Variants: active area and online mode.
- States: loading, selected, locating, location denied/unavailable/no-match, and disabled only while the explicit location action is running.
- Actions: active tenant areas, “Use my current location,” and “Browse online events.” Browser geolocation is never requested during load.
- Accessibility: use native labeled controls and buttons, keep status/error text visible, announce context changes politely, and return focus to the trigger after selection.
- Privacy: persist only stable area ID and mode; never render, persist, log, trace, or analyze the browser origin.

### HomeDiscoveryHero
- Structure: manual featured-event carousel with one active slide, previous/next buttons, swipe support, and a visible position counter.
- Variants: image, image-fallback, and empty/absent. A tenant may expose up to ten slides.
- States: active, previous/next focus and disabled edges, pointer drag/swipe, and reduced-motion.
- Accessibility: controls are real labeled buttons; slide changes are politely announced; title, schedule, location/online context, and event link remain readable without the image.
- Motion: no autoplay. Slide changes use only tokenized opacity/transform motion and become effectively instant under `prefers-reduced-motion`.
- Images: the active image is high priority, at most one likely-next image may also load eagerly, and every remaining image is lazy. Media has explicit dimensions or aspect ratio and meaningful alternative text.

### EventCard
- Structure: the production event card is the only card used by home discovery and `/events`; card-body navigation is a real link or keyboard-equivalent target, while nested share/edit/delete controls remain independent.
- Variants: `DetailedList`, `SingleRow`, and `CompactGrid`.
- States: image, image-fallback, hover, focus-visible, loading placeholder, and server-provided management affordances.
- Accessibility: Enter and Space activate card-body navigation without activating nested controls; focus remains visible; images have meaningful alternative text; metadata is not color-only.
- Authorization: edit/delete and other management actions render only when the matching HAL relation is present. Layout mode never changes authority.

### EventHorizontalRail
- Structure: a titled section followed by a native horizontal overflow region containing production `EventCard` instances.
- Variants: compact-grid shelf and optional evidence-backed single-row spotlight.
- States: loading, populated, empty (section omitted), and bounded section failure.
- Accessibility: native scrolling remains available to keyboard, wheel, touch, and assistive technology; content order matches DOM order; no drag-only control is required.
- Motion: CSS scroll snap may guide resting positions, but no autoplay or JavaScript carousel dependency is allowed.

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
2. `PublicDiscoveryAreaControl`;
3. optional `HomeDiscoveryHero`;
4. “Upcoming in {Area}” using `DetailedList`;
5. optional evidence-backed spotlight using `SingleRow`;
6. compact `EventHorizontalRail` sections;
7. existing tenant footer.

Current-release labels describe only facts supported by the API. Approved labels are “Upcoming in {Area},” “Most viewed in {Area},” “Most viewed online,” an explicit tenant-curated label, and “Recently added.” Do not use “near you,” distance, “trending,” “recommended,” “free,” or unsupported community/grassroots language.

There is no advertisement, CTA substitute, spacer, or reserved ad gap anywhere in the composition. Empty and failed optional sections collapse completely instead of leaving holes.

### Layout Modes

| Mode | Home use | Responsive behavior |
|---|---|---|
| `DetailedList` | “Upcoming in {Area}” | One column at narrow widths, two when space permits, and three on wide layouts. |
| `SingleRow` | Optional spotlight only when the response contains evidence-backed spotlight content. | One readable row; content progressively reduces without hiding the event title or primary link. |
| `CompactGrid` | “Most viewed in {Area},” “Most viewed online,” explicit curation, and “Recently added.” | Fixed readable card width inside native horizontal overflow with a clipped next-card cue. |

Do not create a fourth card layout for `/home`. Section order and mode come from the composite home response, while card rendering stays in the production `EventCard`.

### Responsive, Direction, And Motion

- Verify the complete page at 375px, 768px, and 1280px. Content must not produce viewport-level horizontal overflow.
- Component CSS uses the existing spacing/type/color tokens and logical properties. Rails, icon placement, control order, and text alignment must remain usable in RTL without physical left/right declarations.
- Touch targets follow `--isl-target-min`; hero and rail controls never depend on hover.
- No discovery component autoplays, pulses, or repeatedly animates. Reduced-motion mode removes non-essential transitions while preserving every action and state change.

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
