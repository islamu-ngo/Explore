---
description: >-
  Bind tenant domains safely and operate the focused public-discovery SEO
  surface.
---

# Custom Domains & SEO

Custom domains are tenant-routing and public-accountability boundaries, not only branding aliases.

## Domain setup

Before enabling a domain:

1. prove tenant ownership or authorization;
2. configure DNS and valid TLS;
3. forward the original host through the trusted reverse proxy;
4. establish the canonical host policy;
5. exclude the instance administration host;
6. verify mandatory public disclosures;
7. test unknown-host failure.

Multi-tenant resolution checks trusted BFF context, admin-host exclusion, custom domain, then subdomain. An unknown host fails with `404` and must never select an arbitrary tenant.

## Implemented SEO surface

SEO support is intentionally focused on public event discovery:

* public `/sitemap.xml`;
* environment-aware `/robots.txt`;
* canonical, Open Graph, and Twitter metadata for crawlable public event details;
* schema.org Event JSON-LD for eligible public events;
* `noindex` for non-public or non-crawlable states;
* a minimal web manifest.

## Current limits

The platform does not promise site-wide metadata automation, a dedicated SEO administration console, Search Console integration, global ranking optimization, or indexing outcomes.

Operators remain responsible for domain reputation, content quality, consent requirements, indexing policy, legal notices, search-engine verification, and measurement.

## Acceptance

Fetch sitemap and robots output in production and non-production, inspect canonical and social metadata for a public event, verify non-public states are not indexable, validate JSON-LD, and confirm each tenant domain resolves only its own public content and disclosures.
