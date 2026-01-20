# ISLAMU Event

<div align="center">

**Open-Source Federated Event Discovery Platform**

[![Build Status](https://img.shields.io/github/actions/workflow/status/islamu-ngo/Explore/build.yml?branch=main&logo=github&style=flat-square)](https://github.com/islamu-ngo/Explore/actions)
[![Code Coverage](https://img.shields.io/codecov/c/github/islamu-ngo/Explore?style=flat-square)](https://app.codecov.io/github/islamu-ngo/Explore)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=islamu-ngo_Explore&metric=alert_status)](https://sonarcloud.io/summary/overall?id=islamu-ngo_Explore)
[![License: AGPL v3](https://img.shields.io/github/license/islamu-ngo/Explore?color=594ae2&style=flat-square)](LICENSE)
[![Discord](https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square)](https://discord.gg/wrkY824Yv5)

[Features](#-core-features) • [Architecture](#-technical-architecture) • [Quick Start](#-quick-start) • [Documentation](#-documentation) • [Contributing](#-contributing)

</div>

---

## Overview

**ISLAMU Event** is a production-grade event discovery and management platform built for Muslim communities worldwide. Combining enterprise architecture with community-driven values, it enables organizations to promote events and individuals to discover culturally-appropriate Islamic programming.

### Key Differentiators

- **🏛️ Enterprise Architecture:** Built on Clean Architecture and SOLID principles using .NET 10
- **🌐 Federation-Ready:** ATProto-first design with ActivityPub gateway (planned)
- **🔐 Privacy-First:** Self-hostable with complete data ownership
- **🎯 Cultural Intelligence:** Advanced filtering by madhab, gender, age, and prayer times
- **🛡️ Verified Organizations:** Two-tier verification system for trust and quality
- **📖 Open Source:** AGPL-3.0 licensed for transparency and community ownership

---

## 🎯 Core Features

### For Event Seekers

**Discover events that match your needs with precision:**

| Feature | Description |
|---------|-------------|
| **🗺️ Geospatial Search** | Find events within a specific radius using PostGIS |
| **👨‍👩‍👧‍👦 Audience Filtering** | Filter by age group (children, youth, adults, seniors) and gender |
| **🕋 Madhab-Specific** | Filter events by Islamic jurisprudence school |
| **📚 Topic Categories** | Browse by Aqidah, Fiqh, Tafsir, Hadith, History, and more |
| **⏰ Prayer-Relative Scheduling** | Events aligned with local prayer times |
| **🌐 Multi-Language** | Support for Arabic, English, French, and more |
| **📱 Responsive Design** | Optimized for mobile, tablet, and desktop |

### For Event Organizers

**Manage and promote events with professional tools:**

| Feature | Description |
|---------|-------------|
| **✅ Verification System** | Build trust with fact-checked organization verification |
| **📅 Multi-Session Events** | Manage conferences, weekly classes, and recurring programs |
| **🎯 Advanced Targeting** | Reach specific audiences with granular filters |
| **📊 Analytics** | Track views, registrations, and engagement metrics |
| **🌍 Federation** | Distribute events across decentralized platforms |
| **💼 Multi-Tenant** | Manage multiple organizations under one account |

### For Self-Hosters

**Own your infrastructure and data:**

| Feature | Description |
|---------|-------------|
| **🐳 Docker-Ready** | One-command deployment with docker-compose |
| **🔓 Open Source** | Full source code access (AGPL-3.0) |
| **🔐 Security-First** | Industry-standard authentication and authorization |
| **📈 Scalable** | Horizontal scaling with load balancing |
| **🛠️ Extensible** | Plugin architecture for custom features |
| **📚 Comprehensive Docs** | Detailed deployment and operations guides |

---

## 🏗️ Technical Architecture

### Clean Architecture Layers

ISLAMU Event follows **Clean Architecture** principles with strict dependency rules:

```
┌─────────────────────────────────────────────────────────┐
│  Presentation (API, Blazor)                             │
│  • ASP.NET Core Web API + Blazor Server/WASM           │
│  • Controllers delegate to MediatR                      │
├─────────────────────────────────────────────────────────┤
│  Infrastructure (Persistence, External Services)        │
│  • EF Core 10 + PostgreSQL + PostGIS                   │
│  • Repository pattern implementation                    │
│  • External integrations (Auth, Storage, Email)         │
├─────────────────────────────────────────────────────────┤
│  Application (Business Logic, CQRS)                     │
│  • MediatR commands/queries/handlers                    │
│  • DTOs + FluentValidation                             │
│  • AutoMapper profiles                                  │
├─────────────────────────────────────────────────────────┤
│  Domain (Entities, Value Objects)                       │
│  • Core business entities                               │
│  • Domain events and rules                              │
│  • Zero external dependencies                           │
└─────────────────────────────────────────────────────────┘
```

**Architectural Patterns:**
- **CQRS with MediatR:** Separate read and write operations for scalability
- **Repository Pattern:** Data access abstraction with generic and specific repositories
- **Unit of Work:** Transaction management via EF Core DbContext
- **Dependency Injection:** ASP.NET Core DI container
- **AutoMapper:** Entity-to-DTO transformation

### Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Backend** | .NET 10, ASP.NET Core, C# 13, MediatR, FluentValidation, AutoMapper |
| **Database** | PostgreSQL 17, PostGIS, Entity Framework Core 10 |
| **Frontend** | Blazor (Server + WASM), MudBlazor, BFF Pattern |
| **Authentication** | Keycloak (OIDC/OAuth 2.0), JWT tokens |
| **Authorization** | Cerbos (Policy Decision Point), ABAC + RBAC |
| **Secrets** | Infisical (encrypted vault) |
| **Storage** | MinIO (S3-compatible object storage) |
| **Email** | SendGrid/SMTP |
| **Monitoring** | Sentry (error tracking), Kener (status page) |
| **DevOps** | Docker, Docker Compose, GitHub Actions, Coolify |
| **API Docs** | Scalar, Swagger/OpenAPI 3.0 |

### Federation Architecture

**ATProto-First with ActivityPub Gateway:**

```
┌─────────────────────────────────────────────────────────┐
│  Users (Decentralized Identities - DIDs)               │
└──────────────┬──────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────┐
│  ATProto Network (PDS, Relay, AppView)                 │
│  • Personal Data Servers (PDS)                         │
│  • Firehose relay (real-time events)                   │
│  • ISLAMU Event AppView (indexing layer)              │
└──────────────┬──────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────┐
│  ActivityPub Gateway (Planned)                          │
│  • Expose events as ActivityPub objects                │
│  • Translate Follow, RSVP, Like actions                │
│  • WebFinger, Actor endpoints, Inbox/Outbox            │
└──────────────┬──────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────┐
│  Fediverse (Mastodon, Mobilizon, Pleroma, etc.)        │
└─────────────────────────────────────────────────────────┘
```

**Current State:** ATProto indexing layer (reads from firehose)
**Planned:** Full bidirectional federation with ActivityPub

---

## 🚀 Quick Start

### Using Our Hosted Instance

Visit **[explore.openislamu.org](https://explore.openislamu.org)** to:
- Browse events (no account required)
- Create an account to post events
- Register for events and follow organizations

### Self-Hosting with Docker

**Prerequisites:**
- Docker 20.10+
- Docker Compose 2.0+
- 2GB RAM minimum (4GB recommended)

**Deploy in 3 steps:**

```bash
# 1. Clone and configure
git clone https://github.com/islamu-ngo/Explore.git
cd Explore
cp .env.example .env
# Edit .env with your configuration

# 2. Start all services
docker-compose up -d

# 3. Access at http://localhost:7001
```

**What's included:**
- Web API (ASP.NET Core)
- Blazor frontend (Server + WASM)
- PostgreSQL database with PostGIS
- Keycloak (authentication)
- MinIO (file storage)
- Email service (SMTP)

See [OPERATIONS.md](docs/OPERATIONS.md) for production deployment.

### Local Development

**Prerequisites:**
- .NET 10 SDK
- PostgreSQL 17 with PostGIS
- Node.js 20+ (for frontend tooling)

```bash
# 1. Clone and restore
git clone https://github.com/islamu-ngo/Explore.git
cd Explore
dotnet restore

# 2. Configure database
# Edit connection string in Explore.API/appsettings.Development.json

# 3. Run migrations
dotnet ef database update --project Explore.Persistence

# 4. Run with Aspire (recommended)
dotnet run --project Explore.AppHost

# Or run projects individually:
# dotnet run --project Explore.API
# dotnet run --project Explore.Blazor
```

**Development URLs:**
- **Aspire Dashboard:** https://localhost:17225
- **API:** https://localhost:7001
- **Blazor:** https://localhost:7002
- **Scalar Docs:** https://localhost:7001/scalar/v1

---

## 📚 Documentation

### User Documentation

| Document | Description |
|----------|-------------|
| [User Guide](docs/USER_GUIDE.md) | How to use the platform (browsing, posting events) |
| [Organizer Guide](docs/ORGANIZER_GUIDE.md) | Managing organizations and events |
| [Self-Hosting Guide](docs/OPERATIONS.md) | Deployment and infrastructure |

### Developer Documentation

| Document | Description |
|----------|-------------|
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture and design patterns |
| [API.md](docs/API.md) | REST API conventions and endpoints |
| [GOVERNANCE.md](docs/GOVERNANCE.md) | Coding standards and conventions |
| [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) | Critical rules for contributors |
| [CONTRIBUTING.md](docs/CONTRIBUTING.md) | How to contribute code |
| [BLAZOR.md](docs/BLAZOR.md) | Frontend architecture (Blazor) |
| [SECURITY.md](docs/SECURITY.md) | Authentication and authorization |
| [FEDERATION.md](docs/FEDERATION.md) | ATProto and ActivityPub integration |
| [DATABASE.md](schema/islamu-event.md) | Database schema and migrations |

### AI Agent Documentation

| Document | Description |
|----------|-------------|
| [CLAUDE.md](CLAUDE.md) | AI agent entrypoint and instructions |
| [TEMPLATE_GLOSSARY.md](docs/TEMPLATE_GLOSSARY.md) | Placeholder substitution guide |

---

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Explore.Application.UnitTests
```

### Test Coverage

| Layer | Target | Current |
|-------|--------|---------|
| Domain | 90%+ | 🚧 In Progress |
| Application | 80%+ | 🚧 In Progress |
| Infrastructure | 70%+ | ⏳ Planned |
| API | 75%+ | ⏳ Planned |

**Testing Strategy:**
- **Unit Tests:** Application layer (handlers, validators, mappers)
- **Integration Tests:** API endpoints with WebApplicationFactory
- **Repository Tests:** In-memory database (EF Core)
- **E2E Tests:** Playwright (planned)

---

## 🔐 Security

### Security Features

- **🔒 HTTPS Enforcement:** All traffic encrypted with HSTS
- **🔐 Modern Authentication:** OAuth 2.0/OIDC via Keycloak
- **🗝️ Secret Management:** Infisical vault for credentials
- **🛡️ Authorization:** Policy-based access control via Cerbos
- **🔍 Input Validation:** FluentValidation + ASP.NET Core model binding
- **🚫 SQL Injection Prevention:** Parameterized queries (EF Core)
- **🌐 CORS:** Configurable origin whitelist
- **⏱️ Rate Limiting:** ASP.NET Core middleware

### Security Best Practices

**Authentication:**
- JWT tokens with refresh rotation
- Multi-factor authentication (MFA) support
- Session timeout enforcement

**Authorization:**
- Attribute-based access control (ABAC)
- Role-based access control (RBAC)
- Resource-level permissions

**Data Protection:**
- Encryption at rest (database)
- Encryption in transit (TLS 1.3)
- Personally Identifiable Information (PII) handling

### Vulnerability Reporting

Found a security issue? **DO NOT** open a public issue.

**Email:** contact@openislamu.org
**Response Time:** Within 48 hours
**Disclosure Policy:** See [SECURITY-POLICY.md](SECURITY-POLICY.md)

---

## 🤝 Contributing

We welcome contributions from developers, designers, translators, and community members!

### Ways to Contribute

**Non-Technical:**
- 🐛 Report bugs
- 💡 Suggest features
- 📖 Improve documentation
- 🌐 Translate UI/docs
- 📣 Spread the word

**Technical:**
- 💻 Fix bugs
- ✨ Implement features
- 🧪 Write tests
- 📊 Improve performance
- 🎨 Enhance UI/UX

### Contribution Workflow

1. **Read the guides:**
   - [CONTRIBUTING.md](docs/CONTRIBUTING.md) — Process and standards
   - [GOVERNANCE.md](docs/GOVERNANCE.md) — Coding conventions
   - [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) — Critical rules

2. **Find a task:**
   - Browse [GitHub Issues](https://github.com/islamu-ngo/Explore/issues)
   - Check [Roadmap Kanban](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988)
   - Look for `good first issue` labels

3. **Submit a pull request:**
   ```bash
   git checkout -b feature/my-feature
   git commit -am 'Add feature'
   git push origin feature/my-feature
   # Open PR on GitHub
   ```

**Code Review:**
- PRs reviewed within 48 hours
- CI/CD checks must pass
- Code coverage must not decrease

See [Contribution Guidelines](https://sites.plane.so/pages/b957e6c5278845feac5557d22bd54756) for details.

---

## 📊 Roadmap

View the [Public Roadmap](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988) to see planned features, vote on priorities, and track progress.

### 🚀 Current Milestones

**Q1 2026:**
- ✅ Core platform with event CRUD
- ✅ Multi-tenant architecture
- 🚧 ATProto indexing layer
- 🚧 Advanced filtering (madhab, language, prayer times)

**Q2 2026:**
- 📅 Real-time notifications (SignalR)
- 📅 Mobile apps (iOS + Android)
- 📅 ActivityPub gateway (federation)
- 📅 Analytics dashboard

**Q3 2026:**
- 📅 Video streaming integration
- 📅 Ticketing system
- 📅 AI-powered recommendations
- 📅 Marketplace (sponsorships)

**Q4 2026:**
- 📅 Full ATProto DID integration
- 📅 Reputation system
- 📅 Advanced search (ElasticSearch)
- 📅 Multi-language support (i18n)

---

## 👥 Community

### Join the Conversation

[![Discord](https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=for-the-badge)](https://discord.gg/wrkY824Yv5)
[![GitHub Discussions](https://img.shields.io/github/discussions/islamu-ngo/Explore?color=594ae2&logo=github&style=for-the-badge)](https://github.com/islamu-ngo/Explore/discussions)

**Discord:** Real-time chat, support, and collaboration
**GitHub Discussions:** Long-form discussions, polls, and knowledge sharing

### Code of Conduct

We follow a [Code of Conduct](CODE_OF_CONDUCT.md) in all community spaces. We're committed to creating a welcoming, inclusive, and respectful environment for everyone.

### Contributors

<a href="https://github.com/islamu-ngo/explore/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=islamu-ngo/explore" />
</a>

**Thank you to all our contributors!** Every contribution, big or small, makes a difference.

---

## 📈 Project Metrics

![Repository Stats](https://repobeats.axiom.co/api/embed/a0f11a3d9b80342b5f5965127c2c45871c9d3397.svg "Repobeats analytics")

[![Build Status](https://img.shields.io/github/actions/workflow/status/islamu-ngo/Explore/build.yml?branch=main&style=flat-square)](https://github.com/islamu-ngo/Explore/actions)
[![Code Coverage](https://img.shields.io/codecov/c/github/islamu-ngo/Explore?style=flat-square)](https://app.codecov.io/github/islamu-ngo/Explore)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=islamu-ngo_Explore&metric=alert_status)](https://sonarcloud.io/summary/overall?id=islamu-ngo_Explore)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=islamu-ngo_Explore&metric=sqale_index)](https://sonarcloud.io/summary/overall?id=islamu-ngo_Explore)

---

## 🙏 Acknowledgements

This project is built on incredible open-source tools:

| Tool | Purpose | License |
|------|---------|---------|
| [Keycloak](https://www.keycloak.org/) | Identity & Access Management | Apache 2.0 |
| [Cerbos](https://www.cerbos.dev/) | Policy Decision Point | Apache 2.0 |
| [Svix](https://www.svix.com/) | Webhook delivery | MIT |
| [Infisical](https://infisical.com/) | Secrets management | MIT |
| [MudBlazor](https://www.mudblazor.com/) | Blazor UI library | MIT |
| [Penpot](https://penpot.app/) | Design tool | MPL 2.0 |
| [Plane](https://plane.so/) | Project management | AGPL-3.0 |
| [Coolify](https://coolify.io/) | Deployment platform | Apache 2.0 |
| [Kener](https://kener.ing/) | Status page | MIT |

---

## 📞 Contact

- **🐛 Bug Reports:** [GitHub Issues](https://github.com/islamu-ngo/Explore/issues)
- **💬 Community Chat:** [Discord Server](https://discord.gg/wrkY824Yv5)
- **📧 Email:** contact@openislamu.org
- **🌐 Website:** [openislamu.org](https://openislamu.org)

---

## 📄 License

**ISLAMU Event** is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

**Key Terms:**
- ✅ Free to use, modify, and distribute
- ✅ Source code must remain open (copyleft)
- ⚠️ Network use requires source code disclosure
- ⚠️ Derivatives must use AGPL-3.0

See [LICENSE](LICENSE) for full legal text.

---

## 🇵🇸 Support Palestine

The ongoing humanitarian crisis in Palestine demands our attention and support.

[![Support Palestine](https://github.com/Safouene1/support-palestine-banner/blob/master/banner-support.svg)](https://www.palestinercs.org/en/Donation)

**[Donate to the Palestinian Red Crescent Society](https://www.palestinercs.org/en/Donation)**

---

<div align="center">

**⭐️ Star this repository if you find it useful!**

**Built with ❤️ by the ISLAMU community**

[🏠 Home](https://openislamu.org) • [📚 Docs](docs/) • [💬 Discord](https://discord.gg/wrkY824Yv5) • [🗺️ Roadmap](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988)

</div>
