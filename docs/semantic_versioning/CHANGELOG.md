# Changelog — ISLAMU Event Platform

> All notable changes to this project are documented in semantic version files.
> This project follows [Semantic Versioning 2.0.0](https://semver.org/).
>
> **Pre-1.0 Convention**: Major version zero (0.y.z) indicates initial development. The public API may change at any time and should not be considered stable. This release is intended for beta testing, early adopters, and feedback gathering.

**Last Updated:** 2026-02-13

---

## Version History

| Version | Title | Released | Status |
|---------|-------|----------|--------|
| [v0.1.0](v0.1.0.md) | First Public Release (Beta) | TBD | 🚀 **CURRENT** |

---

## Versioning Policy

### Current Phase: Pre-1.0 Beta (0.1.0)

**What does 0.1.0 mean?**
- **First publicly-usable release** ready for beta testing and early adopter feedback
- **Major version zero (0.y.z)** signals: "Initial development — anything may change"
- **API not yet stable**: Breaking changes may occur in any release before 1.0.0
- **Production use at your own risk**: While feature-complete, the public API is not guaranteed stable

**Recommended version**: 0.1.0 (not 0.0.1) per [Semantic Versioning best practices](https://semver.org/)

### Progression to 1.0.0

The project will reach **v1.0.0** when:
1. ✅ The public REST API surface is stable and documented *(complete)*
2. ✅ Core event management features are production-ready *(complete)*
3. ✅ Multi-tenancy and organization workflows are validated *(complete)*
4. ⏳ Federation endpoints (ATProto/ActivityPub) are operational *(Phase 2 planned)*
5. ⏳ Production deployment validated with real tenants *(in progress)*
6. ⏳ Test coverage meets minimum thresholds across all 7 test projects *(in progress)*
7. ⏳ Public beta feedback incorporated *(awaiting beta users)*

**Timeline**: v1.0.0 targeted for Q2 2026 after federation implementation and public beta validation.

---

## Categories Used

| Emoji | Category | Description |
|-------|----------|-------------|
| 🚀 | Features | New functionality |
| 🔒 | Security | Authentication, authorization, vulnerability fixes |
| 🔄 | Refactor | Code improvements without behavior change |
| 🐛 | Fixes | Bug fixes |
| 🎨 | Styling | UI/UX and CSS changes |
| 🧪 | Testing | Test additions or improvements |
| 📦 | Dependencies | Dependency additions or updates |
| 🔧 | Chore | Tooling, CI, configuration |

---

## Technology Stack (v0.1.0)

| Component | Technology | Version |
|-----------|------------|---------|
| Runtime | .NET | 10.0 |
| Orchestration | .NET Aspire | Latest |
| Frontend | Blazor | Server + WebAssembly (InteractiveAuto) |
| UI Components | MudBlazor | Latest |
| Database | PostgreSQL + PostGIS | 16+ |
| ORM | Entity Framework Core | 10 |
| Authentication | Keycloak | OIDC/OAuth 2.0 |
| Authorization | Cerbos | PDP |
| Secrets | Infisical | Latest |
| API Docs | Scalar + Swagger/NSwag | Latest |
| Logging | Serilog | Latest |
| Telemetry | OpenTelemetry | Latest |
| Federation | ATProto / ActivityPub | Phase 1 (data models only) |

---

## Feature Checklist

### ✅ Implemented in v0.1.0
- [x] Event Management (CRUD, sessions, registration, advanced filtering)
- [x] Organization Management (CRUD, members, roles, reviews)
- [x] User Management & Authentication (Keycloak OIDC, BFF pattern)
- [x] Multi-Tenancy (single-tenant and SaaS deployment modes)
- [x] Fine-Grained Authorization (Cerbos PDP, resource-level policies)
- [x] Secrets Management (Infisical with environment variable fallback)
- [x] Object Storage (S3-compatible with presigned URLs)
- [x] Email Service (SMTP with resilience pipelines)
- [x] Event Aspects (Islamic module, Tech module, modular extensibility)
- [x] HATEOAS REST API (Level 3, HAL+JSON, 18 link policies)
- [x] Blazor Frontend MVP (44 pages, MudBlazor components)
- [x] API Documentation (Scalar + Swagger with OpenAPI 3.0)
- [x] Soft Delete & Audit Trail (full audit with who/when deleted)
- [x] Query Specifications (33 parameters, fluent builder, module-conditional)
- [x] Multi-Level Caching (Output + HybridCache)
- [x] Observability (Serilog + OpenTelemetry + PLG stack)
- [x] CI/CD Pipeline (GitHub Actions + Docker + ATCR)
- [x] Test Coverage (7 test projects, TUnit, bUnit, architecture tests)

### ⏳ Planned for 1.0.0
- [ ] Federation Endpoints (WebFinger, Actor endpoints, Inbox/Outbox)
- [ ] Federation Protocol Logic (ATProto PDS, AppView indexing)
- [ ] ActivityPub Bridge (ATProto ↔ ActivityPub gateway)
- [ ] DID Resolution (PLC/DNS-based)
- [ ] HTTP Signatures (federation authentication)
- [ ] Moderation System (content review workflows)
- [ ] Mobile Optimization (responsive design improvements)
- [ ] Real-Time Notifications (SignalR, push notifications)
- [ ] Rate Limiting (sliding window per IP/user)
- [ ] Security Headers (CSP, X-Frame-Options, etc.)

---

## Semantic Versioning References

### Official Resources
- [Semantic Versioning 2.0.0](https://semver.org/) — Official semver specification
- [Best Practices for 0.y.z Versioning](https://talent500.com/blog/semantic-versioning-explained-guide/) — Starting with 0.1.0
- [Software Versioning on Wikipedia](https://en.wikipedia.org/wiki/Software_versioning) — Historical context

### Key Takeaways
1. **Start with 0.1.0** (not 0.0.1) for first public release
2. **Major version zero** (0.y.z) signals initial development
3. **API instability expected** until 1.0.0
4. **Minor version bumps** (0.y.0) indicate new features
5. **Patch version bumps** (0.y.z) indicate bug fixes
6. **Pre-release labels** (0.1.0-beta.1) for testing builds

---

**Ready for beta testing and early adopter feedback. API may change before 1.0.0 stable release.**
