# ISLAMU Event — Discover Islamic Events Worldwide 🌍

<div align="center">

![ISLAMU Event Banner](images/banner.png)

**Open-source event discovery platform connecting Muslim communities globally**

[![Join Discord](https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Join%20Discord&logo=discord&logoColor=%237289da&style=for-the-badge)](https://discord.gg/wrkY824Yv5)
[![Star on GitHub](https://img.shields.io/github/stars/islamu-ngo/Explore?color=594ae2&style=for-the-badge&logo=github)](https://github.com/islamu-ngo/Explore/stargazers)
[![License: AGPL v3](https://img.shields.io/github/license/islamu-ngo/Explore?color=594ae2&style=for-the-badge)](LICENSE)

[✨ Features](#-why-islamu-event) •
[🚀 Get Started](#-get-started) •
[👥 Community](#-join-our-community) •
[🤝 Contribute](#-how-to-contribute) •
[📖 Roadmap](#-roadmap)

</div>

---

## 🎯 What is ISLAMU Event?

**ISLAMU Event** is the open-source platform helping Muslims discover Islamic events — from local lectures to international conferences, from youth programs to scholarly seminars.

Whether you're a **community organizer** looking to promote your events or a **Muslim seeking knowledge**, ISLAMU Event makes it easy to discover culturally-appropriate Islamic programming near you.

### 🌟 Our Mission

> **Connect Muslim communities worldwide** through a trusted, federated event discovery platform that respects Islamic values and cultural diversity.

---

## ✨ Why ISLAMU Event?

### For Event Seekers 🔍

**Discover events that match your needs:**

- 🕌 **Local & Global Events:** Find events in your city or join online from anywhere
- 👨‍👩‍👧‍👦 **Family-Friendly Filtering:** Filter by audience (children, youth, adults, families)
- 🧕👔 **Gender-Appropriate Options:** Filter by gender-specific or mixed audiences
- 📚 **Topic-Based Search:** Find events by category (Aqidah, Fiqh, Tafsir, Hadith, and more)
- 🕋 **Madhab-Specific:** Filter by Islamic jurisprudence school (Hanafi, Maliki, Shafi'i, Hanbali)
- 🌐 **Multi-Language:** Browse events in Arabic, English, French, and more
- ⏰ **Prayer-Relative Scheduling:** Events aligned with prayer times ("15 minutes after Maghrib")

### For Event Organizers 📢

**Promote your events to the right audience:**

- ✅ **Verified Organization Status:** Build trust with fact-checked verification
- 🎯 **Advanced Targeting:** Reach specific audiences (age, gender, location, madhab)
- 📅 **Multi-Session Events:** Manage conferences, weekly classes, and recurring programs
- 🌍 **Federation-Ready:** Your events can be discovered across the decentralized web
- 📊 **Analytics:** Track views, registrations, and engagement
- 💼 **Multi-Tenant:** Manage multiple organizations under one account

### For Self-Hosters 🖥️

**Own your community's data:**

- 🔓 **Open Source (AGPL-3.0):** Full source code access, no vendor lock-in
- 🐳 **Docker-Ready:** Deploy with docker-compose in minutes
- 🔐 **Privacy-First:** Keep your community's data on your own servers
- 🌐 **Federation-Ready:** Join the ATProto/ActivityPub ecosystem
- 💰 **Free Forever:** No subscription fees, no usage limits

---

## 🎨 Key Features

### 🔍 Smart Discovery

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Culturally-Aware Search                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  📍 Location: Within 25 miles of Chicago, IL                        │
│  👥 Audience: Families with children                                │
│  🧕 Gender: Mixed or Family-friendly                                │
│  📚 Category: Islamic Studies                                       │
│  🕋 Madhab: Shafi'i                                                 │
│  🌐 Language: Arabic, English                                       │
│  📅 Date: This weekend                                              │
│  ⏰ Time: After Asr prayer                                          │
│                                                                     │
│  [Search] ─────────────────────────────────────────────────────     │
│                                                                     │
│  ✅ 12 events found matching your criteria                          │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 🛡️ Two-Tier Verification System

**Build trust with transparent verification:**

| Tier | Description | Badge |
|------|-------------|-------|
| **User-Submitted** | Anyone can post events immediately | 🟡 User Reported |
| **Verified Organization** | Fact-checked, trusted organizations | ✅ Verified |

**Verification Process:**
1. Organization applies with documentation
2. ISLAMU team verifies legitimacy (registration check, online presence)
3. Approved organizations get **Verified Badge**
4. Community moderation for quality control

### 📱 Modern User Experience

- **Responsive Design:** Works perfectly on mobile, tablet, and desktop
- **Progressive Web App (PWA):** Install as native app on any device
- **Real-Time Updates:** Instant notifications for event changes
- **Offline-First:** Browse events even without internet (coming soon)
- **Accessible:** WCAG 2.1 compliant for inclusive access

### 🌐 Federation-Ready

**Join the decentralized web:**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      ISLAMU Event Federation Model                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  🏢 ISLAMU-Hosted Instance (Primary)                                        │
│     • Islamic events globally                                               │
│     • Verified organizations                                                │
│     • Advanced filtering                                                    │
│                                                                             │
│                         ↕️ Federation ↕️                                      │
│                                                                             │
│  🏘️ Community Instances                          🏢 Organization Instances  │
│     • Local mosque events                        • University events        │
│     • Regional programs                          • Organization-specific    │
│     • Custom filtering                           • Private events           │
│                                                                             │
│  🌍 Fediverse Integration (ActivityPub)                                     │
│     • Mastodon, Mobilizon, Pleroma                                          │
│     • Follow organizations                                                  │
│     • RSVP from any platform                                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Benefits:**
- Your data stays with you
- No single point of failure
- Interoperate with other platforms
- Community-owned infrastructure

---

## 🚀 Get Started

### 🌐 Use Our Hosted Instance

Visit **[explore.openislamu.org](https://explore.openislamu.org)** to:
- Browse events (no account needed)
- Create account to post events
- Register for events
- Follow organizations

### 🖥️ Self-Host (Advanced)

Deploy your own instance in **5 minutes**:

```bash
# 1. Clone the repository
git clone https://github.com/islamu-ngo/Explore.git
cd Explore

# 2. Configure environment (copy and edit)
cp .env.example .env

# 3. Start with Docker Compose
docker-compose up -d

# 4. Access at http://localhost:7001
```

**What You Get:**
- Full-featured event platform
- PostgreSQL database
- Keycloak authentication
- MinIO file storage
- Email notifications

See [OPERATIONS.md](OPERATIONS.md) for detailed deployment guide.

---

## 👥 Join Our Community

<div align="center">

### We're Building This Together! 🤝

[![Discord](https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=for-the-badge)](https://discord.gg/wrkY824Yv5)
[![GitHub Discussions](https://img.shields.io/github/discussions/islamu-ngo/Explore?color=594ae2&logo=github&style=for-the-badge)](https://github.com/islamu-ngo/Explore/discussions)

</div>

**Join our Discord server** to:
- 💬 Chat with contributors and users
- 🐛 Report bugs and get help
- 💡 Share feature ideas
- 📢 Stay updated on releases
- 🤝 Find collaboration opportunities

**GitHub Discussions** for:
- 📝 Long-form discussions
- 🗳️ Feature polls and voting
- 📚 Knowledge sharing
- 🎓 Tutorial requests

**Follow a [Code of Conduct](CODE_OF_CONDUCT.md)** in all community channels — respectful, inclusive, and welcoming to all.

---

## 🤝 How to Contribute

**Everyone is welcome!** You don't need to be a developer to contribute.

### 🎨 Non-Technical Contributions

- **📣 Spread the Word:** Share with your community, mosque, or organization
- **🐛 Report Bugs:** Found an issue? [Open a GitHub issue](https://github.com/islamu-ngo/Explore/issues)
- **💡 Suggest Features:** Share your ideas in [Discussions](https://github.com/islamu-ngo/Explore/discussions)
- **📖 Improve Docs:** Help us write better documentation
- **🌐 Translate:** Add your language (Arabic, Urdu, Turkish, French, etc.)
- **🎨 Design:** Create graphics, icons, or UI mockups in [Penpot](https://penpot.app/)
- **📊 Test:** Test new features and provide feedback

### 💻 Technical Contributions

**We welcome developers at all levels!**

1. **Read the contribution guide:**
   - [CONTRIBUTING.md](CONTRIBUTING.md) — Workflow and standards
   - [GOVERNANCE.md](docs/GOVERNANCE.md) — Coding conventions
   - [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) — Critical rules

2. **Find a task:**
   - Browse [GitHub Issues](https://github.com/islamu-ngo/Explore/issues)
   - Check [Roadmap Kanban](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988)
   - Look for `good first issue` or `help wanted` labels

3. **Submit a pull request:**
   ```bash
   # Fork and clone
   git clone https://github.com/YOUR_USERNAME/Explore.git
   cd Explore

   # Create a feature branch
   git checkout -b feature/my-awesome-feature

   # Make your changes and commit
   git add .
   git commit -m "Add awesome feature"

   # Push and open pull request
   git push origin feature/my-awesome-feature
   ```

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core, PostgreSQL, EF Core
- Frontend: Blazor (Server + WASM), MudBlazor
- Infrastructure: Docker, Keycloak, Cerbos, Infisical

See [ARCHITECTURE.md](docs/ARCHITECTURE.md) for technical details.

---

## 📖 Roadmap

<div align="center">

### [🗺️ View Full Roadmap](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988)

**Vote on features, comment, and track progress!**

</div>

### 🚀 Current Focus

- ✅ **Core Platform:** Event creation, search, filtering (DONE)
- ✅ **Multi-Tenant Architecture:** Organization management (DONE)
- 🚧 **ATProto Integration:** Federated event indexing (IN PROGRESS)
- 🚧 **Advanced Filtering:** Madhab, language, prayer-relative scheduling (IN PROGRESS)
- ⏳ **ActivityPub Gateway:** Fediverse interoperability (PLANNED)

### 🌟 Coming Soon

- 📱 **Mobile Apps:** iOS and Android native apps
- 🔔 **Real-Time Notifications:** Push notifications for event updates
- 🎟️ **Ticketing System:** Paid event registration
- 📊 **Analytics Dashboard:** Event organizer insights
- 🤖 **AI-Powered Recommendations:** Personalized event suggestions
- 🌐 **Multi-Language Support:** Full i18n for global communities

### 🎯 Long-Term Vision

- **Decentralized Identity:** Full ATProto DID integration
- **Reputation System:** Community-driven organization ratings
- **Video Streaming:** Live and recorded event streaming
- **Interactive Features:** Q&A, polls, chat during events
- **Marketplace:** Event sponsorships and vendor listings

---

## 📊 Project Stats

<div align="center">

![Contributors](https://img.shields.io/github/contributors/islamu-ngo/Explore?color=594ae2&style=for-the-badge&logo=github)
![Stars](https://img.shields.io/github/stars/islamu-ngo/Explore?color=594ae2&style=for-the-badge&logo=github)
![Forks](https://img.shields.io/github/forks/islamu-ngo/Explore?color=594ae2&style=for-the-badge&logo=github)
![Issues](https://img.shields.io/github/issues/islamu-ngo/Explore?color=594ae2&style=for-the-badge&logo=github)
![Pull Requests](https://img.shields.io/github/issues-pr/islamu-ngo/Explore?color=594ae2&style=for-the-badge&logo=github)

![Repository Stats](https://repobeats.axiom.co/api/embed/a0f11a3d9b80342b5f5965127c2c45871c9d3397.svg "Repobeats analytics")

</div>

---

## 🙏 Thank You to Our Contributors

<div align="center">

<a href="https://github.com/islamu-ngo/explore/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=islamu-ngo/explore" />
</a>

**Every contribution matters — thank you for helping build this platform!**

</div>

---

## 🛡️ Security & Privacy

**We take security seriously:**

- 🔒 **HTTPS Enforced:** All traffic encrypted
- 🔐 **Secure Authentication:** OAuth 2.0/OIDC via Keycloak
- 🗝️ **Secret Management:** Infisical for sensitive credentials
- 🐛 **Vulnerability Reporting:** See [SECURITY-POLICY.md](SECURITY-POLICY.md)

**Found a security issue?** Please **DO NOT** open a public issue. Email us at **contact@openislamu.org** and we'll respond within 48 hours.

---

## 🏆 Acknowledgements

**Built with incredible open-source tools:**

<div align="center">

| Tool | Purpose |
|------|---------|
| [Keycloak](https://www.keycloak.org/) | Authentication |
| [Cerbos](https://www.cerbos.dev/) | Authorization |
| [MudBlazor](https://www.mudblazor.com/) | UI Components |
| [PostgreSQL](https://www.postgresql.org/) | Database |
| [Coolify](https://coolify.io/) | Deployment |
| [Plane](https://plane.so/) | Project Management |
| [Penpot](https://penpot.app/) | Design |

**...and [many more](docs/ACKNOWLEDGEMENTS.md)!**

</div>

---

## 📞 Contact Us

<div align="center">

### Have Questions? We're Here to Help! 💬

</div>

- **💬 Discord:** [Join our community server](https://discord.gg/wrkY824Yv5)
- **🐛 Bug Reports:** [Open a GitHub issue](https://github.com/islamu-ngo/Explore/issues/new)
- **💡 Feature Requests:** [Start a discussion](https://github.com/islamu-ngo/Explore/discussions)
- **📧 Email:** contact@openislamu.org

---

## 📄 License

**ISLAMU Event** is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

**What this means:**
- ✅ **Free to use** for any purpose
- ✅ **Free to modify** and distribute
- ✅ **Open source forever** — copyleft ensures derivatives stay open
- ⚠️ **Network use = source code release** — if you run a modified version as a service, you must share your source code

See [LICENSE](LICENSE) for full legal terms.

---

## 🇵🇸 Support Palestine

<div align="center">

The ongoing suffering of the Palestinian people is a humanitarian crisis that demands our attention and support.

[![Support Palestine](https://github.com/Safouene1/support-palestine-banner/blob/master/banner-support.svg)](https://www.palestinercs.org/en/Donation)

**[Donate to the Palestinian Red Crescent Society](https://www.palestinercs.org/en/Donation)**

</div>

---

<div align="center">

## ⭐️ Star Us on GitHub!

**If ISLAMU Event helps your community, give us a star!** ⭐️

It helps others discover the project and motivates our contributors.

[![Star History Chart](https://api.star-history.com/svg?repos=islamu-ngo/Explore&type=Date)](https://star-history.com/#islamu-ngo/Explore&Date)

---

**Built with ❤️ by the ISLAMU community**

[🏠 Website](https://openislamu.org) • [📚 Docs](docs/) • [💬 Discord](https://discord.gg/wrkY824Yv5) • [🗺️ Roadmap](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988)

**Join us in building the future of Islamic event discovery!** 🚀

</div>
