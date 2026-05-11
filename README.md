<a name="readme-top"></a>

<div align="center">

# ISLAMU Event

Self-hostable, open-source event discovery and management software for communities, organizations, and platform operators.

ISLAMU Event powers ISLAMU’s Islamic events instance, but the software itself is purpose-agnostic, white-label, and designed to be rebranded for any event ecosystem.

> Pre-1.0 notice: ISLAMU Event is still before v1. Breaking changes may happen between releases. We avoid data-loss-class breaks where possible, but configuration changes may be required.

![GitHub Workflow Status][github-workflow-status-shield]
[![GitHub License][github-license-shield]][github-license-link]
[![GitHub Repo Stars][github-stars-shield]][github-stars-link]
[![GitHub Last Commit][github-last-commit-shield]][github-last-commit-link]
[![Contributors][github-contributors-shield]][github-contributors-link]
[![Discussions][github-discussions-shield]][github-discussions-link]
[![Discord][discord-shield]][discord-link]

[**ISLAMU Islamic Events Instance**][islamu-platform] · [**Quick Start**](#-quick-start) · [**Docs**](docs/index.md) · [**Roadmap**][roadmap-link]

</div>

---

## About ISLAMU Event

ISLAMU Event is a **self-hostable event discovery and management platform** for publishing, discovering, and operating events across one organization or many isolated tenants.

The public ISLAMU instance is Islamic-focused, but the software itself is purpose-agnostic and designed to be rebranded for any event ecosystem.

It is built as a **white-label platform engine**: the hosted ISLAMU instance focuses on Islamic events, while the software is purpose-agnostic and designed to be rebranded for any event ecosystem.

![Event List Screenshot][event-list-image]

## ✨ Who It Serves

### Event Seekers

- **🔍 Advanced Discovery:** Event search with title, dates, location, categories, tags, and paging filters
- **👨‍👩‍👧‍👦 Cultural Intelligence:** Culturally-Aware Filters for Age ranges, gender segregation modes, madhab targeting, prayer-relative timing (for instances that enable these modules)
- **🌐 Multi-Language Support:** Event sessions with multiple language options
- **📱 PWA & Responsive Design:** Mobile-friendly Blazor UI with MudBlazor components
- **✅ RSVP & Registration:** Waitlists, approval workflows, registration limits per session

### Event Organizers

- **📅 Signle & Multi-Session Events:** Conferences, seminars, recurring programs with speakers, agendas, and language variants
- **📊 Flexible Publishing:** Manage registrations, waitlists, approval workflows, capacity limits, and event visibility.
- **👥 Member Management:** Invite members, assign roles (Owner, Admin, Editor, Viewer), track permissions
- **🎯 Modular Event Types:** Data Modeling: custom fields, event aspects, event templates...

### Platform Owners & Self Hosters

- **🐳 Deployment:** Docker ready
- **💼 Multi-Tenancy:** Switch between single-tenant and SaaS modes at runtime without code changes
- **🛠️ White-Label Control:** Custom branding, domains, logos, navigation links, policies per tenant
- **🔧 Admin Hierarchy:** Instance admins, tenant admins, and organization admins with cascading settings
- **🌍 Federation Foundation:** ATProto-oriented models and outbound sync plumbing exist, while public ActivityPub and ATProto server endpoints remain roadmap work
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

- **🔐 Authentication:** OAuth 2.0/OIDC via **Keycloak**
- **🛡️ Authorization:** Runtime provider switching via system setting (Cerbos PDP or local DB-backed provider), with optional tenant BYO Cerbos
- **Tenant Isolation:** API-authoritative tenant resolution with EF Core global query filters for tenant-scoped data
- **🔍 Input Validation:** FluentValidation + ASP.NET Core model binding
- **🚫 Data Integrity:** Parameterized EF Core queries to eliminate SQL injection
- **🗝️ Secret Management:** Environment-first secret loading with optional Infisical compatibility
- **Observability:** OpenTelemetry + structured logging with Serilog
- **🔒 HTTPS Hardening:** HTTPS redirection and production HSTS support
- **🌐 CORS:** Configurable origin whitelist
- **⏱️ Rate Limiting:** ASP.NET Core middleware

## Roadmap

The roadmap is tracked publicly in the [Roadmap Kanban View][roadmap-link]. Use it to follow planned work, vote, comment,

![Roadmap Kanban View][roadmap-image]

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

Start with [Contributing](docs/CONTRIBUTING.md). Code contributors should also read [Governance](docs/GOVERNANCE.md), [Quick Reference](docs/QUICK_REFERENCE.md), and [Architecture](docs/ARCHITECTURE.md).

AI-assisted contributors should follow [`CLAUDE.md`](CLAUDE.md) and [`AGENTS.md`](AGENTS.md).

Please read [Contribution Guidelines][contribution-guidelines] for details on the process for submitting pull requests to us.

## 📚 Documentation

| Reader | Start here | Then read |
|---|---|---|
| New self-hoster | [Self-Hosting](docs/SELF_HOSTING.md) | [Configuration](docs/CONFIGURATION.md), [Operations](docs/OPERATIONS.md), [Troubleshooting](docs/TROUBLESHOOTING.md) |
| Contributor | [Contributing](docs/CONTRIBUTING.md) | [Governance](docs/GOVERNANCE.md), [Quick Reference](docs/QUICK_REFERENCE.md), [Architecture](docs/ARCHITECTURE.md) |
| API integrator | [API](docs/API.md) | [API Cookbook](docs/API_COOKBOOK.md), [Domain Model](docs/DOMAIN.md), [Security Model](docs/SECURITY-MODEL.md) |
| Platform operator | [Deployment Modes](docs/DEPLOYMENT_MODES.md) | [Multi-Tenancy](docs/MULTI_TENANCY.md), [Admin Hierarchy](docs/ADMIN_HIERARCHY.md), [Backup/Restore/Upgrade](docs/BACKUP_RESTORE_UPGRADE.md) |
| UI/frontend contributor | [Blazor](docs/BLAZOR.md) | [Design System](docs/DESIGN_SYSTEM.md), [Accessibility](docs/ACCESSIBILITY.md), [Render Policies](docs/RENDER_POLICIES.md) |

Full documentation index: [docs/index.md](docs/index.md).

## 🏗️ Technology Stack (v0.1.0)

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Architecture | Clean Architecture, CQRS, MediatR |
| UI | Blazor Server with InteractiveServer, MudBlazor |
| API | ASP.NET Core, REST/HAL, OpenAPI, Swagger, Scalar |
| Data | PostgreSQL, EF Core |
| Auth | Keycloak OIDC/OAuth2 |
| Authorization | Cerbos or local provider |
| Secrets | Environment variables and Infisical-compatible provider abstraction |
| Observability | Serilog, OpenTelemetry |
| Deployment | Docker Compose, .NET Aspire for development |
| Tests | TUnit, bUnit, integration and architecture tests |

## Community

Join the ISLAMU community on [Discord][discord-link] and our [GitHub Discussions][github-discussions-link]. We follow a [Code of Conduct][code-of-conduct] in all our community channels.

Feel free to ask questions, report bugs, participate in discussions, share ideas, request features, or showcase. We would love to hear from you.

> Give us a Star ⭐️

## 🛡️ Security Disclosure

If you discover a security vulnerability in ISLAMU Event, please report it responsibly instead of opening a public issue. We take all legitimate reports seriously and will investigate them promptly. See the [Security Policy][security-policy] for more info.

To disclose any security issues, please email us at [contact@openislamu.org][contact-email].

## Contributors

### Core Maintainer

|                                                                                                                                                                            Amir Akrari                                                                                                                                                                             |
| :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: |
|                                                                                                                                         <img src="https://github.com/amirakrari.png" width="200px" alt="Amir Akrari" />                                                                                                                                          |
| <a href="https://github.com/amirakrari"><img src="https://api.iconify.design/devicon:github.svg" width="25px"></a> <a href="https://bsky.app/profile/amirakrari.bsky.social"><img src="https://api.iconify.design/simple-icons:bluesky.svg" width="25px"></a> |

I am deeply grateful to all our amazing contributors.

[![Contributors Image][contributors-image]][contributors-link]

## 📊📈 Repo Stats

![Repo Stats][repobeats-image]

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
[security-doc]: docs/SECURITY-MODEL.md
[deployment-modes-doc]: docs/DEPLOYMENT_MODES.md
[extensibility-doc]: docs/EXTENSIBILITY.md
[modular-events-doc]: docs/MODULAR_EVENTS.md
[render-policies-doc]: docs/RENDER_POLICIES.md
[security-policy]: SECURITY-POLICY.md
[privacy-policy]: https://openislamu.org/privacy
[license-link]: LICENSE
[contact-email]: mailto:contact@openislamu.org

[github-repo-link]: https://github.com/islamu-ngo/Event
[github-issues-link]: https://github.com/islamu-ngo/Event/issues/new
[github-discussions-link]: https://github.com/islamu-ngo/Event/discussions
[github-contributors-link]: https://github.com/islamu-ngo/Event/graphs/contributors

[github-workflow-status-shield]: https://img.shields.io/github/actions/workflow/status/islamu-ngo/Event/test.yml?branch=main&logo=github&style=flat-square
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
