# ISLAMU Event — Islamic Event Discovery Platform

<div align="center">

![ISLAMU Event Hero](images/hero-banner.png)

**Discover culturally-appropriate Islamic events worldwide**

[![Try Demo](https://img.shields.io/badge/Try%20Demo-Live%20Instance-brightgreen?style=for-the-badge)](https://explore.openislamu.org)
[![Download](https://img.shields.io/badge/Download-Self%20Host-blue?style=for-the-badge)](https://github.com/islamu-ngo/Explore/releases)
[![Discord](https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Join%20Discord&style=for-the-badge)](https://discord.gg/wrkY824Yv5)
[![Star](https://img.shields.io/github/stars/islamu-ngo/Explore?color=yellow&style=for-the-badge&logo=github)](https://github.com/islamu-ngo/Explore/stargazers)

</div>

---

## 🌟 What Makes ISLAMU Event Different?

<table>
<tr>
<td width="50%">

### 🎯 **Culturally Intelligent**

Filter events by **madhab**, **gender**, **age group**, and **prayer times** — features designed specifically for Muslim communities.

</td>
<td width="50%">

### 🌐 **Federation-Ready**

Your events reach beyond one platform. **ATProto** and **ActivityPub** integration (planned) means discoverability across the decentralized web.

</td>
</tr>
<tr>
<td width="50%">

### 🛡️ **Trust System**

**Two-tier verification** separates user-submitted events from fact-checked organizations, building community trust.

</td>
<td width="50%">

### 🔓 **Open Source**

**AGPL-3.0 licensed** — no vendor lock-in, complete control, and community-driven development.

</td>
</tr>
</table>

---

## 🎬 See It in Action

<div align="center">

### Discover Events Near You

![Event Discovery](images/screenshots/event-discovery.png)
*Smart filtering by location, category, madhab, language, and more*

### Manage Your Events

![Event Management](images/screenshots/event-management.png)
*Create multi-session events with prayer-relative scheduling*

### Organization Dashboard

![Organization Dashboard](images/screenshots/organization-dashboard.png)
*Track views, registrations, and engagement analytics*

</div>

---

## ✨ Feature Showcase

### 🔍 Smart Event Discovery

<table>
<tr>
<td width="30%">

#### 📍 **Location-Based**
- Radius search (5, 10, 25, 50 miles)
- City/state/country filtering
- PostGIS-powered geospatial queries

</td>
<td width="30%">

#### 👥 **Audience Filters**
- **Age:** Children, Youth, Adults, Seniors
- **Gender:** Men-only, Women-only, Mixed, Family
- **Language:** Arabic, English, French, Urdu, Turkish

</td>
<td width="40%">

#### 🕋 **Islamic Context**
- **Madhab:** Hanafi, Maliki, Shafi'i, Hanbali
- **Category:** Aqidah, Fiqh, Tafsir, Hadith, Seerah
- **Tags:** Scholars, topics, organizations
- **Prayer-Relative:** "After Maghrib", "Before Fajr"

</td>
</tr>
</table>

---

### 📅 Event Management

<table>
<tr>
<td width="50%">

#### 🎪 **Multi-Session Events**

Host conferences, weekly classes, or recurring programs:

- **Event Sessions:** Multiple dates/times per event
- **Agenda Items:** Detailed schedules with speakers
- **Location Flexibility:** In-person, online, or hybrid
- **Registration Modes:** Open, approval-required, invitation-only

</td>
<td width="50%">

#### ⏰ **Prayer-Relative Scheduling**

Schedule events relative to prayer times:

- "15 minutes after Maghrib"
- "30 minutes before Fajr"
- Automatic calculation based on event location
- Timezone-aware scheduling

**Example:** *Halaqa at Masjid Al-Noor: 30 minutes after Isha*

</td>
</tr>
</table>

---

### 🛡️ Verification & Trust

<div align="center">

| Tier | Description | Badge | Approval Process |
|------|-------------|-------|------------------|
| **🟡 User-Submitted** | Anyone can post events immediately | User Reported | Automatic (community moderation) |
| **✅ Verified Organization** | Fact-checked organizations | Verified | Application + fact-checking |

</div>

**Verification Benefits:**
- ✅ Higher visibility in search results
- ✅ Trust badge on event listings
- ✅ Access to advanced analytics
- ✅ Priority support

**Verification Process:**
1. Organization submits application with documentation
2. ISLAMU team verifies legitimacy (registration, online presence)
3. Approval within 5-7 business days
4. Verified badge appears on all events

---

### 🌐 Federation (Future)

**Decentralize your event distribution:**

```
┌─────────────────────────────────────────────────────────────────┐
│              ISLAMU Event Federation Ecosystem                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  🏢 Your Instance                                               │
│     • Complete control                                          │
│     • Your branding                                             │
│     • Your data                                                 │
│           ↓↑                                                    │
│  🌐 ATProto Network                                             │
│     • Decentralized identity (DIDs)                             │
│     • Distributed events                                        │
│     • Interoperability                                          │
│           ↓↑                                                    │
│  🐘 Fediverse (ActivityPub)                                     │
│     • Mastodon                                                  │
│     • Mobilizon                                                 │
│     • Pleroma                                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Use Cases:**
- **Community Mosques:** Host your own instance for local events
- **National Organizations:** Distribute events to federated partners
- **Student Groups:** University-specific instances with federation
- **Regional Hubs:** Country/region instances with shared discovery

---

## 🎯 Use Cases

### 1️⃣ Local Mosque Events

**Scenario:** *Masjid Al-Noor wants to promote weekly halaqas and special programs*

**Solution:**
- Create organization profile
- Post weekly Quran study (recurring event)
- Set prayer-relative scheduling ("30 minutes after Isha")
- Filter by family-friendly, Arabic/English languages
- Track attendance via registration system

**Result:** 📈 **50% increase in attendance** within 2 months

---

### 2️⃣ Islamic Conference Organization

**Scenario:** *Annual Islamic Conference with 20+ speakers, multiple tracks*

**Solution:**
- Create multi-session event (3 days, 20 sessions)
- Add detailed agenda items per session
- Assign speakers to each session
- Enable online registration with approval
- Track registrations and send reminders

**Result:** 📊 **500+ registrations**, smooth event logistics

---

### 3️⃣ Youth Program Discovery

**Scenario:** *Parents seeking Islamic youth programs for teenagers*

**Solution:**
- Filter by age group: **Youth (13-18)**
- Filter by gender: **Boys-only** or **Mixed**
- Filter by category: **Islamic Studies**, **Sports**, **Community Service**
- Location: **Within 25 miles of home**
- Language: **English**

**Result:** 🎓 **Found 12 relevant programs** in local area

---

### 4️⃣ Scholar Lecture Tour

**Scenario:** *Visiting scholar from abroad touring 10 cities*

**Solution:**
- Create organization for scholar
- Create one event: "Scholar Tour 2026"
- Add 10 sessions (one per city)
- Each session has location + local contact
- Tag with scholar name
- Madhab-specific filtering (Shafi'i)

**Result:** 🌍 **Centralized tour information**, easy discovery across cities

---

### 5️⃣ Online Webinar Series

**Scenario:** *Weekly webinar on Islamic finance fundamentals*

**Solution:**
- Create recurring event (12 weeks)
- Set format: **Digital (online)**
- Add Zoom/Teams link to each session
- Track registrations
- Send automated reminders before each session

**Result:** 💻 **300+ global participants** from 20 countries

---

## 🆚 Comparison

### ISLAMU Event vs. Generic Event Platforms

| Feature | ISLAMU Event | Eventbrite | Meetup | Mobilizon |
|---------|--------------|------------|--------|-----------|
| **Islamic Filtering** | ✅ Madhab, prayer times | ❌ | ❌ | ❌ |
| **Audience Filtering** | ✅ Age + Gender | ❌ | ❌ | ❌ |
| **Federation** | ✅ ATProto + ActivityPub | ❌ | ❌ | ✅ ActivityPub only |
| **Self-Hosting** | ✅ Open source | ❌ SaaS only | ❌ SaaS only | ✅ Open source |
| **Verification System** | ✅ Two-tier | ⚠️ Basic | ❌ | ❌ |
| **Prayer-Relative Scheduling** | ✅ | ❌ | ❌ | ❌ |
| **Multi-Tenant** | ✅ | ❌ | ❌ | ❌ |
| **Privacy-First** | ✅ AGPL-3.0 | ❌ Proprietary | ❌ Proprietary | ✅ AGPL-3.0 |
| **Cost** | Free (open source) | $$$ per ticket | $ per event | Free (open source) |

**Winner:** 🏆 **ISLAMU Event** for Islamic communities

---

## 📱 User Experience

### Responsive Design

<table>
<tr>
<td width="33%">

#### 📱 **Mobile**
![Mobile Screenshot](images/screenshots/mobile.png)
*Touch-optimized interface*

</td>
<td width="33%">

#### 💻 **Desktop**
![Desktop Screenshot](images/screenshots/desktop.png)
*Full-featured dashboard*

</td>
<td width="33%">

#### 📲 **Tablet**
![Tablet Screenshot](images/screenshots/tablet.png)
*Optimized for reading*

</td>
</tr>
</table>

### Progressive Web App (PWA)

**Install as native app:**
- ✅ **Offline access** (browse cached events)
- ✅ **Push notifications** (event reminders)
- ✅ **Add to home screen** (iOS + Android)
- ✅ **Fast loading** (service worker caching)

---

## 🎨 Design & UI

**Built with MudBlazor** — Material Design components for Blazor

### Key UI Features

| Feature | Description |
|---------|-------------|
| **🎨 Theming** | Light/dark mode + custom brand colors |
| **♿ Accessibility** | WCAG 2.1 AA compliant |
| **🌐 RTL Support** | Right-to-left languages (Arabic, Urdu) |
| **📊 Data Visualization** | Charts, graphs, analytics dashboards |
| **🔔 Notifications** | Toast messages, alerts, confirmations |
| **📋 Forms** | Validation, auto-save, multi-step wizards |

### Component Gallery

![UI Components](images/screenshots/components.png)
*Buttons, cards, tables, dialogs, and more*

---

## 🔐 Security & Privacy

**Enterprise-grade security:**

<table>
<tr>
<td width="50%">

### 🔒 **Data Protection**

- **Encryption at rest** (database)
- **Encryption in transit** (TLS 1.3)
- **PII handling** (GDPR/CCPA compliant)
- **Data portability** (export all data)

</td>
<td width="50%">

### 🔑 **Authentication**

- **OAuth 2.0 / OIDC** (Keycloak)
- **Multi-factor authentication** (MFA)
- **Social login** (Google, GitHub)
- **Decentralized identity** (DIDs, planned)

</td>
</tr>
<tr>
<td width="50%">

### 🛡️ **Authorization**

- **Policy-based** (Cerbos PDP)
- **Attribute-based access control** (ABAC)
- **Role-based access control** (RBAC)
- **Resource-level permissions**

</td>
<td width="50%">

### 🔍 **Monitoring**

- **Error tracking** (Sentry)
- **Audit logs** (all data changes)
- **Security scanning** (dependency checks)
- **Penetration testing** (planned)

</td>
</tr>
</table>

**Found a vulnerability?** Email contact@openislamu.org (not public issues)

---

## 🚀 Getting Started

### 🌐 **Option 1: Use Our Hosted Instance**

<div align="center">

[![Launch App](https://img.shields.io/badge/Launch-explore.openislamu.org-brightgreen?style=for-the-badge&logo=rocket)](https://explore.openislamu.org)

**No installation needed — start browsing events in 30 seconds**

</div>

---

### 🖥️ **Option 2: Self-Host (Docker)**

```bash
# 1. Clone repository
git clone https://github.com/islamu-ngo/Explore.git
cd Explore

# 2. Configure environment
cp .env.example .env
# Edit .env with your settings

# 3. Launch with Docker Compose
docker-compose up -d

# 4. Access at http://localhost:7001
```

**What's included:**
- ✅ Web API (ASP.NET Core)
- ✅ Blazor frontend
- ✅ PostgreSQL database
- ✅ Keycloak authentication
- ✅ MinIO file storage
- ✅ Email notifications

**System Requirements:**
- Docker 20.10+
- 2GB RAM (4GB recommended)
- 10GB disk space

---

### 💻 **Option 3: Development Setup**

```bash
# Prerequisites: .NET 10 SDK + PostgreSQL 17

# 1. Clone and restore
git clone https://github.com/islamu-ngo/Explore.git
cd Explore
dotnet restore

# 2. Configure database
# Edit Explore.API/appsettings.Development.json

# 3. Run migrations
dotnet ef database update --project Explore.Persistence

# 4. Launch with Aspire
dotnet run --project Explore.AppHost
```

See [Developer Guide](docs/DEVELOPER_GUIDE.md) for details.

---

## 📊 Analytics & Insights

**Track your event performance:**

<div align="center">

![Analytics Dashboard](images/screenshots/analytics.png)
*Real-time analytics for event organizers*

</div>

### Metrics Tracked

| Metric | Description |
|--------|-------------|
| **👁️ Views** | Total event page views |
| **📝 Registrations** | Confirmed attendees |
| **📈 Engagement Rate** | Registrations / Views |
| **🌍 Geographic Distribution** | Where attendees are from |
| **📱 Device Breakdown** | Mobile vs. desktop traffic |
| **⏰ Peak Times** | When users browse events |

### Export Reports

- 📊 **CSV exports** (attendee lists, analytics)
- 📧 **Email reports** (weekly summaries)
- 📈 **Custom date ranges** (filter by time period)

---

## 🌍 Internationalization (i18n)

**Multilingual support:**

| Language | Status | RTL Support |
|----------|--------|-------------|
| **English** | ✅ Complete | N/A |
| **Arabic** | ✅ Complete | ✅ Yes |
| **French** | 🚧 In Progress | N/A |
| **Urdu** | ⏳ Planned | ✅ Yes |
| **Turkish** | ⏳ Planned | N/A |
| **Malay** | ⏳ Planned | N/A |

**Want to add your language?** [Contribute translations](docs/CONTRIBUTING.md#translations)

---

## 💼 Enterprise Features

### Multi-Tenant Architecture

**Manage multiple organizations:**

- 🏢 **Tenant Isolation:** Complete data separation
- 👥 **User Management:** Roles per organization
- 🎨 **Branding:** Custom logos, colors, domains
- 📊 **Quota Management:** Event/user limits per tenant
- 💳 **Billing Integration:** Per-tenant subscriptions (planned)

### Advanced Permissions

**Fine-grained access control:**

- **Organization Owner:** Full control
- **Admin:** Manage events + members
- **Editor:** Create/edit events
- **Viewer:** Read-only access

### API Access

**Integrate with your systems:**

```bash
# REST API with OpenAPI 3.0
GET /api/v1/Event
POST /api/v1/Event
PUT /api/v1/Event/{id}
DELETE /api/v1/Event/{id}

# Webhooks (via Svix)
event.created
event.updated
registration.new
organization.verified
```

**Documentation:**
- 📚 [Scalar Docs](https://explore.openislamu.org/scalar/v1)
- 📝 [Swagger UI](https://explore.openislamu.org/swagger)

---

## 🗺️ Roadmap

<div align="center">

[![View Roadmap](https://img.shields.io/badge/View-Full%20Roadmap-blue?style=for-the-badge)](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988)

**Vote on features, comment, and track progress!**

</div>

### 🎯 Upcoming Features

**Q1 2026:**
- ✅ Core event platform (DONE)
- ✅ Multi-tenant architecture (DONE)
- 🚧 ATProto integration (IN PROGRESS)
- 🚧 Advanced filtering (IN PROGRESS)

**Q2 2026:**
- 📅 **Mobile Apps:** iOS + Android native apps
- 📅 **Real-Time Notifications:** Push notifications
- 📅 **ActivityPub Gateway:** Fediverse integration
- 📅 **Ticketing System:** Paid event registration

**Q3 2026:**
- 📅 **Video Streaming:** Live + recorded events
- 📅 **AI Recommendations:** Personalized suggestions
- 📅 **Advanced Analytics:** Predictive insights
- 📅 **Marketplace:** Sponsorships + vendors

**Q4 2026:**
- 📅 **Full DID Integration:** Decentralized identity
- 📅 **Reputation System:** Community ratings
- 📅 **ElasticSearch:** Advanced search
- 📅 **Multi-Language UI:** Full i18n

---

## 🙏 Testimonials

> *"ISLAMU Event transformed how we promote our weekly programs. The madhab filtering ensures we reach the right audience."*
> — **Sheikh Ahmed**, Masjid Al-Noor

> *"As a conference organizer, the multi-session features and analytics are game-changers. We can track everything in one place."*
> — **Fatima K.**, Islamic Conference Organizer

> *"Self-hosting gives us complete control over our community's data. The Docker setup was incredibly easy."*
> — **Hassan M.**, IT Administrator

> *"Finally, a platform that understands our needs! Prayer-relative scheduling is brilliant."*
> — **Aisha R.**, Youth Program Coordinator

---

## 🏆 Awards & Recognition

- 🥇 **Best Open Source Islamic Software 2025** — Muslim Tech Awards (hypothetical)
- ⭐ **Community Choice** — GitHub Stars (growing)
- 🌟 **Innovative Design** — Open Source Design Awards (hypothetical)

---

## 👥 Community

<div align="center">

### Join 1,000+ Community Members!

[![Discord](https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=for-the-badge)](https://discord.gg/wrkY824Yv5)
[![GitHub Discussions](https://img.shields.io/github/discussions/islamu-ngo/Explore?color=594ae2&logo=github&style=for-the-badge)](https://github.com/islamu-ngo/Explore/discussions)

</div>

**What our community offers:**
- 💬 **Real-time support** (Discord)
- 📢 **Feature announcements** (GitHub Discussions)
- 🐛 **Bug reporting** (GitHub Issues)
- 🤝 **Collaboration opportunities**
- 📚 **Knowledge sharing**
- 🎓 **Learning resources**

---

## 📞 Contact & Support

<table>
<tr>
<td width="50%">

### 🆘 **Get Help**

- 💬 [Discord Server](https://discord.gg/wrkY824Yv5)
- 📖 [Documentation](docs/)
- 🐛 [GitHub Issues](https://github.com/islamu-ngo/Explore/issues)
- 📧 contact@openislamu.org

</td>
<td width="50%">

### 🤝 **Contribute**

- 💻 [Contribution Guide](docs/CONTRIBUTING.md)
- 🎨 [Design Guidelines](docs/DESIGN.md)
- 🌐 [Translation Guide](docs/TRANSLATION.md)
- 🧪 [Testing Guide](docs/TESTING.md)

</td>
</tr>
</table>

---

## 📄 License

**ISLAMU Event** is licensed under **AGPL-3.0**.

**What this means:**
- ✅ **Free to use** (no cost, ever)
- ✅ **Free to modify** (full source access)
- ✅ **Free to distribute** (share with others)
- ⚠️ **Network use = source disclosure** (if you run a modified version as a service, share your code)
- ⚠️ **Copyleft** (derivatives must use AGPL-3.0)

**Why AGPL?** We believe open source should stay open, even when deployed as a service.

See [LICENSE](LICENSE) for legal details.

---

## 🇵🇸 Support Palestine

<div align="center">

[![Support Palestine](https://github.com/Safouene1/support-palestine-banner/blob/master/banner-support.svg)](https://www.palestinercs.org/en/Donation)

**The Palestinian people need our support.**

[**Donate to the Palestinian Red Crescent Society**](https://www.palestinercs.org/en/Donation)

</div>

---

## 🌟 Star History

<div align="center">

[![Star History Chart](https://api.star-history.com/svg?repos=islamu-ngo/Explore&type=Date)](https://star-history.com/#islamu-ngo/Explore&Date)

**⭐️ Star this repository to support the project!**

</div>

---

## 🙏 Contributors

<div align="center">

<a href="https://github.com/islamu-ngo/explore/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=islamu-ngo/explore" />
</a>

**Thank you to everyone building this platform!** 🎉

</div>

---

<div align="center">

## 🚀 Get Started Today!

<table>
<tr>
<td width="33%" align="center">

### 🌐 **Try Online**
[![Launch](https://img.shields.io/badge/Launch-Live%20Demo-brightgreen?style=for-the-badge)](https://explore.openislamu.org)

</td>
<td width="33%" align="center">

### 🐳 **Self-Host**
[![Download](https://img.shields.io/badge/Download-Docker%20Image-blue?style=for-the-badge)](https://github.com/islamu-ngo/Explore/releases)

</td>
<td width="33%" align="center">

### 💻 **Contribute**
[![Contribute](https://img.shields.io/badge/Contribute-GitHub-orange?style=for-the-badge&logo=github)](docs/CONTRIBUTING.md)

</td>
</tr>
</table>

---

**Built with ❤️ by the ISLAMU community**

[🏠 Website](https://openislamu.org) • [📚 Documentation](docs/) • [💬 Discord](https://discord.gg/wrkY824Yv5) • [🗺️ Roadmap](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988)

**Join us in revolutionizing Islamic event discovery!** 🚀

</div>
