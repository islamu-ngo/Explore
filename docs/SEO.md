ABOUTME: Documents implemented sitemap, robots, render-policy, and public-discovery SEO behavior.
ABOUTME: Separates source-backed SEO features from planned or unsupported site-wide SEO automation claims.

# SEO And Public Discovery

> **Audience:** Operators | Admins | Integrators | Contributors
> **Status:** Mixed
> **Owner:** Frontend
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.API/Controllers/SitemapController.cs`, `Explore.Application/Features/Seo/Handlers/Queries/GetSitemapEventsQueryHandler.cs`, `Explore.Infrastructure/Services/PublicUrlBuilder.cs`, `Explore.Blazor/Controllers/RobotsController.cs`, `Explore.Blazor.Client/Routes.razor`, `Explore.Blazor.Client/Services/RuntimeRenderPolicyService.cs`, `Explore.Application/Features/PublicExperience/`, `Explore.Application/Services/TenantPolicySettingService.Read.cs`, `Explore.Application/Services/TenantPolicySettingService.Apply.cs`, `docs/RENDER_POLICIES.md`, `docs/ADMIN_GUIDE.md`

SEO support is implemented as a focused set of public-discovery primitives: sitemap generation, robots output, render-policy decisions for public routes, and tenant public-experience settings. Do not describe this as full site-wide SEO automation.

## Sitemap Behavior

`GET /sitemap.xml` is public XML output. It combines static public routes with published public event URLs.

| Sitemap Input | Behavior |
|---|---|
| Static routes | Includes public static paths such as `/`, `/events`, `/welcome`, `/about`, `/contact`, `/privacy`, `/terms`, and `/community-guidelines`. |
| Event routes | Includes published public events returned by the sitemap query handler. |
| URL base | Uses the public request base URL, including forwarded host/proto and path base handling. |
| Size boundary | Event projection is clamped for sitemap safety; do not document unlimited event output. |

The sitemap endpoint is source-backed, but sitemap endpoint tests were not identified during the Batch C source map. Query-level sitemap tests exist; avoid claiming broader endpoint test coverage unless new tests are added.

## Robots Behavior

`GET /robots.txt` is public from the Blazor host:

| Environment | Behavior |
|---|---|
| Production | Allows crawling and advertises `/sitemap.xml`. |
| Non-production | Returns `Disallow: /`. |

Robots output uses forwarded host/proto context for the sitemap URL. Do not document non-production environments as indexable.

## Render Policy And Public Routes

Runtime render policy groups routes into onboarding, admin, public SEO, and operational buckets. Public SEO route classification influences prerender behavior, especially under the `SeoBalanced` preset.

Important boundaries:

- Onboarding routes render as `InteractiveServer` regardless of stored render-mode settings.
- Public SEO routes default toward prerendering when advanced overrides do not override the preset.
- Defaults fall back safely when tenant settings are missing.
- `Explore.Blazor.Client/Routes.razor` registers `/home` and `/welcome`; keep route documentation tied to that router mapping and the runtime classifier instead of inventing separate page-level SEO behavior.

See [RENDER_POLICIES.md](RENDER_POLICIES.md) for the canonical render-policy model.

## Public Experience And Tenant Controls

Public discovery settings are tenant-scoped and managed through admin settings rather than a standalone SEO admin page.

| Area | Implemented Control |
|---|---|
| Instance render policy | Instance admin controls presets, advanced overrides, route groups, and tenant delegation locks. |
| Tenant render policy | Tenant admin can override allowed render-policy settings when not locked by instance governance. |
| Tenant public experience | Tenant admin controls public-experience mode, catalog labels, primary organization, home blocks JSON, CTA JSON, and event-section presets. |
| Tenant domains | Tenant admin controls preferred home-page/domain-facing settings through tenant domain/public experience surfaces. |

Tenant resolution and public URL generation are domain-aware. See [MULTI_TENANCY.md](MULTI_TENANCY.md) and [ADMIN_GUIDE.md](ADMIN_GUIDE.md) for the broader administration context.

## Implemented Vs Planned

Implemented:

- Public sitemap endpoint.
- Public robots endpoint with environment-sensitive indexing behavior.
- Public route render-policy classification.
- Event detail page metadata/canonical behavior in the Blazor client.
- Error pages with page-level noindex behavior.
- Tenant public-experience controls for discovery-oriented presentation.

Not proven by inspected source:

- Site-wide dynamic metadata on every route.
- Structured data / JSON-LD automation.
- Canonical URL management for every possible page.
- SEO score auditing, keyword tools, or search-console integrations.
- A standalone SEO admin page.

## Related Documentation

- [RENDER_POLICIES.md](RENDER_POLICIES.md) - render-policy governance.
- [ADMIN_GUIDE.md](ADMIN_GUIDE.md) - public discovery and admin workflow context.
- [MULTI_TENANCY.md](MULTI_TENANCY.md) - tenant and domain resolution.
- [BLAZOR.md](BLAZOR.md) - Blazor render-mode architecture.
