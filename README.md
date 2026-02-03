<a name="readme-top"></a>

<div align="center">

# ISLAMU Event

# Event Platform & Management System in Heavy Development

ISLAMU Event is a self-hostable, decentralized, multi-tenant event platform for any community or industry. 
The ISLAMU organization hosts a public instance focused on Islamic events, but the software itself is purpose-agnostic, fully white-label, and designed to be rebranded for any use case.

![GitHub Workflow Status][github-workflow-status-shield]
[![Codecov][codecov-shield]][codecov-link]
[![Quality Gate Status][sonarcloud-shield]][sonarcloud-link]
[![GitHub License][github-license-shield]][github-license-link]
[![GitHub Repo Stars][github-stars-shield]][github-stars-link]
[![GitHub Last Commit][github-last-commit-shield]][github-last-commit-link]
[![Contributors][github-contributors-shield]][github-contributors-link]
[![Discussions][github-discussions-shield]][github-discussions-link]
[![Discord][discord-shield]][discord-link]

[**ISLAMU's Islamic Instance**][islamu-platform] · [**Docs**](docs/index.md) · [**Roadmap**][roadmap-link]

</div>

---

## ℹ About ISLAMU Event

ISLAMU Event is an Event Platform & Management System built for both allowing the best Event Discovery and Event Management Experience.

> Give us a Star ⭐️

- **The software is general-purpose**: it can be used for any kind of Events, Communities, and Organizations.
- **The ISLAMU-hosted instance is Islamic-focused**: ISLAMU’s public instance is curated for Islamic Events and Community needs.

## ✨ Why ISLAMU Event

### Key Differentiators

- **🌍 Decentralized by Design:** Federation-ready architecture with ATProto / ActivityPub planned
- **🔐 Privacy-First:** Self-hostable with complete data ownership
- **🎯 Cultural Intelligence:** Advanced filtering by madhab, gender, age, and prayer times
- **🛡️ Verified Organizations:** Two-tier verification system for trust and quality
- **Runtime Mode Switching** Single-tenant ↔ Multi-tenant without code changes
- **Policy-Based Rendering** Blazor render modes controlled by policy, not hardcoded
- **📖 Open Source:** AGPL-3.0 licensed for transparency and community ownership

---

## 🎯 Core Features

### For Event Seekers

- **🔍 Powerful discovery** Search & filter events with location, category, language, and time filters
- **👨‍👩‍👧‍👦 Culturally-aware filters** (e.g., audience, gender, prayer-relative timing) for instances that need them
- **🌐 Multi-language support** for global communities
- **📱 Mobile-friendly experience** across devices

### For Event Organizers

- **📅 Multi-session events** for conferences, seminars, and recurring programs
- **✅ Verification system** to build trust and credibility
- **📊 Analytics and engagement tracking** for organizers
- **Flexible publishing policies** (open, approval-based, or invite-only)
- **Event Extensibility** 

### For Platform Owners

- **🐳 Docker-Ready** One-command deployment with docker-compose
- **💼 Single or Multi-Tenancy Support**
- **🛠️ Full white-label control** (branding, domain, instance identity)
- **Configurable policies** Choose who can publish, verification criteria, categories, and more
- **Federation-ready architecture** with ATProto / ActivityPub planned
- **📚 Comprehensive Docs** for deployment, customization, and operation

##  Deployment & Hosting Options

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

* **Identity & Access:** Managed via **Keycloak**, supporting MFA, Social Login, and SAML/LDAP integration.
* **Authorization:** Fine-grained access control (FGAC) powered by **Cerbos**: Policy Decision Point that coordinates unified policy-based authorization, allowing for complex "Who can do what" logic with human readable yaml for non technical users.
* **Data Integrity:** All database interactions use **Parameterized EF Core queries** to eliminate SQL injection.
* **Secret Management:** Zero hardcoded credentials; all secrets are injected at runtime via **Infisical**.
* **Observability:** Integrated **OpenTelemetry** for distributed tracing and real-time security monitoring.

### Security Features

- **🔒 HTTPS Enforcement:** All traffic encrypted with HSTS
- **🔐 Modern Authentication:** OAuth 2.0/OIDC via Keycloak
- **🗝️ Secret Management:** Infisical vault for credentials
- **🛡️ Authorization:** Policy-based access control via Cerbos
- **🔍 Input Validation:** FluentValidation + ASP.NET Core model binding
- **🚫 SQL Injection Prevention:** Parameterized queries (EF Core)
- **🌐 CORS:** Configurable origin whitelist
- **⏱️ Rate Limiting:** ASP.NET Core middleware

### Extensibility Model

Events use **composition over inheritance**:

```
Event (Core)
  ├── IslamicDetails (optional aspect)
  ├── TechDetails (optional aspect)
  └── EducationalDetails (optional aspect)
```

A "Ramadan Tech Workshop" has both Islamic and Tech aspects. No class explosion.

See [EXTENSIBILITY.md](docs/EXTENSIBILITY.md) and [MODULAR_EVENTS.md](docs/MODULAR_EVENTS.md).

## 🚀 Quick Start

### 🌐 Use Our Hosted Instance

Visit **[event.openislamu.org](https://event.openislamu.org)** to: 
- Browse events (no account needed)
- Create account to post events
- Register for events
- Follow organizations

### 🖥️ Self-Host Your Instance (Advanced)

```bash
# Clone the repository
git clone [https://github.com/islamu-ngo/Explore.git](https://github.com/islamu-ngo/Explore.git) && cd Explore

# Start without Object Storage (API, DB, Auth, UI...) And provide the secrets inside the Admin UI for Object Storage! 
docker-compose up -d

# Start the full stack (API, DB, Auth, UI, Object Storage...)
docker-compose up --profile storage -d


```

See [Operations Guide][operations-doc] for full deployment instructions.

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

### How to Contribute

1. Fork the repoisory
2. Create your feature branch (`git checkout -b feature/foobar`)
3. Commit your changes (`git commit -am 'Add some foobar'`)
4. Push to the branch (`git push origin feature/foobar`)
5. Create a new Pull Request

Please read [Contribution Guidelines][contribution-guidelines] for details on the process for submitting pull requests to us.

## 📚 Documentation

Start here for deeper details and technical guides:

### 📚 Core Docs

- **Project Overview**: [Master Reference][master-reference-doc] & [Project Context][project-doc]
- [Architecture][architecture-doc]
- [Governance][governance-doc]
- [Quick Reference][quick-reference-doc]

### 📚 Platform Docs

- [Operations Guide][operations-doc]
- [Multi-Tenancy][multi-tenancy-doc]
- [Admin Hierarchy][admin-hierarchy-doc]
- [Deployment Modes][deployment-modes-doc]
- [Extensibility][extensibility-doc]
- [Modular Events][modular-events-doc]
- [Rendering Policies][render-policies-doc]

### 📚 Reference Docs

- [API Reference][api-doc]
- [Domain Model][domain-doc]
- [Security][security-doc]
- [Configuration & Environment][configuration-doc]
- [Troubleshooting][troubleshooting-doc]

## 🏗️ Technical Overview

ISLAMU Event is built on **Clean Architecture + CQRS** with a **BFF (Backend-for-Frontend)** pattern.

### Stack

[- **Backend**: .NET 10, ASP.NET Core, MediatR, EF Core
- **Frontend**: Blazor Server + WASM, MudBlazor
- **Database**: PostgreSQL + PostGIS
- **AuthN**: Keycloak (OIDC/OAuth2)(Core: .NET 10 (LTS) & ASP.NET Core
- **AuthZ**: Cerbos (Policy Decision Point)
- **Secrets**: Infisical Vault
- **Observability**: Serilog, OpenTelemetry

### The Request Lifecycle (CQRS)
We utilize **MediatR** for a decoupled command/query pipeline. This ensures that our business logic is isolated from transport layers (API/Blazor).

```mermaid
graph TD
    subgraph Client_Layer [Presentation]
        A[Blazor WASM / Mobile] -->|HTTPS/REST| B[ASP.NET Core API]
    end

    subgraph Application_Layer [Business Logic]
        B -->|Command/Query| C[MediatR Pipeline]
        C -->|Validation| D[FluentValidation]
        D -->|Execution| E[Domain Handler]
    end

    subgraph Infrastructure_Layer [Data & Services]
        E -->|EF Core| F[(PostgreSQL + PostGIS)]
        E -->|OIDC| G[Keycloak]
        E -->|Events| H[Svix Webhooks]
    end

    E -.->|Future| I[ATProto / ActivityPub Gateway]
```

```mermaid
graph LR
    A[Request] --> B[Controller]
    B --> C[MediatR]
    C --> D[Handler]
    D --> E[Repository]
    E --> F[(PostgreSQL)]
    D --> G[Response]
```

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
[roadmap-link]: https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988
[roadmap-image]: images/Roadmap%20Kanban%20View.png
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

[github-repo-link]: https://github.com/islamu-ngo/Explore
[github-issues-link]: https://github.com/islamu-ngo/Explore/issues/new
[github-discussions-link]: https://github.com/islamu-ngo/Explore/discussions
[github-contributors-link]: https://github.com/islamu-ngo/Explore/graphs/contributors

[sonarcloud-shield]: https://sonarcloud.io/api/project_badges/measure?project=islamu-ngo_Explore&metric=alert_status
[sonarcloud-link]: https://sonarcloud.io/summary/overall?id=islamu-ngo_Explore
[github-workflow-status-shield]: https://img.shields.io/github/actions/workflow/status/islamu-ngo/Explore/build.yml?branch=main&logo=github&style=flat-square
[codecov-shield]: https://img.shields.io/codecov/c/github/islamu-ngo/Explore
[codecov-link]: https://app.codecov.io/github/islamu-ngo/Explore
[github-stars-shield]: https://img.shields.io/github/stars/islamu-ngo/Explore?color=594ae2&style=flat-square&logo=github
[github-stars-link]: https://github.com/islamu-ngo/Explore/stargazers
[github-license-shield]: https://img.shields.io/github/license/islamu-ngo/Explore?color=594ae2&logo=github&style=flat-square
[github-license-link]: https://github.com/islamu-ngo/Explore/blob/main/LICENSE
[github-last-commit-shield]: https://img.shields.io/github/last-commit/islamu-ngo/Explore?color=594ae2&style=flat-square&logo=github
[github-last-commit-link]: https://github.com/islamu-ngo/Explore
[github-contributors-shield]: https://img.shields.io/github/contributors/islamu-ngo/Explore?color=594ae2&style=flat-square&logo=github
[github-discussions-shield]: https://img.shields.io/github/discussions/islamu-ngo/Explore?color=594ae2&logo=github&style=flat-square
[discord-shield]: https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square
[discord-link]: https://discord.gg/wrkY824Yv5

[repobeats-image]: https://repobeats.axiom.co/api/embed/a0f11a3d9b80342b5f5965127c2c45871c9d3397.svg
[contributors-image]: https://contrib.rocks/image?repo=islamu-ngo/explore
[contributors-link]: https://github.com/islamu-ngo/explore/graphs/contributors

[keycloak-link]: https://www.keycloak.org/
[cerbos-link]: https://www.cerbos.dev/
[svix-link]: https://www.svix.com/
[infisical-link]: https://infisical.com/
[mudblazor-link]: https://www.mudblazor.com/
[penpot-link]: https://penpot.app/
[plane-link]: https://plane.so/
[coolify-link]: https://coolify.io/
[kener-link]: https://kener.ing/

[palestinian-red-crescent]: https://www.palestinercs.org/en/Donation
[support-palestine-banner]: https://github.com/Safouene1/support-palestine-banner/blob/master/banner-support.svg
[support-palestine-banner-source]: https://github.com/Safouene1/support-palestine-banner/

[contribution-guidelines]: https://sites.plane.so/pages/b957e6c5278845feac5557d22bd54756
