# Deployment Modes & Customization

ISLAMU Event is designed to be **highly customizable** to support diverse deployment scenarios—from single-organization instances to full SaaS platforms serving multiple tenants. This section covers all customization options.

## Deployment Modes

ISLAMU Event supports two primary deployment modes:

```
┌─────────────────────────────────────────────────────────────────────┐
│                      DEPLOYMENT MODES                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  MODE 1: Single-Instance Deployment                                 │
│  ─────────────────────────────────────                              │
│  • Multi-tenancy DISABLED                                           │
│  • One organization/community per deployment                        │
│  • Simpler configuration and maintenance                            │
│  • Example: ISLAMU's own Islamic events instance                    │
│  • Example: A university running their own event platform           │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              Single Instance                                │    │
│  │  ┌─────────────────────────────────────────────────────┐    │    │
│  │  │  events.islamu.org                                  │    │    │
│  │  │  • All events in one space                          │    │    │
│  │  │  • Single admin team                                │    │    │
│  │  │  • Unified branding                                 │    │    │
│  │  └─────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                     │
│  MODE 2: Multi-Tenant SaaS Deployment                               │
│  ────────────────────────────────────                               │
│  • Multi-tenancy ENABLED                                            │
│  • Multiple isolated organizations/communities                      │
│  • For SaaS providers offering ISLAMU Event as a service            │
│  • Each tenant has custom domain, branding, settings                │
│  • Shared infrastructure, isolated data                             │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              Multi-Tenant SaaS Platform                     │    │
│  │  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐      │    │
│  │  │ Tenant A      │ │ Tenant B      │ │ Tenant C      │      │    │
│  │  │ mosque-a.com  │ │ uni-events.eu │ │ community.org │      │    │
│  │  │ Own settings  │ │ Own settings  │ │ Own settings  │      │    │
│  │  │ Own branding  │ │ Own branding  │ │ Own branding  │      │    │
│  │  └───────────────┘ └───────────────┘ └───────────────┘      │    │
│  │                    Shared Infrastructure                    │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Environment Configuration for Deployment Mode

```yaml
# docker-compose.yml
services:
  explore-api:
    environment:
      # Deployment Mode
      - EXPLORE__MULTITENANCY__ENABLED=false          # true for SaaS mode
      - EXPLORE__MULTITENANCY__DEFAULT_TENANT=default # Used when disabled
      
      # Instance Identity (Single-Instance Mode)
      - EXPLORE__INSTANCE__NAME=ISLAMU Event
      - EXPLORE__INSTANCE__DOMAIN=events.islamu.ngo
      - EXPLORE__INSTANCE__DESCRIPTION=Islamic Event Discovery Platform
```

## Blazor Rendering Modes

ISLAMU Event supports **three Blazor rendering modes** configurable via environment variables:

| Mode | Description | Use Case |
|------|-------------|----------|
| **Server** | Server-side rendering | real-time updates |
| **WebAssembly** | Client-side rendering in browser | Offline capability, reduced server load |
| **Auto** | Server initially, then WebAssembly | Best of both worlds (recommended) |

### Configuration

```yaml
# docker-compose.yml
services:
  explore-blazor:
    environment:
      # Blazor Rendering Mode: Server | WebAssembly | Auto
      - BLAZOR__RENDER_MODE=Auto
      
      # Additional Blazor Settings
      - BLAZOR__PRERENDER=true                    # Enable prerendering for SEO
      - BLAZOR__DETAILED_ERRORS=false             # Show detailed errors (dev only)
      - BLAZOR__WEBSOCKET_COMPRESSION=true        # Compress SignalR traffic
```

```csharp
// Program.cs - Runtime configuration
var renderMode = builder.Configuration["Blazor:RenderMode"] ?? "Auto";

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// In App.razor - Dynamic render mode
@code {
    private IComponentRenderMode RenderMode => 
        Configuration["Blazor:RenderMode"] switch
        {
            "Server" => RenderMode.InteractiveServer,
            "WebAssembly" => RenderMode.InteractiveWebAssembly,
            "Auto" => RenderMode.InteractiveAuto,
            _ => RenderMode.InteractiveAuto
        };
}
```

## Instance-Level Administration

Instance Administrators (not organization admins) can configure platform-wide behavior through the **Instance Settings** panel or environment variables.

### Organization & Event Publishing Policies

Configurable inside the **Instance Settings** panel in the webapp for instance administrator.

```
┌─────────────────────────────────────────────────────────────────────┐
│              INSTANCE-LEVEL POLICY CONFIGURATION                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ORGANIZATION CREATION POLICY                                       │
│  ────────────────────────────                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Option A: Open Registration (Default)                       │   │
│  │  • Anyone can create an organization                         │   │
│  │  • Organizations start as "Unverified"                       │   │
│  │  • Can publish events immediately                            │   │
│  │                                                              │   │
│  │  Option B: Approval Required (ISLAMU Instance)               │   │
│  │  • Anyone can REQUEST to create an organization              │   │
│  │  • Instance admin must APPROVE before org is active          │   │
│  │  • Only approved orgs can publish events                     │   │
│  │                                                              │   │
│  │  Option C: Invite Only                                       │   │
│  │  • Only instance admins can create organizations             │   │
│  │  • Most restrictive, for curated platforms                   │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  USER EVENT PUBLISHING POLICY                                       │
│  ────────────────────────────                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Option A: Users Can Publish (marked as "User Reported")     │   │
│  │  • Individual users can post events                          │   │
│  │  • Events flagged with "User Reported" badge                 │   │
│  │  • Subject to community moderation                           │   │
│  │                                                              │   │
│  │  Option B: Users Cannot Publish Directly                     │   │
│  │  • Only organizations can publish events                     │   │
│  │  • Higher quality control                                    │   │
│  │                                                              │   │
│  │  Option C: Users Publish with Approval                       │   │
│  │  • Users submit events for review                            │   │
│  │  • Instance moderators approve before publishing             │   │
│  │  • Balance between openness and quality                      │   │
│  │                                                              │   │
│  │  Option D: Users Publish without approval nor verification   │   │
│  │  • Users submit events                                       │   │
│  │  • fully open                                                │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```


## Tenant-Level Configuration (BYOK - Bring Your Own Keys)

Tenant-level BYOK integrations are a **roadmap** capability.

**Current state**:

- Object storage integration exists (S3-compatible) via `Explore.Infrastructure`.
- Other per-tenant integrations (analytics, payments, AI services, email/SMS routing) are not implemented yet.

```
┌─────────────────────────────────────────────────────────────────────┐
│               TENANT BYOK INTEGRATIONS (PLANNED)                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ANALYTICS (planned)                 PAYMENTS (planned)              │
│  ─────────                          ────────                        │
│  • Google Analytics                 • Stripe                        │
│  • Plausible Analytics                                              │
│  • PostHog                                                          │
│                                                                     │
│                                                                     │
│                                                                     │
│                                                                     │
│  AI SERVICES (planned)                                               │
│  ───────────                                                        │
│  • OpenAI                                                           │
│  • Anthropic Claude                                                 │
│  • Azure OpenAI                                                     │
│  • Ollama (self-hosted)                                             │
│  • Custom LLM endpoint                                              │
│                                                                     │
│  EMAIL & SMS (planned)               STORAGE                          │
│  ───────────                        ───────                         │
│  • SendGrid                         • AWS S3                        │
│  • Mailgun                          • Azure Blob                    │
│  • Amazon SES                       • Google Cloud Storage          │
│  • Twilio (SMS)                     • MinIO (self-hosted)           │
│  • Custom SMTP                      • Local filesystem              │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```



## Organization-Level Integrations

Organization administrators can configure their own notification channels and webhooks for their organization's events.

```
┌─────────────────────────────────────────────────────────────────────┐
│              ORGANIZATION INTEGRATION OPTIONS                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  NOTIFICATION CHANNELS                                              │
│  ─────────────────────                                              │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Slack Integration                                           │   │
│  │  • New event created → Post to channel                       │   │
│  │  • New registration → Notify organizers                      │   │
│  │  • Event reminder → Scheduled messages                       │   │
│  │  • Capacity alert → Warning when nearly full                 │   │
│  │                                                              │   │
│  │  Telegram Integration                                        │   │
│  │  • Bot notifications to group/channel                        │   │
│  │  • Event announcements                                       │   │
│  │  • Registration confirmations                                │   │
│  │                                                              │   │
│  │  Email Notifications                                         │   │
│  │  • Customizable templates                                    │   │
│  │  • Digest options (immediate/daily/weekly)                   │   │
│  │  • Role-based routing                                        │   │
│  │                                                              │   │
│  │  Discord Integration                                         │   │
│  │  • Webhook-based notifications                               │   │
│  │  • Rich embeds for events                                    │   │
│  │                                                              │   │
│  │  Matrix Integration                                          │   │
│  │  • Room notifications                                        │   │
│  │  • Decentralized messaging                                   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  WEBHOOKS                                                           │
│  ────────                                                           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Configurable webhooks for all event types:                  │   │
│  │                                                              │   │
│  │  Event Lifecycle:                                            │   │
│  │  • event.created        • event.published                    │   │
│  │  • event.updated        • event.cancelled                    │   │
│  │  • event.deleted        • event.started                      │   │
│  │  • event.ended          • event.reminder (configurable)      │   │
│  │                                                              │   │
│  │  Participation:                                              │   │
│  │  • participant.registered    • participant.cancelled         │   │
│  │  • participant.checked_in    • participant.waitlisted        │   │
│  │  • capacity.threshold_reached                                │   │
│  │                                                              │   │
│  │  Organization:                                               │   │
│  │  • organization.member_joined   • organization.member_left   │   │
│  │  • organization.verified        • organization.settings_changed│  │
│  │                                                              │   │
│  │  Moderation:                                                 │   │
│  │  • report.created       • report.resolved                    │   │
│  │  • comment.flagged      • content.removed                    │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```
