I have an open source self-hostable software which is an event platform, and it has multi-tenancy support. The way I currently implement it is that, when it is self-hosted, first there is the instance onboarding. The one who self-hosted it then logs in; this account becomes the instance administrator account, and he is left to choose between the single-tenant mode and the multi-tenant mode. He can switch out runtime at any time by going to the settings and turning on multi-tenancy, with a warning that tells it that if you have more than one tenant, then you can't reverse to single-tenant mode. You need to delete any other tenant to only have one tenant to go back to single-tenant mode.

When it's single-tenant mode, there is this abstraction taking place where all the advanced features of multi-tenancy administration for the instance administration, like locking certain settings, override for certain tenants, or allowing it for others, having default things, and whether or not tenants can override some settings. Well, it leaves the experience to be that when you go on the domain, on the webapp, it directly redirects any user to the landing page, and it's as if it's simply a user of the application.

Now, for the tenant admin or the instance administrator, he clicks on the profile icon on the right top of the page, and then there is a button that redirects to the administration page. It then leads to a page which is the tenant administration page, but because it's in single-tenant mode deployment, there also is the button to go to multi-tenant mode.

Now I guess I've nailed the implementation for the single-tenant mode, but now, if the self-host chooses to go to multi-tenant mode, how do I handle it? There is no longer tenant-specific, so there needs to be a page dedicated for the instance administrator. How are multi-tenant SaaS companies handling it? When they choose multi-tenant mode deployment, should they be redirected to an instance administrator dashboard where there is this vision and management of all the different tenants, with analytics and so on and intance settings like allowing override of settings by tenants or locking the setting ? Now then, let's say that it is in multi-tenant mode and the instance administrator goes to the web app. If he goes in while he is not logged in, it will display the tenant that he is currently going into. Oh right, I hadn't talked about it at all. At the base domain, where should it redirect if there are multiple tenants? You can have tenant resolution multiple ways:
- with the header passing in the header
- passing with the subdomain
- with the domain
can go to the right tenant.
Now let's say the user goes to the web app domain. It shouldn't be tied to a tenant, right? Should the default domain be a landing page? I don't want to do it. I want to have a separate landing page website.
Now let's say he's logged in as the instance administrator and he goes to the instance. Should he be directly redirected to the instance administration dashboard? how should self registration of tenanats be done ? tell me all about that! with in mind this is self hostable agpl v3 anybody can host for a saas offering or for their own use case.

You’re touching a **classic architectural problem** in self-hostable multi-tenant platforms: separating **instance administration** from **tenant usage** while keeping the UX clean. Most mature platforms (GitLab, Keycloak, Sentry, Supabase, Authentik, etc.) solve this with **two logical control planes**:

1. **Instance / Platform administration**
2. **Tenant / Workspace administration**

Below is the architecture pattern that works best for **self-hostable SaaS software** like yours.

---

# 1. Core Concept: Separate Control Planes

You should clearly separate:

| Layer                      | Who uses it            | What it manages                    |
| -------------------------- | ---------------------- | ---------------------------------- |
| **Instance Control Plane** | Instance administrator | Platform settings, tenants, limits |
| **Tenant Control Plane**   | Tenant admins          | Tenant data, users, events         |
| **Tenant Application**     | Normal users           | Using the event platform           |

This separation exists in many systems:

Examples:

* GitLab → Admin Area vs Projects
* Keycloak → Master Realm vs Realms
* Sentry → Organization vs Projects
* Supabase → Platform vs Projects

---

# 2. Domain Structure (Very Important)

The cleanest architecture:

```
platform.example.com      -> instance admin portal
tenant1.example.com       -> tenant application
tenant2.example.com       -> tenant application
```

Optional:

```
api.example.com
```

### Why this works

The **instance control plane must not belong to any tenant**.

So:

```
platform.example.com
```

is the **global admin panel**.

---

# 3. Base Domain Behavior

You asked what should happen when visiting the base domain.

### Recommended

```
example.com -> redirect to external marketing website
```

Your event platform should **not host the marketing site**.

Example pattern:

```
example.com           -> marketing site
app.example.com       -> tenant resolver
platform.example.com  -> instance admin
```

---

# 4. Tenant Resolution

Support multiple strategies (good idea).

Typical order:

1️⃣ **Custom domain**

```
events.mosque.org
```

2️⃣ **Subdomain**

```
tenant.example.com
```

3️⃣ **Header**

```
X-Tenant-ID
```

4️⃣ **Path**

```
example.com/t/tenant
```

For self-hosted environments, **subdomain + custom domain** is usually enough.

---

# 5. Instance Administrator Experience

When multi-tenant mode is enabled:

The instance admin should **never enter a tenant automatically**.

Instead:

```
platform.example.com
```

Opens:

### Instance Dashboard

Example layout:

```
Instance Dashboard
-------------------

Tenants
- tenant1
- tenant2
- tenant3

Statistics
- active tenants
- events created
- users

System
- instance settings
- default configurations
```

---

# 6. Instance Dashboard Features

The instance admin must manage:

### Tenants

* create tenant
* suspend tenant
* delete tenant
* impersonate tenant admin
* usage statistics

---

### Platform Settings

Examples:

```
tenant_self_registration
allow_custom_domains
allow_theme_override
allow_email_override
allow_analytics_override
```

These correspond exactly to your **override/lock system**.

Example:

```
Email provider
[Locked globally]

Theme
[Tenants may override]
```

---

### Default Settings

Defaults applied to new tenants:

```
default_event_visibility
default_storage_quota
default_plan
default_features
```

---

# 7. Tenant Creation Models

There are **three models**.

You should support **all three**.

---

## Model 1 — Instance Admin Creates Tenant

Used by:

* private organizations
* internal deployments

Flow:

```
Admin dashboard
→ Create tenant
→ configure domain
→ create first tenant admin
```

---

## Model 2 — Tenant Self Registration

Used by SaaS providers.

Example:

```
Create organization
Organization name
Subdomain
Admin account
```

Flow:

```
signup.app.com
```

Then create tenant automatically.

Settings:

```
allow_tenant_self_registration = true
```

---

## Model 3 — Invite Based

Example:

```
request organization
admin approves
```

Less common.

---

# 8. After Tenant Creation

Tenant gets:

```
tenant.example.com
```

Tenant admin logs in there.

Tenant admin **cannot access instance admin panel**.

---

# 9. Instance Admin Access to Tenants

Provide:

### Impersonation

Instance admin can:

```
Login as tenant admin
```

or

```
Open tenant dashboard
```

Example:

```
platform.example.com/tenants/tenant1/open
```

---

# 10. What Happens When Instance Admin Visits Tenant Domain

Example:

```
tenant1.example.com
```

If instance admin logs in:

Show **tenant UI**, but with an **instance admin banner**:

```
You are viewing this tenant as instance administrator
```

This is extremely common.

---

# 11. Single-Tenant Mode

Your idea is correct.

Single tenant mode should behave as if:

```
tenant = default
```

So the app behaves like a normal product.

But internally:

```
tenant_id = 1
```

---

# 12. Switching Single ↔ Multi Tenant

Your rule is good:

```
multi → single allowed only if 1 tenant
```

Exactly correct.

Many platforms enforce this.

---

# 13. Suggested URL Architecture

Example:

```
platform.example.com
   /dashboard
   /tenants
   /settings
   /analytics
```

Tenant:

```
tenant.example.com
   /dashboard
   /events
   /settings
   /users
```

---

# 14. Recommended Role System

You should have two separate role sets.

### Instance Roles

```
InstanceOwner
InstanceAdmin
InstanceOperator
```

---

### Tenant Roles

```
TenantOwner
TenantAdmin
EventManager
User
```

Never mix them.

---

# 15. Instance Analytics

Instance dashboard should show:

```
Total tenants
Total users
Total events
Total storage
Activity
```

Very useful for SaaS operators.

---

# 16. Tenant Discovery

If someone visits:

```
app.example.com
```

You have options:

### Option A — Organization chooser

Like Slack.

User logs in → sees list of tenants.

---

### Option B — Direct tenant domain only

Simpler.

User must know tenant domain.

---

# 17. What Most SaaS Platforms Do

Examples:

| Platform | Instance admin URL |
| -------- | ------------------ |
| GitLab   | `/admin`           |
| Keycloak | `/admin/master`    |
| Sentry   | `/settings`        |
| Supabase | `/org`             |

But many modern platforms use **separate domain** which is cleaner.

---

# 18. My Recommended Architecture for Your Platform

```
example.com
    -> marketing site

app.example.com
    -> tenant resolver

platform.example.com
    -> instance administration
```

Tenant domains:

```
tenant1.example.com
tenant2.example.com
custom-domain.org
```

---

# 19. Why This Is Perfect for Self-Hosting

Because self-hosters can choose:

### SaaS Mode

```
platform.domain.com
tenant.domain.com
```

### Internal Mode

```
events.company.com
```

Single tenant.

### Hybrid

```
tenant1.company.com
tenant2.company.com
```

---

# 20. Key Rule

Never mix:

```
instance control plane
tenant application
```

They must always be logically separate.

---

# If you want, I can also show you the **best database schema design for tenants** (including tenant resolution, domain mapping, and instance settings tables).

It will make your platform **enterprise-grade like Stripe / GitLab / Sentry**.


Well, during the instance onboarding, when running the app for the first time, the databases are clean except for the seed data from the first migration. It really is a clean state. I ask whether this user, which is the instance administrator, wants to self-host for single-tenant mode or in SaaS mode. If you choose single-tenant mode, then there shouldn't be a redirect to the marketing side, right? Even if he chooses multi-tenant mode, should there be a redirect to the marketing side? What I try to say is it will require the self-hosting to not only host the API and the Keycloak for authentication, read this and all this heavy infrastructure, and also add a website app. This increases the requirements for the server and makes it more heavy on infrastructure and so on. I'm like, "How can they have the benefit of it, especially talking about those who will go into SaaS mode or will switch into SaaS mode, the advantage of the marketing page, which must be absolutely customizable, right? The landing page that I will have is fully for my purpose of the instance, because it's open source and so I will self-host an instance for my organization, which will be for Islamic events only. Others can use this software for other purposes, like tech events or whatsoever, and my landing page won't be a landing page only for Islamic events. It'll be a landing page for my organization; it's one of the solutions amongst many of my organization, and I can simply have all those different tenants and so on. People will self-host and redirect to my landing page for Islamic events. It doesn't make any sense, right, or still I redirect them to my ISLAMU non-profit organization, which is an organization for Islamic software. As you can see, sometimes software can be for multi-purpose, so it doesn't go into conflict with it, as long as my instance is for Islamic events and others can be white-labeled.

What I'm asking is, should I simply make this my own marketing page for Islamic events, also just as I've done the repo to show the multi-purpose and the white-labeling capacity and how it's neutral for any use cases to be self-hosted? She'll make this marketing page as neutral as possible for it to be rejected by any self-hoster, or should they include a separate project for marketing, which will be customizable and go alongside that they can self-host? Explain to me really, and also for the domain, well basically i will have projectname.orgname.tld just like google does with maps.google.com for example. then orgname.tld will be marketing site for all the different projects (see how posthog has all their offering 1 same marketing site and documentation for all their products and different navmenu tabs per product and it's like multiple websites in 1.


You’re asking a **very important product architecture question for self-hostable software**:

> Should the platform include a marketing site or not?

This is something many successful open-source SaaS platforms had to decide. The answer is **not obvious**, but there is a very clear best practice that avoids the problems you’re worried about.

---

# The Key Principle

**The application must NOT depend on a marketing website.**

A marketing website is **optional infrastructure**, not required infrastructure.

Your event platform should work perfectly with only:

```
API
Web App
Auth (Keycloak)
Database
```

Nothing else.

---

# Why bundling a marketing site is problematic

If your platform **requires a marketing site**, then self-hosters must deploy:

```
API
WebApp
Auth
Database
Marketing website
```

This introduces several issues:

### 1️⃣ Extra infrastructure

Small organizations self-hosting internally do not want this.

Example:

```
events.company.org
```

They just want the application.

---

### 2️⃣ Branding mismatch

Your marketing site might say:

```
ISLAMU Events Platform
```

But someone might use it for:

```
Tech conferences
School events
Gaming tournaments
NGO events
```

---

### 3️⃣ Maintenance burden

Marketing sites change often:

* blog
* docs
* announcements
* SEO
* content

This should not be tied to the application runtime.

---

# The correct architecture

Your app should provide **a minimal default entry page**, not a marketing site.

Think of it as a **portal page**.

Example when visiting the root domain:

```
example.com
```

Show something like:

```
Welcome to this Event Platform instance

Tenants:
- islamic-events
- community-events
- youth-events

Login
Create organization (if enabled)
```

Minimal. Neutral.

---

# This is exactly what major platforms do

Example behaviors:

### GitLab

Self-hosted instance root page:

```
Welcome to GitLab
Sign in
Register
Explore projects
```

No marketing.

---

### Keycloak

Self-hosted root page:

```
Keycloak Administration Console
```

Minimal.

---

### Sentry

Self-hosted:

```
Login / Register
```

No marketing.

---

# What should happen during onboarding

During first run:

```
/setup
```

Instance admin chooses:

```
Single tenant mode
Multi-tenant SaaS mode
```

After that:

### Single-tenant mode

Root domain goes directly to the application.

Example:

```
events.company.org
```

Opens the tenant.

No marketing page.

---

### Multi-tenant mode

Root domain becomes **instance portal**.

Example:

```
events.example.org
```

Shows:

```
Instance portal

Login
Create organization
Browse tenants (optional)
```

---

# Do NOT redirect to a marketing site automatically

Because the platform cannot assume:

```
example.org
```

belongs to the same infrastructure.

Many deployments look like:

```
app.company.org
auth.company.org
api.company.org
```

But marketing lives on:

```
company.org
```

Different server entirely.

---

# The best compromise

Include a **very simple built-in landing page**.

Neutral.

Example:

```
Welcome to the Event Platform

Login
Create Organization
Documentation
Powered by <project name>
```

Self-hosters can disable or customize it.

---

# Optional feature: Custom Landing Page

Add configuration:

```
landing_page_mode:

default
redirect
custom
```

---

### Mode 1 — Default

Built-in minimal page.

---

### Mode 2 — Redirect

Redirect to another site:

```
https://example.org
```

Used when someone has a marketing site.

---

### Mode 3 — Custom HTML

Self-hosters can upload a custom landing page.

Example:

```
/var/www/custom-landing
```

---

# Your own deployment strategy

You described a **very good structure** already.

Example:

```
orgname.tld
    marketing + docs

events.orgname.tld
    event platform instance
```

Or even:

```
events.islamu.org
```

This is exactly how modern SaaS works.

Example:

| Marketing    | App              |
| ------------ | ---------------- |
| posthog.com  | app.posthog.com  |
| sentry.io    | app.sentry.io    |
| supabase.com | app.supabase.com |

---

# Your platform repo structure (recommended)

```
event-platform/
    backend/
    webapp/
    infrastructure/
```

Separate repo:

```
event-platform-marketing/
```

Optional.

---

# What self-hosters should deploy

Minimal deployment:

```
api
webapp
keycloak
postgres
```

Optional:

```
marketing site
docs
blog
```

---

# One more important SaaS feature

In multi-tenant mode you should allow:

```
tenant_self_registration
```

Example flow:

User visits:

```
events.example.com
```

Clicks:

```
Create Organization
```

Creates:

```
mosque-events.example.com
```

---

# Your system design is already very good

You correctly implemented:

✔ single vs multi tenant runtime switch
✔ tenant overrides
✔ instance overrides
✔ lockable settings

This is **very advanced SaaS architecture** already.

---

# My final recommendation

Your platform should include:

### ✔ Built-in neutral portal page

Minimal.

### ✔ Configurable redirect to external marketing site

Optional.

### ✔ No dependency on marketing site

Critical.

### ✔ Separate instance admin panel

Example:

```
/admin
```

or

```
platform.domain.com
```

---

# The mental model

Your project is:

```
Infrastructure software
```

Not:

```
Website software
```

---

If you want, I can also show you **the ideal multi-tenant SaaS architecture used by Stripe / Sentry / Supabase**, including **tenant resolution, routing, and domain mapping**, which will make your platform extremely robust.


well now with that subdomain resolution for the different pages. well for self hosters it's difficult right? so they should have dns setup correctly with their provider to redirect to correct ip. so no worries if it's for saas mode deployment. but for signle tenant mode it should be as eassy as possible so like only have in signle tenant mode the app's url always redirect to the eventlist page. then they go to settings page and it has settings for turning on multi tenant mode if the user is instance administrator? but during onboarding we ask if in signle tenant mode or saas mode and if they pick saas mode there instead of single tenant then there are dns settings to do to redirect to correct subdomain right? then have an input field for the domain to add for the platform settings with instruction for how to setup with their dns provider? are those different webapps ? when you say multiple controle plane ? meaning multiple blazor projects ? explain it all


You’re now touching **deployment ergonomics**, which is one of the hardest parts of designing a **self-hostable multi-tenant SaaS platform**. Your instincts are good: **single-tenant must be extremely easy**, while **SaaS mode can require more infrastructure**. Let’s break this down clearly.

---

# 1️⃣ Single-Tenant Mode Should Be Dead Simple

Your idea is correct.

A single-tenant deployment should require **only one domain**.

Example:

```
events.company.org
```

User flow:

```
events.company.org → Event list
```

No tenant resolution needed.

Internally you still keep:

```
tenant_id = 1
```

But the user never sees this.

### Admin access

From the profile menu:

```
Profile → Administration
```

Then:

```
/admin
```

This is the **tenant admin panel**.

---

# 2️⃣ Enabling Multi-Tenant Mode Later

You are correct again: the toggle should exist in settings.

Example:

```
Settings → Instance Settings → Enable Multi-Tenant Mode
```

Warning:

```
Enabling multi-tenant mode requires DNS configuration.
```

After enabling:

```
instance admin dashboard appears
```

---

# 3️⃣ DNS Requirements for SaaS Mode

Yes, self-hosters must configure DNS.

Typical setup:

```
*.events.example.org → server IP
```

Wildcard DNS.

Example records:

```
A record
*.events.example.org → 123.45.67.89
```

This allows:

```
tenant1.events.example.org
tenant2.events.example.org
tenant3.events.example.org
```

without creating each manually.

You should show **instructions in the UI**.

Example page:

```
Instance Settings → Domains
```

Display:

```
Platform domain:
events.example.org

Required DNS configuration:

A record:
*.events.example.org → 123.45.67.89
```

---

# 4️⃣ Domain Configuration UI

You should store domains in the database.

Example table:

```
instance_domains
```

Fields:

```
id
domain
is_primary
tenant_resolution_strategy
```

Example values:

```
events.example.org
tenant.events.example.org
```

---

# 5️⃣ Tenant URL Example

Once configured:

Tenant creation flow:

```
Tenant name: Mosque Events
Subdomain: mosque
```

Tenant URL becomes:

```
mosque.events.example.org
```

---

# 6️⃣ Do You Need Multiple WebApps?

No.

This is a very common misunderstanding.

You **do not need multiple Blazor projects**.

You can run **one web application** that behaves differently depending on the route and tenant context.

Example:

```
Blazor WebApp
```

Handles:

```
Tenant UI
Tenant Admin
Instance Admin
```

All in one.

---

# 7️⃣ What “Control Plane” Means

Control plane does **NOT mean separate applications**.

It means **separate areas of responsibility**.

Example:

### Instance Control Plane

Used by instance admins.

Routes:

```
/instance
/instance/tenants
/instance/settings
/instance/analytics
```

---

### Tenant Control Plane

Used by tenant admins.

Routes:

```
/admin
/admin/events
/admin/users
/admin/settings
```

---

### Application Plane

Used by normal users.

Routes:

```
/
/events
/event/{slug}
```

---

# 8️⃣ Routing Example

Your Blazor router might look like:

```
/instance/*
/admin/*
/events/*
```

Access controlled by roles.

Example:

| Role          | Access      |
| ------------- | ----------- |
| InstanceAdmin | `/instance` |
| TenantAdmin   | `/admin`    |
| User          | `/events`   |

---

# 9️⃣ Tenant Resolution in Backend

Your middleware resolves tenant.

Pseudo logic:

```
host = request.Host

if host == platform_domain
    tenant = null
else
    tenant = resolveTenant(host)
```

Example:

```
mosque.events.org
```

Subdomain:

```
mosque
```

Lookup:

```
tenant_domains
```

---

# 10️⃣ What Happens on Root Domain in SaaS Mode

Example:

```
events.example.org
```

This should open the **instance portal**.

Example page:

```
Welcome to this Event Platform instance

Login
Create organization
Browse organizations
```

---

# 11️⃣ Tenant Self Registration

If enabled:

```
Create organization
```

User fills:

```
Organization name
Subdomain
Admin account
```

Tenant created automatically.

---

# 12️⃣ Instance Admin Dashboard

Accessible at:

```
/instance
```

Contains:

```
Tenants
Instance settings
Analytics
Plans
Storage
Logs
```

---

# 13️⃣ Single-Tenant → Multi-Tenant Migration

Your rule is perfect:

```
multi → single allowed only if 1 tenant
```

Implementation:

```
if tenant_count > 1
    disable revert
```

---

# 14️⃣ Why This Design Is Used by Many Platforms

Examples:

* GitLab
* Keycloak
* Sentry

They all use:

```
single app
multiple control planes
```

Not multiple webapps.

---

# 15️⃣ What Your Architecture Might Look Like

```
ASP.NET API
Blazor WebApp
Keycloak
PostgreSQL
```

Blazor structure:

```
Pages/
    Events/

Admin/
    Tenant/

Instance/
    Tenants/
    Settings/
```

---

# 16️⃣ Developer Mental Model

Think of it like this:

```
instance
   ├── tenant
   │     ├── events
   │     └── users
   └── tenant
```

The **instance admin manages tenants**.

The **tenant admin manages events**.

---

# 17️⃣ Self-Hosting Experience

### Single Tenant

Deploy:

```
docker compose up
```

Visit:

```
events.company.org
```

Done.

---

### SaaS Mode

Extra steps:

1️⃣ Configure DNS wildcard

```
*.events.company.org
```

2️⃣ Set platform domain

```
events.company.org
```

3️⃣ Enable tenant self-registration (optional)

---

# 18️⃣ UX Tip

When enabling SaaS mode show a wizard:

```
Step 1
Configure DNS

Step 2
Set platform domain

Step 3
Create first tenant
```

---

# 19️⃣ You Are Designing a Very Advanced Platform

Your system is essentially:

```
Self-hostable SaaS platform
```

Similar class to:

* Supabase
* Sentry
* GitLab

Which is why these architecture questions appear.

