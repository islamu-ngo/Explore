<a name="readme-top"></a>

<div align="center">

# ISLAMU Event

Event Platform & Management System

ISLAMU Event is a self-hostable, white-label event discovery and management platform for communities, organizations, and SaaS operators.
The public ISLAMU instance is Islamic-focused, but the software itself is purpose-agnostic and designed to be rebranded for any event ecosystem.

Until we reach version 1 you should expect breaking changes from release to release. Watch the changelogs to learn about them.
We try to not introduce breaking changes that result in a definitive loss of data, but you should expect to have to redo your configuration from time to time.

![GitHub Workflow Status][github-workflow-status-shield]
[![Codecov][codecov-shield]][codecov-link]
[![Quality Gate Status][sonarcloud-shield]][sonarcloud-link]
[![GitHub License][github-license-shield]][github-license-link]
[![GitHub Repo Stars][github-stars-shield]][github-stars-link]
[![GitHub Last Commit][github-last-commit-shield]][github-last-commit-link]
[![Contributors][github-contributors-shield]][github-contributors-link]
[![Discussions][github-discussions-shield]][github-discussions-link]
[![Discord][discord-shield]][discord-link]

[**Public Instance**][islamu-platform] · [**Quick Start**](#-quick-start) · [**Docs**](docs/index.md) · [**Roadmap**][roadmap-link]

</div>

---

## ℹ About ISLAMU Event

ISLAMU Event is a **self-hostable event discovery and management platform** for publishing, discovering, and operating events across one organization or many isolated tenants.

It is built as a **white-label platform engine**: the hosted ISLAMU instance focuses on Islamic events, while the core software can be rebranded for conferences, tech communities, nonprofits, education, local groups, and other event-driven ecosystems.

> Give us a Star ⭐️

### Platform Flexibility
- **The software is general-purpose**: Adaptable for any kind of events, communities, and organizations (tech meetups, conferences, workshops, community gatherings)
- **The ISLAMU-hosted instance is Islamic-focused**: Our public instance at [event.openislamu.org](https://event.openislamu.org) is curated for Islamic events and community needs
- **White-label ready**: Rebrand and customize the platform for your specific use case with full control over branding, policies, and features

![Event List Screenshot][event-list-image]

## 🚀 Quick Start

### 🌐 Try the Public Instance

Visit **[event.openislamu.org](https://event.openislamu.org)** to:
- 🔍 browse events without an account
- 📝 create an account and publish events
- ✅ register for events
- 👥 follow organizations

### 🐳 Run with Docker

**Prerequisites:**
- Git
- Docker
- Docker Compose v2+

**Quick deploy:**
```bash
# Clone the repository
git clone https://github.com/islamu-ngo/Event.git && cd Event

# Start core services (UI, API, PostgreSQL, Keycloak)
docker compose up -d

# Optional: start with object storage support
docker compose up --profile storage -d
```

**Default local endpoints:**
- Blazor UI: `http://localhost:7002`
- API: `http://localhost:7039`
- Swagger UI: `http://localhost:7039/swagger`

### 🛠️ Run for Local Development

**Prerequisites:**
- .NET 10 SDK (solution targets `net10.0`; preview SDK pinned in `global.json`)
- Docker or local equivalents for required infrastructure

**Typical local dev endpoints:**
- Blazor: `https://localhost:7177`
- API: `https://localhost:7039`

### 🔐 First Run

- If onboarding is not completed yet, the Blazor root route redirects to `/setup`
- When `SETUP_SECRET` is not set, API startup logs print an auto-generated setup secret valid for 60 minutes
- Production deployments still require real configuration for domains, TLS, secrets, and external integrations

**For detailed deployment instructions:**
- [Operations Guide](docs/OPERATIONS.md) — production deployment and hosting
- [Configuration Guide](docs/CONFIGURATION.md) — environment variables and runtime settings

### 👩‍💻 Contributor Fast-Track

If you plan to contribute code, start with:
- [Contribution Guidelines](docs/CONTRIBUTING.md)
- [Governance](docs/GOVERNANCE.md)
- [Quick Reference](docs/QUICK_REFERENCE.md)
- [Architecture](docs/ARCHITECTURE.md)

## ✨ Capabilities

### Why Teams Choose It

- **🔐 Security-First:** BFF pattern (no browser token storage), runtime authorization provider (Cerbos or local), secret provider abstraction
- **🛡️ Verified Organizations:** Two-tier verification system designed to build trust and content quality
- **⚡ Multi-Tenancy:** Runtime mode switching between single-tenant and SaaS-style deployments without code changes
- **🔧 Modular Events:** Plugin-style aspects (Islamic, Tech) with per-tenant enablement
- **🎯 Cultural Intelligence:** Advanced filtering based on enabled modules, including madhab, gender, age, prayer-relative timing, and skill level
- **🏗️ Enterprise Architecture:** Clean Architecture + CQRS with MediatR, REST Level 3 (HATEOAS)
- **🌍 Federation-Ready:** ATProto data models complete (Phase 1), with ActivityPub and protocol endpoints planned for `1.0.0`
- **🧪 Test Strategy:** Unit, integration, UI, and architecture test projects are already in place
- **📖 Open Source:** AGPL-3.0 licensed for transparency and community ownership

### For Event Seekers

- **🔍 Advanced Discovery:** Event search with title, dates, location, categories, tags, and paging filters
- **👨‍👩‍👧‍👦 Culturally-Aware Filters:** Age ranges, gender segregation modes, madhab targeting, prayer-relative timing (for instances that enable these modules)
- **🌐 Multi-Language Support:** Event sessions with multiple language options
- **📱 Responsive Design:** Mobile-friendly Blazor UI with MudBlazor components
- **✅ RSVP & Registration:** Waitlists, approval workflows, registration limits per session

### For Event Organizers

- **📅 Multi-Session Events:** Conferences, seminars, recurring programs with speakers, agendas, and language variants
- **🛡️ Organization Verification:** Two-tier system (user-submitted vs. verified organizations)
- **👥 Member Management:** Invite members, assign roles (Owner, Admin, Editor, Viewer), track permissions
- **⭐ Reviews & Ratings:** Review verified organizations to build community trust
- **🎯 Modular Event Types:** Enable Islamic or Tech aspects per event based on tenant configuration
- **📊 Flexible Publishing:** Open registration, approval-required, or invite-only policies
- **🧩 Composition-Based Event Modeling:** Mix aspects such as Islamic and Tech on the same event without class explosion

### For Platform Owners

- **🐳 Docker-Ready:** Fast local deployment with `docker compose up -d`
- **💼 Multi-Tenancy:** Switch between single-tenant and SaaS modes at runtime without code changes
- **🧭 API-Authoritative Tenant Isolation:** Blazor handles routing convenience while the API owns tenant identity and data isolation
- **🛠️ White-Label Control:** Custom branding, domains, logos, navigation links, policies per tenant
- **🔧 Admin Hierarchy:** Instance admins, tenant admins, and organization admins with cascading settings
- **🌍 Federation Foundation:** ATProto data models complete (Phase 1), ActivityPub and protocol endpoints planned for 1.0.0
- **📚 Comprehensive Docs:** Architecture, deployment, configuration, troubleshooting, and API reference
- **🔐 Enterprise Security:** BFF pattern, Cerbos authorization, Infisical secrets, HATEOAS REST API

## Deployment & Hosting Options

This platform is designed to be flexible and self-hostable for any organization.

- **Single-Instance Mode**: One organization or community per deployment
- **Multi-Tenant SaaS Mode**: Multiple isolated tenants with custom domains and branding

See [Operations Guide][operations-doc] for full deployment details and examples.

## 🎨 Branding & Customization

ISLAMU Event is built for white-label use:

- **Change instance name and domain**
- **Customize logos, colors, and UI**
- **Define your own categories, tags, and policies**
- **Decide who can publish events and how verification works**

See [Configuration Guide][configuration-doc] for full customization options.

## 🛡️ Enterprise Security & Compliance

We treat security as a first-class citizen, not an afterthought.

- **Identity & Access:** OAuth2/OIDC via **Keycloak**
- **Authorization:** Runtime provider switching via system setting (Cerbos PDP or local DB-backed provider), with optional tenant BYO Cerbos
- **Tenant Isolation:** API-authoritative tenant resolution with EF Core global query filters for tenant-scoped data
- **Data Integrity:** Parameterized EF Core queries to eliminate SQL injection
- **Secret Management:** Secret provider abstraction for environment variables and Infisical-compatible loading
- **Observability:** OpenTelemetry + structured logging with Serilog

### Security Features

- **🔒 HTTPS Enforcement:** All traffic encrypted with HSTS
- **🔐 Modern Authentication:** OAuth 2.0/OIDC via Keycloak
- **🗝️ Secret Management:** Environment-first secret loading with optional Infisical compatibility
- **🛡️ Authorization:** Runtime provider: Cerbos PDP or local authorization service
- **🔍 Input Validation:** FluentValidation + ASP.NET Core model binding
- **🚫 SQL Injection Prevention:** Parameterized queries (EF Core)
- **🌐 CORS:** Configurable origin whitelist
- **⏱️ Rate Limiting:** ASP.NET Core middleware

### Extensibility Model

Events use **composition over inheritance**:
`Event` can optionally have `EventIslamicAspect` and/or `EventTechAspect` records (1:1 shared-key extensions).

A "Ramadan Tech Workshop" has both Islamic and Tech aspects. No class explosion.

See [EXTENSIBILITY.md](docs/EXTENSIBILITY.md) and [MODULAR_EVENTS.md](docs/MODULAR_EVENTS.md).

## Roadmap

[Roadmap Kanban View][roadmap-link]: All the work items, go vote, comment, and more.

![Roadmap Kanban View][roadmap-image]

## Community

Join the ISLAMU community on [Discord][discord-link] and our [GitHub Discussions][github-discussions-link]. We follow a [Code of Conduct][code-of-conduct] in all our community channels.

Feel free to ask questions, report bugs, participate in discussions, share ideas, request features, or showcase. We would love to hear from you.

## 🤝 Contributing

There are many ways you can contribute to ISLAMU Event:

**Non-Technical:**
- 🐛 [Report bugs][github-issues-link]
- 💡 [Suggest features][github-issues-link]
- 📖 Improve documentation
- 🌐 Translate UI/docs
- 📣 Spread the word

**Technical:**
- 💻 Fix bugs
- ✨ Implement features
- 🧪 Write tests
- 📊 Improve performance
- 🎨 Enhance UI/UX

### Development Guidelines

Before contributing code, please review:
- **[Contribution Guidelines](docs/CONTRIBUTING.md)** — contributor workflow and expectations
- **[GOVERNANCE.md](docs/GOVERNANCE.md)** — Code conventions and architectural rules
- **[QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md)** — critical rules and project-specific constraints
- **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** — Clean Architecture patterns and CQRS implementation
- **[v0.1.0 Release Notes](docs/semantic_versioning/v0.1.0.md)** — Current feature set and known limitations

If you are using AI-assisted workflows, `CLAUDE.md` documents the repo-specific operating rules used by coding agents.

### How to Contribute

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/foobar`)
3. Commit your changes (`git commit -m "Add feature"`)
4. Push to the branch (`git push origin feature/foobar`)
5. Create a new Pull Request

Before opening a PR:
- build the solution in Release mode
- run the affected test projects individually
- update docs when behavior, configuration, or operations change

Please read [Contribution Guidelines][contribution-guidelines] for details on the process for submitting pull requests to us.

## 📚 Documentation

Start here for deeper details and technical guides:

### If You Want to Self-Host

- [Operations Guide][operations-doc]
- [Configuration & Environment][configuration-doc]
- [Deployment Modes][deployment-modes-doc]
- [Troubleshooting][troubleshooting-doc]

### If You Want to Contribute

- [Project Overview][project-doc]
- [Architecture][architecture-doc]
- [Governance][governance-doc]
- [Quick Reference][quick-reference-doc]

### If You Want Platform Context

- [Master Reference][master-reference-doc]
- [Multi-Tenancy][multi-tenancy-doc]
- [Admin Hierarchy][admin-hierarchy-doc]
- [Rendering Policies][render-policies-doc]
- [Extensibility][extensibility-doc]
- [Modular Events][modular-events-doc]

### If You Want Integration Details

- [API Reference][api-doc]
- [Domain Model][domain-doc]
- [Security][security-doc]

## 🏗️ Technical Overview

ISLAMU Event is built on **Clean Architecture + CQRS** with a **BFF (Backend-for-Frontend)** pattern.

Typical request flow:

`Browser -> Blazor BFF -> API -> Application handlers -> EF Core/PostgreSQL`

### Technology Stack (v0.1.0)

- Runtime: .NET 10 (`net10.0`).
- Architecture: Clean Architecture + CQRS (MediatR).
- Frontend: Blazor Server + WASM (InteractiveAuto).
- UI components: MudBlazor.
- Database: PostgreSQL.
- ORM: Entity Framework Core 10.
- Authentication: Keycloak (OIDC/OAuth 2.0).
- Authorization: Cerbos or local provider (runtime-selected).
- Secrets: environment variables + Infisical compatibility layer.
- API docs: OpenAPI + Swagger + Scalar.
- Logging: Serilog (structured logs).
- Telemetry: OpenTelemetry (traces + metrics).
- Orchestration: .NET Aspire (dev), Docker (deployment).
- Testing: TUnit + bUnit + integration test projects.

## 🛡️ Security

If you discover a security vulnerability in ISLAMU Event, please report it responsibly instead of opening a public issue. We take all legitimate reports seriously and will investigate them promptly. See the [Security Policy][security-policy] for more info.

To disclose any security issues, please email us at [contact@openislamu.org][contact-email].

## 📊📈 Repo Stats

![Repo Stats][repobeats-image]

## Contributors

I am deeply grateful to all our amazing contributors.

[![Contributors Image][contributors-image]][contributors-link]

## 🙏 Acknowledgement

- [Keycloak][keycloak-link]: An Open Source Identity and Access Management Provider.
- [Cerbos][cerbos-link]: An Open Source Policy Decision Point.
- [Svix][svix-link]: An Open Source Webhooks Service.
- [Infisical][infisical-link]: An Open Source Secret Management Platform.
- [MudBlazor][mudblazor-link]: An Open Source Blazor UI library that simplifies the creation of beautiful websites and webapps.
- [Penpot][penpot-link]: An Open Source Design Tool.
- [Plane][plane-link]: An Open Source Project Management Platform that unifies projects, knowledge and agents with all-in-one workspace: projects, wiki, and AI.
- [Coolify][coolify-link]: An Open Source Platform as a Service, alternative to Vercel, Heroku, Netlify, and Railway for easy deploying to your own servers.
- [Kener][kener-link]: An Open Source Status Page.

## Inspiration (UI/...)

- [Luma][luma-link]: A Modern Event Management & Discovery Platform
- [Smoke Signals][smoke-signals-link]: An Event & RSVP Management and Discovery Web Application built on top of ATProtocol.
- [Mangadex][mangadex-link]: A Manga Discovery Platform with advanced filtering and multi-language support.

## ISLAMU Solutions

- [ISLAMU Event][github-repo-link]: Event Platform & Management System.

## 📞 Contact

For any question or problem reporting, please consider opening a [new issue][github-issues-link] or send an email to [contact@openislamu.org][contact-email] or create a new post in our [Discord Server][discord-link].

## Privacy Policy

You can find details [here][privacy-policy].

## 📄 License

This project is licensed under the terms of [GNU AGPL v3][license-link].

## Quick Note

The tyranny of Israel on the Palestinian people is horrifying and heartbreaking. As such, we all
should try our best to support the Palestinians from our position. Consider supporting the Palestinians
by donating to the [Palestinian Red Crescent Society][palestinian-red-crescent].

[![ReadMeSupportPalestine][support-palestine-banner]][palestinian-red-crescent]

Banner from: [support-palestine-banner repository][support-palestine-banner-source]

<div align="right">

[![][back-to-top]][back-to-top-link]

</div>

<!-- LINK GROUP -->

[back-to-top]: https://img.shields.io/badge/-BACK_TO_TOP-151515?style=flat-square
[back-to-top-link]: #readme-top
[islamu-platform]: https://event.openislamu.org
[event-list-image]: assets/event-list-image.png
[roadmap-link]: https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988
[roadmap-image]: assets/Roadmap%20Kanban%20View.png
[code-of-conduct]: CODE_OF_CONDUCT.md
[master-reference-doc]: docs/index.md
[project-doc]: docs/PROJECT.md
[architecture-doc]: docs/ARCHITECTURE.md
[api-doc]: docs/API.md
[domain-doc]: docs/DOMAIN.md
[operations-doc]: docs/OPERATIONS.md
[configuration-doc]: docs/CONFIGURATION.md
[troubleshooting-doc]: docs/TROUBLESHOOTING.md
[governance-doc]: docs/GOVERNANCE.md
[quick-reference-doc]: docs/QUICK_REFERENCE.md
[multi-tenancy-doc]: docs/MULTI_TENANCY.md
[admin-hierarchy-doc]: docs/ADMIN_HIERARCHY.md
[security-doc]: docs/SECURITY.md
[deployment-modes-doc]: docs/DEPLOYMENT_MODES.md
[extensibility-doc]: docs/EXTENSIBILITY.md
[modular-events-doc]: docs/MODULAR_EVENTS.md
[render-policies-doc]: docs/RENDER_POLICIES.md
[security-policy]: SECURITY-POLICY.md
[privacy-policy]: PRIVACY-POLICY.md
[license-link]: LICENSE
[contact-email]: mailto:contact@openislamu.org

[github-repo-link]: https://github.com/islamu-ngo/Event
[github-issues-link]: https://github.com/islamu-ngo/Event/issues/new
[github-discussions-link]: https://github.com/islamu-ngo/Event/discussions
[github-contributors-link]: https://github.com/islamu-ngo/Event/graphs/contributors

[sonarcloud-shield]: https://sonarcloud.io/api/project_badges/measure?project=islamu-ngo_Explore&metric=alert_status
[sonarcloud-link]: https://sonarcloud.io/summary/overall?id=islamu-ngo_Explore
[github-workflow-status-shield]: https://img.shields.io/github/actions/workflow/status/islamu-ngo/Event/build.yml?branch=main&logo=github&style=flat-square
[codecov-shield]: https://img.shields.io/codecov/c/github/islamu-ngo/Event
[codecov-link]: https://app.codecov.io/github/islamu-ngo/Event
[github-stars-shield]: https://img.shields.io/github/stars/islamu-ngo/Event?color=594ae2&style=flat-square&logo=github
[github-stars-link]: https://github.com/islamu-ngo/Event/stargazers
[github-license-shield]: https://img.shields.io/github/license/islamu-ngo/Event?color=594ae2&logo=github&style=flat-square
[github-license-link]: https://github.com/islamu-ngo/Event/blob/main/LICENSE
[github-last-commit-shield]: https://img.shields.io/github/last-commit/islamu-ngo/Event?color=594ae2&style=flat-square&logo=github
[github-last-commit-link]: https://github.com/islamu-ngo/Event
[github-contributors-shield]: https://img.shields.io/github/contributors/islamu-ngo/Event?color=594ae2&style=flat-square&logo=github
[github-discussions-shield]: https://img.shields.io/github/discussions/islamu-ngo/Event?color=594ae2&logo=github&style=flat-square
[discord-shield]: https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square
[discord-link]: https://discord.gg/wrkY824Yv5

[repobeats-image]: https://repobeats.axiom.co/api/embed/a0f11a3d9b80342b5f5965127c2c45871c9d3397.svg
[contributors-image]: https://contrib.rocks/image?repo=islamu-ngo/Event
[contributors-link]: https://github.com/islamu-ngo/Event/graphs/contributors

[keycloak-link]: https://www.keycloak.org/
[cerbos-link]: https://www.cerbos.dev/
[svix-link]: https://www.svix.com/
[infisical-link]: https://infisical.com/
[mudblazor-link]: https://www.mudblazor.com/
[penpot-link]: https://penpot.app/
[plane-link]: https://plane.so/
[coolify-link]: https://coolify.io/
[kener-link]: https://kener.ing/
[luma-link]: https://luma.com/
[smoke-signals-link]: https://smokesignal.events/
[mangadex-link]: https://mangadex.org/

[palestinian-red-crescent]: https://www.palestinercs.org/en/Donation
[support-palestine-banner]: https://github.com/Safouene1/support-palestine-banner/blob/master/banner-support.svg
[support-palestine-banner-source]: https://github.com/Safouene1/support-palestine-banner/

[contribution-guidelines]: docs/CONTRIBUTING.md
