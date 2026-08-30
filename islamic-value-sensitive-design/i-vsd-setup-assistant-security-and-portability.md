<!-- ABOUTME: I-VSD consultancy report for the cross-platform ISLAMU Event Setup Assistant. -->
<!-- ABOUTME: Governs manifest authoring, relevant-only dotenv generation, optional secret entry, licensing, and release trust. -->

# I-VSD Consultancy Report: Setup Assistant Security And Portability

Last Updated: 2026-08-29

## Review Metadata

- Mode: standalone
- Subject: ISLAMU Event Setup Assistant
- Workstream: none
- Report kind: consultancy-report
- Report status: current
- Disposition: ready-for-planning
- Evidence cutoff: 2026-08-29
- Reviewed input revision: `sha256:b053b7f69ca3822efbd1dc2333d2138d6361df8dd5eade311a2f43e2532b17ef`
- Supersedes: none

## Scope

This report evaluates a new, small, cross-platform ISLAMU Event Setup
Assistant built with the MIT-licensed Avalonia framework. The product is a
shipped user-facing application under `src/`, not an internal contributor tool.

It covers:

- one shared Avalonia experience targeting WebAssembly and desktop;
- Windows, Linux, and macOS distribution;
- Linux portable archives plus `.deb`, `.rpm`, Arch package, AppImage, and
  optional Flatpak paths subject to license and release review;
- whole-instance `ConfigurationManifest` and tenant-scoped
  `TenantConfigurationPackage` authoring, validation, comparison, and export;
- a separate `.env` generation workflow for operators who do not use
  Infisical;
- progressive disclosure of deployment topology, providers, optional
  capabilities, environment variables, and secrets;
- a default web mode that never asks for secret values;
- an optional web mode that accepts secrets only after an explicit trust
  decision;
- client-side-only web processing with no secret-bearing request to ISLAMU or
  another server;
- desktop file permissions, atomic writes, overwrite safety, memory,
  clipboard, logging, crash, update, and packaging risks;
- a complete per-target FOSS dependency gate covering direct, transitive,
  native, tooling, asset, and packaging obligations;
- typed instance and tenant legal-document configuration, including terms,
  privacy, cookie, accessibility, conduct, moderation, payment, and other
  accountable public texts;
- an offline legal-template library and safe Markdown editor;
- localized legal-document variants, lifecycle, preview, comparison,
  portability, target review, and publication handoff;
- one `Event.SetupAssistant.Cli` executable with deterministic commands and a
  Terminal.Gui interactive TUI using the same core workflows;
- a future project skill that teaches external AI agents to use versioned CLI
  commands without embedding an AI model, provider, prompt runtime, or agent
  loop in the product;
- official hosting, source availability, provenance, branding, and the limits
  of trying to prevent malicious third-party hosting;
- a FOSS-only dependency philosophy that rejects commercial, proprietary, and
  source-available components while evaluating permissive and reciprocal
  licenses against each target’s public AGPL and alternative outbound paths;
- accessibility, localization, RTL, support, release, incident, and evaluation
  duties.

This is a product and security design report. It does not claim that the
software has been implemented.

## Claim Boundary

This report is provider-responsibility design reasoning under I-VSD. It is not:

- a fatwa, halal/haram decision, or Sharia certification;
- legal, privacy, security, accessibility, supply-chain, or license
  certification;
- proof that browser, operating-system, extension, endpoint-protection, or
  local-device compromise cannot expose a secret;
- proof that an online hosting origin is incapable of serving a modified
  malicious build;
- permission to place proprietary, commercial, source-available, unknown, or
  otherwise unapproved dependencies into the release, or to add reciprocal
  FOSS without target-specific compatibility review;
- proof that generated `.env` values are valid for an external provider;
- a substitute for threat modeling, code review, reproducible-build evidence,
  penetration testing, accessibility testing, or qualified legal review.

The strongest truthful web claim is:

> The identified, reviewed release is designed to process entered secrets in
> browser memory and generate the file locally without transmitting secret
> values. Users of the online-hosted build still trust the hosting origin to
> deliver that reviewed code on every load.

The product must never claim “ISLAMU technically cannot obtain your secrets”
for an online-hosted build. The origin controls the HTML, JavaScript, WebAssembly
modules, headers, and future deployment. Transparency, public source,
reproducible artifacts, strict browser policy, testing, and legal commitments
can provide evidence and accountability, but do not erase that technical trust
boundary.

## Context And Current Repository Facts

1. The repository accepts Infisical or `.env` as secret sources. Secrets must
   never be embedded in AppHost, appsettings, source, tests, or manifest files.
2. `.env.example` is currently a large operator-facing template of 618 lines.
   `docker-compose.yml` is 985 lines and contains substantial conditional
   deployment configuration.
3. `SecretDefinitionRegistry` is the current Domain source of truth for
   recognized secret-backed settings, allowed scopes, sources, and default
   environment-variable names.
4. `ConfigurationManifest` intentionally excludes secrets, PII, provider
   credentials, and operational state.
5. The current manifest schema generator references all of
   `Explore.Application`; that dependency is too broad for a small offline
   desktop/browser client.
6. No Avalonia project or package is currently present.
7. The repository centrally pins package versions, commits package lock files,
   and requires dependency-license review for every shipped graph.
8. Avalonia’s official documentation states that the framework is MIT
   licensed, while professional tooling has separate licensing.
9. Avalonia Browser publishes a static site containing HTML, WebAssembly,
   runtime files, and assets; no server-side .NET code is required.
10. Avalonia supports Windows, macOS, Linux, and browser targets, but browser
    file-system capabilities and desktop security controls differ materially.
11. The user selected two web experiences:
    - no-secret input, selected by default;
    - optional secret input for users who explicitly trust the official host.
12. The user requires relevant-only output: required secret keys for selected
    capabilities remain empty in no-secret mode; irrelevant variables and
    defaulted settings are omitted.
13. Current Terms of Service and Privacy Policy pages contain static,
    repository-authored prose rather than instance/tenant-owned typed content.
14. Current public footer contracts already distinguish tenant directory
    operator links from instance platform-operator links; legal text must
    preserve that role separation.
15. Terminal.Gui 2.4.17 package metadata identifies the package as MIT and its
    official documentation describes Windows, macOS, and Linux terminal UI,
    editor, wizard, keyboard, mouse, Unicode, and inline/full-screen support.
16. Existing repository CLIs use deterministic commands, bounded text output,
    stable usage failures, and nonzero exit codes.
17. The user revised the dependency policy from literal MIT-only to
    philosophy-compatible FOSS: no commercial/proprietary dependency, while
    GPL/AGPL and other free licenses may be considered.
18. The Setup Assistant browser source remains open and auditable. Only
    generated `wwwroot`, build, publish, and release artifacts are ignored.

## Requested Direction Coverage

| Requested direction | Report coverage |
|---|---|
| New Avalonia product under `src/` | Product Direction and Project Architecture |
| Web and desktop from one experience | Target Architecture and Target Matrix |
| Windows, Linux, and macOS | Desktop Distribution And Packaging |
| Debian, Arch, and additional Linux paths | Linux packaging matrix |
| No commercial/proprietary dependencies | FOSS Philosophy And Outbound Compatibility Gate |
| GPL/AGPL and other FOSS may be considered | Target-specific reciprocal-license boundary and legal approval |
| Manifest authoring/export | Configuration Portability Workspace |
| `.env` generation | Environment Setup Workspace |
| Progressive optional variables and secrets | Environment Catalogue and Wizard |
| Web defaults to no secret entry | Default No-Secret Web Mode |
| Optional trusted web secret entry | Optional Web Secret Mode |
| Secret values never sent to a server | Browser Network Denial Contract |
| Empty relevant secret placeholders | Relevant-Only Dotenv Rendering |
| Omit irrelevant and defaulted variables | Relevance and omission rules |
| Header explains intentionally partial output | Generated File Header Contract |
| Official-host transparency | Official Web Trust And Provenance |
| Optional gitignored web target | Gitignore And Exclusivity Assessment |
| Instance and tenant legal texts in portable configuration | Legal Document Configuration Model |
| Terms, privacy, and broader legal-content catalogue | Legal Document Kind Catalogue |
| Templates and Markdown editing | Legal Template Library and Safe Markdown Editor |
| Terminal.Gui CLI/TUI | CLI And TUI Product Architecture |
| Same functional experience across GUI and TUI | Functional Parity Contract |
| Agent-automatable commands | Versioned Command And Machine-Output Contract |
| Agentic skill instead of embedded AI | Setup Assistant Agentic Skill and No Embedded AI Boundary |
| All prior consultation recommendations | Architecture, security, packaging, rejected alternatives, and validation sections |

## Findings

### Finding Register

| ID | Lifecycle | Severity | Claim type | Principle/domain | Provider-controlled decision and risk | Evidence and validation level | Mitigation | Owner or escalation |
|---|---|---|---|---|---|---|---|---|
| IVSD-F001 | accepted | High | Product opportunity | Ihsan, Promise-Keeping; Strategic | A focused setup application can materially improve self-hosting and portability | E001-E009; design and implementation traceability | IVSD-M001 | Product |
| IVSD-F002 | accepted | Critical | Architecture requirement | Amanah, Truthfulness; Technical | Duplicated manifest/environment rules across server and UI will drift | E001-E006; implementation traceability | IVSD-M002 | Architecture |
| IVSD-F003 | accepted | Blocker | Web-origin trust boundary | Amanah, Truthfulness; Technical/Governance | An online origin can serve modified code that exfiltrates secrets | E010-E014; standards-based design validation | IVSD-M003 | Security + Legal |
| IVSD-F004 | accepted | Critical | Protective-default requirement | Non-Harm, Avoiding Gharar; Design | Asking for secrets by default normalizes unnecessary exposure | User decision; design validation | IVSD-M004 | Product + Security |
| IVSD-F005 | accepted | Blocker | Web network boundary | Avoiding Spying, Amanah; Technical | Client-side code can transmit secrets through many browser request channels | E010-E014; standards-based design validation | IVSD-M005 | Web Security |
| IVSD-F006 | accepted | Critical | Browser-state boundary | Privacy, Non-Harm; Technical | Storage, crash, extension, autofill, clipboard, memory, and service-worker behavior can retain or expose secrets | E012-E014; design validation | IVSD-M006 | Web Security + UX |
| IVSD-F007 | accepted | High | No-secret usability requirement | Ihsan, Autonomy; Design | Omitting all secret keys would leave users without an actionable deployment file | User decision, E001-E005; implementation traceability | IVSD-M007 | Product |
| IVSD-F008 | accepted | Critical | Catalogue requirement | Truthfulness, Promise-Keeping; Technical | Scraping `.env.example` or duplicating metadata creates undocumented drift | E001-E006; implementation traceability | IVSD-M008 | Architecture + Docs |
| IVSD-F009 | accepted | Blocker | Separation requirement | Amanah, Privacy; Technical | Combining secrets with portable manifests makes shareable configuration unsafe | E006-E007; implementation traceability | IVSD-M009 | Security |
| IVSD-F010 | accepted | Blocker | Desktop file safety | Amanah, Non-Harm; Technical/Operational | Plaintext `.env` can be exposed by weak permissions, symlinks, backups, or unsafe overwrite | E015-E016; standards-based design validation | IVSD-M010 | Desktop + Security |
| IVSD-F011 | accepted | Critical | Secret-memory limitation | Truthfulness, Privacy; Technical | Managed/browser memory cannot promise deterministic secret erasure | E012-E016; design validation | IVSD-M011 | Security |
| IVSD-F012 | accepted | Critical | Observability boundary | Avoiding Spying, Amanah; Technical/Operational | Telemetry, analytics, logs, crash capture, or update checks can violate the local-only promise | E004-E005, E012-E014; implementation traceability | IVSD-M012 | Security + Operations |
| IVSD-F013 | accepted | Blocker | Dependency-license requirement | Amanah, Promise-Keeping; Governance | “FOSS” does not prove a dependency can satisfy the public AGPL build, reciprocal obligations, and every intended alternative outbound path | E008-E009, E017, E027-E028; documentation and policy evidence | IVSD-M013 | IP/Legal + Release |
| IVSD-F014 | accepted | High | Distribution integrity | Amanah, Ihsan; Operational | Unsigned or unverifiable desktop/web artifacts expose secret-handling users to supply-chain substitution | E009-E011, E017; design validation | IVSD-M014 | Release + Security |
| IVSD-F015 | accepted | High | Platform-support requirement | Justice, Ihsan; Design/Operational | “Cross-platform” is misleading without real per-OS packaging, launch, file, accessibility, and upgrade evidence | E010-E011, E018-E020; official framework evidence | IVSD-M015 | Release + QA |
| IVSD-F016 | accepted | High | Hosting-governance concern | Truthfulness, Justice; Strategic/Governance | Ignoring web source cannot prevent malicious rebuilds or third-party hosting and can weaken public auditability | E009, E017, E021; legal/governance evidence | IVSD-M016 | Project Steward + Legal |
| IVSD-F017 | accepted | Critical | Official-instance identity | Truthfulness, Amanah; Governance/Design | Users can confuse an unofficial fork with the official secret-capable service | E021; design validation | IVSD-M017 | Legal + Product |
| IVSD-F018 | accepted | High | Accessibility requirement | Justice, Ihsan; Design/Evaluation | Complex forms, masked values, review, and file generation can exclude disabled users | E022; implementation traceability | IVSD-M018 | Accessibility + UI |
| IVSD-F019 | accepted | High | Localization requirement | Justice, Ihsan; Design | Deployment and security explanations can be misunderstood without localization and RTL | E022-E023; implementation traceability | IVSD-M019 | Localization + UI |
| IVSD-F020 | accepted | Blocker | Live-secret authority boundary | Amanah, Avoiding Spying; Technical | A convenience tool can drift into extracting secrets from live instances or Infisical | E003-E007; implementation traceability | IVSD-M020 | Security + Secrets |
| IVSD-F021 | accepted | High | Claim-governance requirement | Truthfulness, Avoiding Gharar; Strategic/Governance | “Fully safe” or “we cannot get secrets” overstates what hosted or local software can prove | E012-E014; design validation | IVSD-M021 | Legal + Product |
| IVSD-F022 | accepted | High | Evidence requirement | Ihsan, Amanah; Evaluation | Passing unit tests cannot prove zero disclosure, package integrity, usability, or long-term safety | E009-E022; implementation traceability only | IVSD-M022 | QA + Security + Operations |
| IVSD-F023 | accepted | Critical | Legal-authority requirement | Truthfulness, Justice; Governance/Technical | Portable legal text can misattribute instance, tenant, organizer, or merchant responsibility if scope is generic | E007, E024-E026; implementation traceability | IVSD-M023 | Legal + Architecture |
| IVSD-F024 | accepted | Critical | Legal-lifecycle requirement | Amanah, Rights of People; Governance/Technical | Importing legal text can overwrite published history or fabricate prior user acceptance | E024-E026; implementation traceability | IVSD-M024 | Legal + Domain |
| IVSD-F025 | accepted | High | Template-governance concern | Truthfulness, Avoiding Gharar; Design/Governance | A legal template can be mistaken for legal advice or jurisdictional compliance | User direction, E009; design validation | IVSD-M025 | Legal + Product |
| IVSD-F026 | accepted | Critical | Markdown-content boundary | Non-Harm, Amanah; Technical/Design | Unrestricted Markdown/HTML can introduce scripts, tracking, deceptive links, inaccessible output, or remote resource loads | E012-E014, E022; standards and implementation traceability | IVSD-M026 | Security + Accessibility |
| IVSD-F027 | accepted | High | Portability-completeness requirement | Promise-Keeping, Ihsan; Strategic/Technical | Legal links without portable source text leave migrations incomplete and dependent on old origins | E007, E024-E026; implementation traceability | IVSD-M027 | Product + Architecture |
| IVSD-F028 | accepted | High | Content-scale requirement | Amanah, Ihsan; Technical/Operational | Legal Markdown and localized variants can exceed current manifest string/file limits or make diffs unusable | E007; implementation traceability | IVSD-M028 | Architecture + UX |
| IVSD-F029 | accepted | High | Access/parity requirement | Justice, Ihsan; Design/Technical | Desktop/web-only operation excludes terminal-first, remote-shell, and automation users | E029-E030; official functional evidence | IVSD-M029 | Product + CLI |
| IVSD-F030 | accepted | Critical | Automation-contract requirement | Truthfulness, Amanah; Technical | Agents and scripts cannot safely automate an interactive TUI or unstable prose output | E031-E033; repository implementation traceability | IVSD-M030 | CLI + Tooling |
| IVSD-F031 | accepted | Blocker | Terminal secret boundary | Amanah, Avoiding Spying; Technical/Operational | Arguments, shell history, scrollback, process listings, pipes, logs, and stdout can expose secrets | Design validation | IVSD-M031 | CLI + Security |
| IVSD-F032 | accepted | Critical | Agent-safety requirement | Amanah, Non-Harm; Governance/Technical | An agentic skill can encourage agents to read, transmit, infer, or persist secrets unless explicitly prohibited | E032-E033; skill-contract evidence | IVSD-M032 | Agent Governance + Security |
| IVSD-F033 | accepted | High | AI-boundary requirement | Truthfulness, Avoiding Gharar; Strategic/Technical | Embedded AI would add providers, data flows, cost, nondeterminism, and privacy duties unrelated to deterministic setup | User decision; design validation | IVSD-M033 | Product |
| IVSD-F034 | accepted | High | Human-approval requirement | Autonomy, Justice; Design/Governance | Agent-generated configuration can silently broaden policy or publish legal text without informed review | E007, E032-E033; design validation | IVSD-M034 | Agent Governance + Product |
| IVSD-F035 | accepted | High | TUI accessibility limitation | Justice, Truthfulness; Design/Evaluation | Terminal.Gui functional parity does not prove screen-reader, RTL, color, Unicode, or small-terminal parity | E029-E030; official functional evidence only | IVSD-M035 | Accessibility + CLI |
| IVSD-F036 | accepted | Critical | Skill-lifecycle requirement | Promise-Keeping, Truthfulness; Governance | Publishing a skill before versioned commands exist teaches fictional or stale behavior | E032-E033; skill-contract evidence | IVSD-M036 | Skill owner + CLI owner |

### IVSD-F001 — The Setup Assistant Advances Credible Self-Hosting

The product is justified because it reduces the expertise required to produce
two difficult artifacts without weakening their boundaries:

- non-secret portable configuration;
- deployment-local environment configuration, which may contain secrets.

It supports small communities that do not operate Infisical while preserving
advanced self-hosters’ ability to use the same schemas, validation, and
deployment profiles. It also gives contributors one visible contract for
configuration coverage and missing documentation.

### IVSD-F002 — Shared Rules Must Be Headless

The Avalonia application must not reference `Explore.Application`,
`Explore.Infrastructure`, persistence, MediatR, EF Core, or provider SDKs. A
small pure library should own:

- manifest/package contracts;
- strict lexical reading and static validation;
- deterministic serialization;
- environment-variable metadata and activation predicates;
- dotenv parsing and rendering;
- safe value-format validation;
- generated-output diagnostics.

Server runtime validation remains authoritative for current instance state,
tenant authority, locks, policy ceilings, reference mapping, and transactional
import. Offline static validation must never claim to prove those runtime facts.

### IVSD-F003 — Hosted Web Secret Entry Always Trusts The Origin

Avalonia Browser can execute all product logic in WebAssembly without
server-side application code. This does not mean the host is unable to receive
secrets. The host supplies the executable client on every uncached load and can
change it.

The optional official web secret mode is acceptable only as an explicit
trust-based convenience:

- default remains no-secret mode;
- users must affirm that they trust the displayed official origin and release;
- the page states that source and build evidence apply to an identified
  release, not to every possible future response;
- desktop remains the recommended path for higher-assurance secret entry;
- an offline, checksum-verifiable web bundle may be offered as another path.

### IVSD-F004 — No-Secret Mode Is The Protective Default

Every new browser session starts in no-secret mode. A remembered preference
must not silently reopen secret mode. The no-secret path remains fully useful:

- asks topology and feature questions;
- asks non-secret values;
- identifies relevant secret variables;
- emits those relevant secret keys with empty values;
- reports that secret completion remains;
- omits unrelated variables and features;
- relies on documented runtime defaults where absence is intentional.

### IVSD-F005 — Secret Mode Requires A Browser Network-Denial Contract

“We do not call our API” is too narrow. Browser code can communicate through
fetch, XMLHttpRequest, WebSocket, EventSource, beacon, forms, images, media,
fonts, frames, workers, navigation, dynamic scripts, and future APIs.

The reviewed secret-capable web build must:

1. load all required local assets before secret entry;
2. use no third-party scripts, fonts, images, analytics, tags, or CDNs;
3. deny scripted connections;
4. deny forms, embedding, objects, remote images/media/fonts, and unapproved
   workers;
5. avoid CSP violation reporting because reporting itself sends a request;
6. contain no lazy localization, documentation, update, or feature fetch after
   secret entry;
7. prevent external navigation while secret values remain;
8. prove zero requests after the secret-mode transition in supported browsers.

CSP is defense in depth, not proof against a malicious origin, compromised
browser, extension, or user override.

### IVSD-F006 — Browser-Local Is Not The Same As Ephemeral Or Secret

Secret values must never enter:

- URL, query, fragment, history, title, referrer, or route state;
- localStorage, sessionStorage, IndexedDB, Cache API, service-worker state, or
  browser-managed application state;
- cookies;
- browser logs, console, diagnostics, exceptions, or source maps;
- DOM attributes, hidden fields, accessibility labels, validation messages, or
  clipboard unless the user explicitly asks to copy;
- telemetry or CSP reports.

Masked input does not protect against browser extensions, password managers,
screen capture, accessibility tooling, compromised operating systems, or
developer tools. The UI must disclose this without frightening or shaming the
user.

### IVSD-F007 — Empty Relevant Placeholders Preserve Utility

In no-secret mode:

- selected required secret variables render as `KEY=`;
- each is marked as required before startup through adjacent comments and the
  readiness report;
- optional secrets for selected optional features render only when that
  feature requires them;
- secrets for unselected features do not appear;
- fake example secrets and insecure defaults are forbidden;
- generated cryptographic values are not produced unless the user explicitly
  enables secret mode or uses the desktop generator.

An empty secret placeholder is not a valid deployment. The final review must
state `Incomplete: secret values still required` and name the keys without
inventing values.

### IVSD-F008 — Environment Metadata Needs One Canonical Catalogue

The Setup Assistant must not parse human prose in `.env.example` as product
logic. Introduce a pure canonical catalogue with metadata such as:

- key;
- category and description resource key;
- value type and safe validation;
- secret classification;
- required/optional/defaulted status;
- declarative `RequiredWhen` conditions;
- deployment topology/profile;
- provider/capability dependency;
- generation policy;
- restart requirement;
- documentation anchor;
- example policy;
- scope and output format.

The catalogue contains no secret values. It should generate or validate:

- `.env.example`;
- Setup Assistant form metadata;
- startup configuration coverage;
- configuration documentation;
- Compose-variable coverage;
- CI drift checks.

`SecretDefinitionRegistry` remains authoritative for secret-binding semantics,
but the planned architecture must resolve its current Domain/Application
coupling without copying registry data into the UI.

### IVSD-F009 — Manifests And Dotenv Files Must Stay Separate

`ConfigurationManifest` and `TenantConfigurationPackage` remain non-secret and
shareable according to their authority. `.env` is deployment-local and may be
secret-bearing.

The product may generate both into one user-selected directory, but it must
never:

- embed `.env` values in a manifest;
- create one combined JSON or ZIP by default;
- label the files as having equivalent sensitivity;
- attach `.env` to an import/export package;
- upload `.env` to an instance;
- treat `.env` as tenant configuration.

### IVSD-F010 — Desktop Writing Must Fail Safely

Desktop builds can provide stronger file guarantees than browsers:

- native save picker;
- same-directory temporary file;
- owner-only mode established before or immediately with creation;
- atomic replacement only after a redacted review;
- symlink/reparse-point and unexpected file-type refusal;
- explicit overwrite confirmation;
- no automatic plaintext backup;
- post-write permission verification;
- safe failure that does not leave a partial file.

On Unix-like systems the target is owner read/write only. On Windows the target
is an ACL limited to the current user and required system authority. If the
filesystem cannot represent the requested protection, default behavior is to
refuse and explain; an advanced override must be explicit and cannot be called
safe.

Browser downloads cannot reliably impose equivalent filesystem permissions.
The secret-capable web mode must state that limitation before download.

### IVSD-F011 — Secret Lifetime Can Be Reduced, Not Proven Erased

.NET strings, Avalonia bindings, WebAssembly memory, browser DOM state, and OS
buffers can copy secret values. The product should:

- minimize copies and conversions;
- avoid immutable secret-containing display strings where practical;
- clear view models and rendered values immediately after generation,
  cancellation, navigation, or idle expiry;
- dispose buffers where supported;
- never retain secret state for reopening;
- not claim deterministic secure erasure.

### IVSD-F012 — Secret Workflows Must Be Observability-Free

Production builds must have:

- no product analytics;
- no remote telemetry;
- no automatic crash upload;
- no session replay;
- no remote logging;
- no CSP reporting endpoint;
- no update check during or after a secret session;
- no developer tools package;
- no source maps containing application secrets or user values;
- only local bounded diagnostics that never include entered values.

An optional user-created support report may contain build identity, platform,
selected feature keys, and closed error codes. It must not contain values,
paths that reveal usernames without consent, clipboard contents, environment
contents, or raw exceptions.

### IVSD-F013 — FOSS Philosophy Does Not Remove License Compatibility

The user revised the policy to permit free/open-source licenses, including
reciprocal GPL/AGPL families, while rejecting commercial, proprietary, and
source-available dependencies. This is coherent with open-source stewardship,
but legal compatibility remains target-specific.

Apply three boundaries:

1. `Event.Setup.Core`, because it can be shared with the main server and every
   UI, must preserve every intended ISLAMU outbound path. A reciprocal
   dependency that prevents alternative licensing is blocked there.
2. Public Setup Assistant executables may be explicitly AGPL-only when a
   reciprocal dependency is compatible with the assembled public work and the
   Project Steward documents that the target is excluded from alternative
   licensing.
3. A separate executable invoked through a bounded process protocol may have
   different obligations, but separation must be legally and technically real
   rather than a wrapper around intimately linked functionality.

No target may include:

- commercial-license runtime packages;
- proprietary or source-available components;
- field-of-use, seat, hosting, or noncommercial restrictions;
- unknown/unverified licenses;
- a package whose source, notice, installation-information, relinking, or
  network-source obligations cannot be satisfied.

Avalonia and Terminal.Gui each identify their primary framework/package as MIT.
Their exact pinned transitive/native graphs still require lock, SBOM, artifact,
and human review.

### IVSD-F014 — Release Identity Protects Secret-Handling Users

Every artifact should bind:

- product version;
- Git commit;
- target RID/format;
- package-lock digest;
- SBOM digest;
- build manifest digest;
- signing identity;
- checksum file;
- source URL;
- reproducibility status.

Desktop artifacts require platform signing where available. macOS release
requires signing and notarization. Windows release requires Authenticode or the
selected signed package mechanism. Linux packages and portable archives require
detached signatures plus checksums and repository-key documentation.

The official web page must display the release identity and source link before
the user can enter secrets.

### IVSD-F015 — Cross-Platform Is An Evidence Claim

The support matrix must distinguish:

- source compiles;
- application launches;
- file picker works;
- dotenv can be saved;
- permissions are enforced;
- screen reader and keyboard work;
- package installs and uninstalls;
- upgrade preserves no secret drafts;
- signed artifact verifies;
- supported architecture.

Native AOT may improve startup and reduce runtime surface, but is an
optimization after functional and accessibility parity. It must not be used to
justify weaker test coverage or unverifiable native dependencies.

### IVSD-F016 — Gitignore Is Not An Anti-Malicious-Hosting Control

Two proposals must be separated:

1. **Ignore generated web publish output.** Recommended. `bin/`, `obj/`, and
   release artifacts should remain generated, signed, and retained by the
   release system rather than committed.
2. **Ignore or withhold the web target source so only ISLAMU can host it.**
   Rejected as a security control.

Withholding source:

- does not prevent someone from building a similar malicious page;
- does not prevent phishing on another domain;
- reduces public auditability and reproducible-build evidence;
- conflicts with the project’s open-source trust and may create AGPL source
  obligations for the official network service;
- creates false confidence that exclusivity prevents impersonation.

The user accepted the corrected boundary: track and publish the browser source;
ignore only generated `wwwroot`, build, publish, and release artifacts.

### IVSD-F017 — Official Hosting Needs Truthful Identity, Not Technical Monopoly

Realistic controls are:

- one documented official HTTPS origin;
- visible instance/operator legal identity;
- source and exact release links;
- signed release manifest and checksums;
- reproducible-build evidence;
- immutable content-addressed assets;
- strict CSP and security headers;
- no third-party resources;
- public security contact and incident process;
- trademark and brand-use policy;
- explicit warning that unofficial forks and lookalike domains are not
  ISLAMU-operated;
- optionally a downloadable offline bundle for independent verification.

Open-source users remain free to self-host compliant builds under the
repository license. Trademark and truthful attribution—not hidden source—are
the appropriate way to distinguish official operation.

### IVSD-F018 — Secret Forms Must Remain Accessible

Masked controls require real labels, description and error association,
keyboard reveal controls, and non-color indicators. The workflow must support:

- one logical heading structure;
- skip navigation;
- complete keyboard operation;
- visible focus;
- screen-reader mode/status announcements;
- no forced timeout without warning and extension;
- accessible review of key presence without announcing secret values;
- responsive reflow;
- high contrast and reduced motion;
- platform screen-reader testing.

### IVSD-F019 — Security Instructions Need Localization And RTL

The UI must localize:

- secret/no-secret mode choice;
- trust disclosure;
- relevant/omitted/defaulted explanations;
- validation and incomplete readiness;
- file-permission warning;
- official/unofficial host identity;
- recovery and support guidance.

RTL uses logical layout. Translation resources are bundled before secret mode;
secret entry must not trigger a TMS request.

### IVSD-F020 — The App Generates Secrets; It Does Not Retrieve Them

Initial releases must not:

- connect to Infisical;
- retrieve existing server environment variables;
- query container process environments;
- call instance endpoints for credentials;
- import browser password-manager secrets automatically;
- test credentials against providers;
- transfer `.env` between instances.

Live authorized manifest export/import may be added later because manifests are
non-secret. Any live secret-provider integration is a separate Tier 1 security
workstream and I-VSD refresh.

### IVSD-F021 — Legal Transparency Does Not Change Technical Capability

Terms, privacy notices, public source, and organizational commitments are
important accountability controls. They cannot support the absolute claim that
the provider is technically unable to obtain a secret entered into code the
provider serves.

Approved copy should say what the identified build does, what evidence exists,
what trust remains, what is stored, what is transmitted, and which path offers
stronger assurance.

### IVSD-F022 — Zero Disclosure Needs Adversarial Evidence

The release must be tested as if a developer accidentally added:

- analytics;
- remote fonts;
- image beacons;
- exception upload;
- CSP reporting;
- lazy translation;
- update checks;
- form submission;
- service-worker synchronization;
- secret-bearing logs;
- browser storage;
- unsafe file backup;
- incorrect permissions;
- dependency with an unapproved license.

The security promise is release evidence, not developer intention.

### IVSD-F023 — Legal Text Requires Explicit Role Authority

Instance and tenant legal texts should be portable, but not through a generic
key/value document bag.

The contract must preserve distinct accountable authors:

- instance/platform operator;
- tenant/directory operator;
- organizer or merchant where an event-specific contract applies.

An instance document cannot silently become the tenant’s statement. A tenant
document cannot replace the instance operator’s terms, privacy disclosure,
security notice, or platform responsibilities. Single-tenant deployment may
have one organization filling multiple roles, but the stored scopes and public
labels remain separate.

### IVSD-F024 — Legal Configuration Must Not Rewrite Evidence

Portable configuration may contain:

- legal-document drafts;
- current source Markdown;
- localized variants;
- publication intent;
- template provenance;
- proposed effective date;
- acceptance requirement.

It must not contain or rewrite:

- historical acceptance records;
- historical published versions;
- user/account acceptance timestamps;
- consent evidence;
- notification delivery evidence;
- old operator identities;
- legal-hold or dispute state.

Importing published-looking content on a target creates a new target-owned
version after explicit review. It never asserts that users accepted the source
instance’s version.

### IVSD-F025 — Templates Are Starting Points, Not Legal Approval

The Setup Assistant can materially improve quality with structured templates,
but every template must show:

- template identity and version;
- scope and intended operator role;
- language and jurisdiction assumptions;
- required placeholders;
- missing sections;
- provenance and license;
- date of legal review, when one exists;
- a prominent statement that local counsel and operator review remain
  necessary.

No template may be marketed as automatically compliant, universally valid, or
Islamically approved.

### IVSD-F026 — Markdown Must Be A Safe Typed Content Format

The editor should support a constrained Markdown profile rather than arbitrary
HTML:

- headings, paragraphs, emphasis, ordered/unordered lists, block quotes,
  tables, and safe links;
- no raw HTML;
- no script, style, iframe, object, embed, form, SVG, or executable content;
- no remote images, tracking pixels, data URLs, protocol-relative URLs, or
  automatic resource fetch;
- allowlisted link schemes and visible destination review;
- deterministic parsing, sanitization, normalization, and rendering;
- accessible heading and link validation;
- bounded document/locale/package size.

The same parser and sanitizer must be shared by editor preview, server
validation, public rendering, export, and import.

### IVSD-F027 — Legal Source Text Belongs In Portability

Exporting only `TermsUrl` and `PrivacyUrl` can leave a migrated tenant
dependent on the old instance’s domain. Portable configuration should include
the approved Markdown source and metadata for owned legal documents.

Target import must:

- rebind instance/tenant identity placeholders;
- identify links that still point to the source origin;
- require review of jurisdiction, contact, processor, payment, and complaint
  claims;
- preserve source/template provenance;
- create a target draft or newly reviewed version;
- never auto-publish silently.

### IVSD-F028 — Legal Content Changes Contract Limits And UX

Current manifest limits were designed for compact configuration documents.
Multiple legal-document kinds and localized Markdown can be substantially
larger.

The clean next contract must define:

- maximum documents per scope;
- maximum locales per kind;
- maximum Markdown bytes per document;
- maximum placeholder and link counts;
- maximum aggregate package size;
- streaming/bounded parsing;
- deterministic diff summaries;
- optional section-selective legal export.

Limits should be justified by realistic legal content and denial-of-service
protection, not inherited unchanged from the compact v1alpha1 contract.

### IVSD-F029 — CLI/TUI Is A First-Class Product Target

Add `Event.SetupAssistant.Cli` as a shipped executable with two surfaces:

- deterministic subcommands for humans, scripts, CI, and external agents;
- a Terminal.Gui TUI for terminal-first interactive use.

“Same experience” means the same use cases, core rules, diagnostics, previews,
and generated bytes. It does not mean identical visual composition or an
unsupported claim of accessibility parity.

### IVSD-F030 — Agents Need Machine Contracts, Not TUI Automation

Terminal full-screen state is fragile for automation. Agents should use
versioned noninteractive commands with:

- stable command names and exit categories;
- versioned JSON output;
- diagnostic codes instead of prose parsing;
- explicit input/output paths;
- dry-run and no-secret defaults;
- artifact digests and coverage/readiness summaries;
- no ANSI control sequences in machine mode.

The TUI can teach and assist humans. The future skill may explain how to open
it, but must direct agents to the command surface.

### IVSD-F031 — Terminal Secret Entry Has Distinct Leakage Paths

Secret values must never be accepted through:

- command-line arguments;
- process environment used as value transport;
- shell interpolation;
- filenames;
- standard output/error;
- JSON output;
- shell completion;
- terminal scrollback;
- command history.

Initial CLI secret entry should be interactive TTY-only through masked,
non-echoing fields, and secret-bearing output must go directly to a protected
file. Noninteractive/agent mode defaults to placeholders and rejects secret
values.

### IVSD-F032 — The Skill Must Protect Secrets From The Agent

The skill should teach an agent to:

- inspect catalogue metadata, never secret values;
- use no-secret, dry-run, and machine-output modes;
- never read an existing `.env`;
- never ask the user to paste a secret into chat;
- never pass secrets through tool arguments, captured stdin, logs, or reports;
- hand secret completion to the user’s local desktop/TUI session;
- obtain approval for semantic diffs before writing;
- treat legal templates as drafts requiring counsel/operator review.

The skill is guidance, not an authorization or secret boundary. The CLI must
enforce every rule independently.

### IVSD-F033 — AI Remains Outside The Product

The Setup Assistant contains no:

- model SDK;
- AI provider;
- prompt runtime;
- chat UI;
- natural-language command parser;
- autonomous agent loop;
- remote inference;
- model telemetry;
- AI-specific secret.

Users may choose any external agent that can invoke the deterministic CLI.
This preserves local/offline operation and avoids forcing one AI vendor or data
flow onto users who do not want AI.

### IVSD-F034 — Agent Output Requires Human Approval

An agent may propose or generate:

- manifest/package drafts;
- relevant-only no-secret `.env`;
- legal-document drafts from approved templates;
- semantic diffs;
- validation and coverage reports.

An agent must not autonomously:

- enter or generate live provider credentials;
- read/write a completed secret-bearing `.env`;
- publish legal documents;
- assert counsel approval;
- apply to a live instance;
- broaden payment/security/privacy authority;
- erase or replace configuration without explicit approval.

### IVSD-F035 — TUI Parity Has Real Accessibility Limits

Terminal.Gui supports keyboard, mouse, Unicode, editors, and cross-platform
terminals, but actual behavior depends on terminal emulator, shell, font,
color, width, locale, and assistive technology.

The product must publish a separate TUI support matrix and preserve web/desktop
alternatives. A narrow or inaccessible terminal must not be the only path for a
required operation.

### IVSD-F036 — Publish The Skill After The CLI Contract

The planned path is:

```text
.agents/skills/setup-assistant-cli/
```

Do not create an operational skill that names commands until:

- command names and JSON schemas are implemented;
- help and exit categories are tested;
- secret/no-secret behavior is enforced;
- examples run against the shipped version;
- the skill can declare its compatible CLI version range.

An early planning draft may describe principles, but it must not masquerade as
usable operational guidance.

## Recommendations

### Decisive Product Direction

Create one product named **ISLAMU Event Setup Assistant** with five projects:

```text
src/Event.Setup.Core/
src/Event.SetupAssistant/
src/Event.SetupAssistant.Desktop/
src/Event.SetupAssistant.Browser/
src/Event.SetupAssistant.Cli/
```

- `Event.Setup.Core` is pure, deterministic, headless, trim/AOT-friendly, and
  contains no network, persistence, provider SDK, UI, or secret storage.
- `Event.SetupAssistant` contains shared Avalonia views, view models, resource
  keys, and platform-neutral workflows.
- `Event.SetupAssistant.Desktop` owns desktop startup, native file selection,
  permissions, signing metadata, and OS capability adapters.
- `Event.SetupAssistant.Browser` owns static WebAssembly startup, local
  download, browser capability declarations, CSP compatibility, and the
  no-network secret-mode boundary.
- `Event.SetupAssistant.Cli` owns stable commands, versioned machine output,
  terminal capability detection, and the Terminal.Gui human TUI.

Avalonia views and Terminal.Gui views adapt the same workflow/application
contracts; neither owns validation or rendering truth. Tests should mirror each
project. Release and packaging implementation belongs under `eng/`, not
`.ci/scripts/`; CI discovery and release adapters remain under the
repository’s established CI/CD paths.

### Product Workspaces

#### Configuration Portability Workspace

- create, open, edit, validate, normalize, and export
  `ConfigurationManifest`;
- create, open, edit, validate, and export
  `TenantConfigurationPackage`;
- section tree, typed fields, deterministic JSON preview, diff, coverage
  ledger, and documentation links;
- no secret fields or secret-reference values;
- offline static validation only;
- future live manifest operations through authorized APIs and HAL, never by
  embedding access tokens in the client.

#### Environment Setup Workspace

- choose deployment topology;
- choose database and infrastructure providers;
- choose optional capabilities;
- ask only relevant non-secret variables;
- classify secret requirements;
- select no-secret or secret-entry mode;
- render a deterministic, relevant-only `.env`;
- show redacted readiness and omissions;
- save/download locally.

The two workspaces may share a non-secret setup profile, but no secret value.

#### Legal Documents Workspace

- choose instance or tenant authority;
- choose document kind, audience, language, jurisdiction, and lifecycle;
- start from a governed template or blank source;
- edit constrained Markdown with outline, preview, and accessibility checks;
- resolve typed identity/contact placeholders;
- compare locales, templates, source, and target;
- validate links without making network requests during secret mode;
- export legal source and metadata into the correct manifest/package section;
- generate a publication/readiness checklist;
- never publish, obtain acceptance, or provide legal approval from the offline
  editor.

#### CLI And TUI Workspace

- `event-setup tui` opens the Terminal.Gui experience for humans;
- direct commands expose every static validation, render, diff, catalogue,
  legal-template, and readiness workflow;
- TUI navigation, validation, and generated bytes use the same core as
  Avalonia;
- machine mode never emits terminal control sequences;
- no-secret operation is the default;
- TUI secret entry is local, interactive, masked, and never echoed;
- agents and scripts use commands, not full-screen TUI automation.

### Target Architecture

| Target | Shared UI/core | Secret mode | File behavior | Network |
|---|---|---|---|---|
| Browser default | Yes | Disabled | Local download or supported picker | Static asset load only |
| Browser optional | Yes | Explicit opt-in each session | Local download; permissions not enforceable | No request after secret mode begins |
| Windows desktop | Yes | Available | Native picker, user-only ACL, atomic write | None by default |
| Linux desktop | Yes | Available | Native picker, owner-only mode, atomic write | None by default |
| macOS desktop | Yes | Available | Native picker, owner-only mode, sandbox-aware access | None by default |
| CLI machine mode | Core only | No-secret only initially | Explicit path or stdout for non-secret artifacts | None |
| Terminal.Gui TUI | Core plus terminal adapter | Interactive TTY only | Protected file output | None |

### Functional Parity Contract

| Capability | Avalonia web/desktop | Terminal.Gui TUI | CLI machine mode |
|---|---:|---:|---:|
| Browse/explain catalogue | Yes | Yes | Yes |
| Manifest/package create and validate | Yes | Yes | Yes |
| Deterministic format/render | Yes | Yes | Yes |
| Semantic diff and coverage | Yes | Yes | Yes |
| Relevant-only no-secret dotenv | Yes | Yes | Yes |
| Interactive secret completion | Optional web/Desktop | TTY only | No |
| Legal template selection | Yes | Yes | Yes |
| Markdown editing | Rich editor | Terminal editor | File-based validate/render |
| Accessibility/RTL evidence | Per target | Separate terminal matrix | Machine output |
| Agent automation | No UI automation | No TUI automation | Versioned JSON commands |

Parity is asserted only when the same core inputs produce byte-identical
artifacts and equivalent closed diagnostics.

### Versioned Command And Machine-Output Contract

Recommended command families:

```text
event-setup tui
event-setup catalog list|explain
event-setup manifest new|validate|format|diff|coverage|export
event-setup tenant-package new|validate|format|diff|coverage|export
event-setup env plan|render|validate|explain
event-setup legal template-list|new|validate|render|diff
event-setup doctor
```

Planning owns final names, but once shipped they require a versioning policy.
Every command supports:

- help;
- deterministic noninteractive operation;
- explicit input/output paths;
- dry-run where a write is possible;
- text output for humans;
- one JSON object for machine mode;
- stable exit categories;
- bounded diagnostics;
- artifact digest and schema version;
- no secret values.

Machine output should contain:

```text
schemaVersion
command
status
diagnostics[] { code, severity, path }
artifacts[] { kind, path, digest, sensitivity }
coverage
readiness
```

It must not contain localized prose as authority, raw exceptions, terminal
escapes, stack traces, or entered values.

### Terminal Secret-Safety Contract

The CLI rejects secret values supplied through arguments, option values,
process environment, or captured standard input. Initial supported paths are:

- no-secret machine commands;
- interactive Terminal.Gui/TTY secret entry;
- direct protected file output.

Secret TUI requirements:

- real TTY required;
- masked, non-echoing controls;
- no value in terminal title, status bar, accessible label, scrollback, or
  clipboard by default;
- no stdout/stderr secret output;
- no `--output -` when secret mode is active;
- no persistent TUI history or autosave;
- clear state on cancel, completion, suspension, terminal resize failure, and
  process signal where safely possible;
- explicit warning that terminal recorders, multiplexers, extensions,
  accessibility tools, and compromised local systems remain trust boundaries.

### Setup Assistant Agentic Skill

After the command contract is implemented, create:

```text
.agents/skills/setup-assistant-cli/SKILL.md
.agents/skills/setup-assistant-cli/resources/command-contract.md
.agents/skills/setup-assistant-cli/resources/secret-safety.md
.agents/skills/setup-assistant-cli/resources/tui-guide.md
.agents/skills/setup-assistant-cli/resources/workflows.md
```

The skill should load for generating, validating, diffing, or explaining
manifests, tenant packages, relevant-only dotenv files, and legal-document
bundles through the Setup Assistant CLI. It should exclude implementation of
the CLI itself and any request to ingest secret values.

The TUI guide may teach a user how to navigate the human interface, explain
screens, and select safe modes. Agent execution still uses machine commands
rather than terminal-screen automation.

The skill workflow should:

1. verify CLI/version compatibility;
2. select no-secret and dry-run behavior;
3. request machine JSON;
4. validate source artifacts;
5. generate a draft or diff;
6. summarize diagnostics without values;
7. obtain explicit approval before non-secret file writes;
8. validate the final artifact and digest;
9. hand secret completion to the local human UI.

### No Embedded AI Boundary

The product exposes deterministic commands and schemas only. It does not
embed, recommend, proxy, or configure an AI provider. The external agent owns
its model and invocation environment; the Setup Assistant remains useful and
complete without AI.

This separation prevents:

- an AI dependency in every self-hosted build;
- hidden prompt/data transmission;
- model-provider lock-in;
- AI API keys in `.env`;
- nondeterministic validation;
- claims that agent suggestions are legally or operationally authoritative.

### Default No-Secret Web Mode

The start page should make no-secret mode the primary action:

```text
Generate without entering secrets (Recommended)
```

The workflow:

1. loads the complete static app;
2. asks topology and capability questions;
3. asks relevant non-secret values;
4. identifies required secret keys without requesting their values;
5. generates relevant empty placeholders;
6. omits unrelated keys and intentionally defaulted variables;
7. shows `Incomplete until required secret values are supplied`;
8. downloads the file locally;
9. persists no form data; explicit clear/page close ends the product session,
   while the documented browser/OS memory limitations remain.

This mode still requires the same no-telemetry and local-only design. It simply
reduces the sensitivity of entered data.

### Optional Web Secret Mode

Secret mode requires a separate trust interstitial every session:

- exact official origin;
- release version and digest;
- source link;
- statement that this release is designed to send no secret values;
- statement that the hosting origin remains trusted to deliver the code;
- browser-extension/local-device warning;
- desktop recommendation;
- acknowledgment that browser download permissions cannot be enforced;
- explicit `Continue with secret entry` action;
- equal, prominent return to no-secret mode.

Secret mode must not be activated by query string, local preference, browser
storage, deep link, or remembered choice.

After activation:

- all resources are already loaded;
- network is denied;
- documentation/update/source links require clearing secrets first;
- mode change clears all secret state;
- inactivity expiry warns and then clears state;
- download or cancellation clears state;
- browser back/forward does not restore values.

### Browser Network Denial Contract

The exact production CSP must be derived and tested against the pinned
Avalonia/.NET version. Its policy intent is:

- same-origin, content-addressed framework/app assets only;
- no connections;
- no form submission;
- no framing;
- no objects;
- no remote images, fonts, styles, media, or manifests;
- no inline event handlers;
- only the minimum WebAssembly execution permission required by the runtime;
- no reporting endpoint.

Required controls:

- CSP response header plus an early meta fallback where compatible;
- `frame-ancestors 'none'` as a response header;
- SRI/integrity evidence for supported static script/style entrypoints;
- HTTPS and HSTS on the official origin;
- no service worker in the initial secret-capable release;
- no PWA background sync;
- no CDN;
- no dynamic import from remote URLs;
- no third-party JavaScript;
- no external CSS, fonts, icons, or images;
- no analytics or consent manager;
- no browser error-reporting endpoint;
- automated browser network recording from secret-mode entry through clear.

Initial asset requests happen before secret entry and therefore are not “zero
requests for the page.” The exact promise is zero requests after secret mode
starts and zero secret value in any request at any time.

### Relevant-Only Dotenv Rendering

The output algorithm is:

1. Resolve selected deployment profile and capabilities.
2. Include required non-secret keys that have no safe implicit default.
3. Include user-overridden non-secret defaults.
4. Omit unchanged values intentionally supplied by runtime defaults.
5. Include required secret keys for selected capabilities.
6. In no-secret mode, render those secret values empty.
7. In secret mode, render entered or locally generated values.
8. Include optional keys only when the user selected the associated feature or
   explicitly chose advanced inclusion.
9. Sort deterministically by deployment phase, category, and canonical key.
10. Render a redacted coverage/readiness report separately.

The generated header must communicate this meaning:

```dotenv
# Generated by ISLAMU Event Setup Assistant.
# This file intentionally contains only variables relevant to the selected
# deployment and features. Supported variables not shown here use documented
# defaults or belong to features you did not select.
# See the canonical configuration documentation for the complete catalogue.
```

The prose may evolve and must not be pinned by tests. Tests should assert
machine-consumed classifications, key inclusion/omission, deterministic
ordering, and safe rendering.

Relevant empty secret example:

```dotenv
# Required before startup for the selected authentication profile.
KEYCLOAK_BLAZOR_CLIENT_SECRET=
```

No fake value such as `change-me`, `password`, or a copied example credential
is permitted.

### Dotenv Format Safety

The renderer must define one explicit dialect compatible with the supported
ISLAMU Event deployment command. It must test:

- empty values;
- leading/trailing whitespace;
- `#`;
- quotes;
- backslashes;
- dollar signs and Compose interpolation;
- multiline values;
- Unicode and normalization;
- CRLF/LF;
- duplicate keys;
- invalid key names;
- comments;
- values that resemble commands;
- round-trip parse/render behavior.

The tool generates data; it never executes a generated value or shells it into
a command.

### Legal Document Configuration Model

Use a first-class typed `LegalDocumentBundle` rather than arbitrary JSON. Each
entry should carry:

- stable document kind;
- owner scope (`Instance` or `Tenant`);
- language tag;
- audience;
- title and optional short summary;
- constrained Markdown source;
- content digest;
- lifecycle intent;
- effective date when proposed;
- whether fresh acceptance is required;
- accountable identity revision or manifest-local identity reference;
- template ID/version/provenance;
- jurisdiction assumptions;
- superseded source version reference when known;
- change summary;
- typed placeholders and completeness state.

Recommended lifecycle:

```text
Draft -> ReviewRequired -> Approved -> Scheduled -> Published -> Retired
```

The manifest/package expresses desired legal configuration. Canonical Domain
mutation creates immutable target versions and acceptance requirements.
Published/retired history remains persisted evidence outside portable
configuration.

### Legal Document Kind Catalogue

Candidate instance-owned kinds:

- platform terms of service;
- instance privacy notice;
- cookie notice/policy;
- acceptable-use policy;
- community/content rules;
- moderation, reporting, appeal, and correction policy;
- accessibility statement;
- legal notice/imprint;
- security and vulnerability disclosure;
- retention, erasure, and portability notice;
- subprocessors/service-provider disclosure;
- open-source/license and attribution notice;
- API/developer terms;
- federation/ATProto disclosure;
- platform payment-operation notice;
- platform fee/contribution notice;
- complaint, refund, dispute, and reconciliation responsibilities;
- service availability, support, EOL, and migration notice.

Candidate tenant-owned kinds:

- tenant/directory terms;
- tenant privacy/controller notice;
- tenant cookie additions;
- local code of conduct/community rules;
- organizer/event-submission terms;
- event publication and moderation policy;
- cancellation/refund baseline;
- registration/participant privacy notice;
- media/photography consent information;
- safeguarding/minor-participation policy;
- venue/accessibility information policy;
- complaint/correction/copyright contact policy;
- sponsorship/partner disclosure;
- local retention and contact-sharing notice.

The catalogue is closed and typed. Adding a kind requires an accountable owner,
public rendering location, scope, lifecycle, validation, portability,
acceptance, and legal-review decision.

### Instance And Tenant Composition

Public legal navigation should present role-labeled documents:

- `Platform operator`;
- `Directory operator`;
- `Organizer/Merchant` when applicable.

Tenant legal text is additive within its authority. It cannot hide required
instance documents. Instance governance may require specific tenant document
kinds or minimum disclosures, but should not silently write factual tenant
claims. Missing required documents remove affected HAL capabilities or make
activation/readiness fail closed with bounded repair guidance.

### Legal Template Library

Template packs must be:

- project-authored or independently licensed under an approved FOSS license
  compatible with the target distribution;
- bundled locally;
- versioned and immutable after release;
- source- and license-attributed;
- scoped by role, document kind, language, and jurisdiction assumptions;
- composed from typed placeholders;
- accompanied by completeness rules;
- reviewed for accessibility and plain language;
- clearly non-certifying.

External legal prose must not be copied into the repository merely because it
is publicly visible. New templates require clean-room provenance and qualified
legal review. A signed future template-pack update is a separate network and
supply-chain feature; the first release uses only bundled templates.

### Safe Markdown Editor

The editor should provide:

- source, structured outline, and sanitized preview;
- keyboard-complete formatting commands;
- heading-order and link-text diagnostics;
- typed placeholder insertion rather than freehand magic strings;
- unresolved-placeholder panel;
- locale comparison and missing-translation indicators;
- source/preview cursor coordination where accessible;
- word/byte/link/heading counts;
- deterministic formatter;
- change diff and summary;
- safe undo/redo contained in process memory;
- local file open/save;
- template reset without silent data loss;
- export-readiness and publication-readiness as distinct results.

It must not provide:

- raw HTML mode;
- embedded browser content;
- remote image preview;
- arbitrary plugins;
- executable macros;
- network spellcheck/grammar/legal review;
- AI-generated legal text without a separate approved workstream;
- auto-publication.

Any Markdown parser/editor dependency must independently pass the FOSS and
target-outbound compatibility gate.

### Legal Content Quality-Of-Life Improvements

- template comparison;
- clause outline and navigation;
- required-section checklist;
- operator/tenant identity placeholder binding;
- contact and jurisdiction consistency checks;
- source-origin link detector;
- broken relative-link detector;
- safe scheme validation;
- language and RTL preview;
- accessible plain-text export;
- Markdown/PDF/HTML publication preview, with PDF/HTML generation added only
  after dependency review;
- locale completeness dashboard;
- stale legal-review reminder;
- effective-date scheduler preview;
- acceptance-impact warning;
- changelog generation;
- previous-version diff;
- target-instance migration review;
- document-kind coverage ledger;
- publication and notification checklist;
- counsel-review status and evidence reference;
- public footer/navigation preview;
- machine-readable manifest/package export.

### Secret Generation

Only approved secret classes may be generated locally. Every generator
declares:

- required entropy;
- byte length;
- encoding;
- prefix/version when required;
- target variable;
- whether the value is accepted by the target system;
- rotation/recovery documentation.

Use platform cryptographic random APIs. Never use timestamps, GUIDs, ordinary
pseudorandom generators, human words, or one shared value for unrelated keys.
Provider-issued credentials remain provider-issued and are requested from the
user; the assistant does not fabricate or verify them online.

### Desktop File-Security Contract

Before writing:

- display exact target path;
- detect existing file, directory, symlink, reparse point, or special file;
- show a redacted key-level diff;
- require explicit overwrite;
- avoid following links;
- create in the destination directory;
- apply restrictive access before secret content is exposed where the platform
  permits;
- flush and atomically replace;
- verify final permissions and ownership;
- delete incomplete temporary output on failure;
- emit only a closed local error code.

The application does not automatically retain:

- plaintext backup;
- recent-file secret content;
- autosave draft;
- restore file;
- clipboard copy;
- crash attachment.

A non-secret profile may retain feature selections and non-secret values only
after explicit user choice.

### Desktop Distribution And Packaging

#### Windows

| Artifact | Architecture | Initial recommendation |
|---|---|---|
| Portable `.zip` | `win-x64`, `win-arm64` | Required |
| Signed installer/package | `win-x64`, `win-arm64` | Add after FOSS/tooling compatibility review |
| `win-x86` | x86 | Optional only if demand and framework support justify it |

Every executable/package is signed, checksum-published, and smoke-tested on a
clean supported Windows environment.

#### Linux

| Artifact | Architecture/distro role | Recommendation |
|---|---|---|
| `.tar.gz` | `linux-x64`, `linux-arm64` | Baseline portable release |
| `.deb` | Debian/Ubuntu families | Required |
| `.rpm` | Fedora/RHEL/openSUSE families | Required after packaging validation |
| `.pkg.tar.zst` or reviewed PKGBUILD | Arch/Manjaro families | Required for the requested Arch path |
| AppImage | Broad desktop convenience | Optional after complete license/tool review |
| Flatpak | Sandboxed desktop distribution | Optional after portal, permission, manifest, and license review |

Use portable non-version-specific RIDs. Package format does not replace testing
on representative distributions. Initial Linux backend should use the stable
supported path; experimental Wayland-only behavior is not the default.

#### macOS

| Artifact | Architecture | Recommendation |
|---|---|---|
| Signed/notarized `.app` in `.zip` | `osx-x64`, `osx-arm64` | Required |
| Signed/notarized `.dmg` | x64/arm64 or universal | Recommended after release pipeline is stable |

Use hardened runtime with only required entitlements, user-selected file
access, signing, notarization, and staple verification. No network entitlement
should be requested for an offline-only desktop release unless a later
approved feature needs it.

#### Browser

Publish a static, immutable, content-addressed bundle with:

- checksum manifest;
- SBOM;
- source revision;
- integrity metadata;
- CSP header configuration;
- deploy receipt;
- official origin identity;
- archived release bundle for independent verification.

#### CLI/TUI

Publish `event-setup` as:

- self-contained `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`,
  `osx-x64`, and `osx-arm64` executables;
- part of each desktop archive/package;
- an optional framework-dependent .NET global tool after package/license
  review;
- checksum/SBOM/signature-bound artifacts using the same release identity.

Shell-completion definitions may contain command/option names only. They must
never persist paths, recent values, generated keys, or secret-bearing history.

### FOSS Philosophy And Outbound Compatibility Gate

Before implementation selects a version:

1. resolve Avalonia and Terminal.Gui public FOSS packages only;
2. exclude Avalonia Pro, Parcel, licensed diagnostics, commercial tooling, and
   every proprietary/source-available dependency;
3. restore every target RID with lock files;
4. inspect direct, transitive, native, build, test, asset, font, icon,
   template, and packaging dependencies;
5. classify each license, linkage/distribution shape, obligations, and target;
6. determine whether the component preserves all outbound paths or makes one
   executable explicitly AGPL-only;
7. produce an SBOM and notices/source-offer evidence for each target;
8. run repository dependency-license validation;
9. have a human compare release artifacts to lock/SBOM evidence;
10. block unknown, commercial, proprietary, source-available, or
    obligation-incompatible components;
11. record OS-provided libraries and signing/notarization services separately.

GPLv3 and AGPLv3 components may be philosophically acceptable and can be
compatible with an AGPL public executable. They are not automatically
compatible with the Project Steward’s alternative outbound paths. Any such
dependency requires a target-specific legal decision and documented
distribution boundary.

### Official Web Trust And Provenance

The official service should expose:

- operator public/legal identity;
- official domain;
- privacy and security statement;
- release version, commit, and artifact digest;
- link to the exact source revision;
- reproducible-build status;
- date and scope of last independent security review;
- no-secret default;
- desktop/offline alternatives;
- vulnerability-reporting route;
- incident notice policy.

Marketing and UI must distinguish:

- **Designed local processing:** implementation property of an identified
  release;
- **Official hosting commitment:** organizational/legal promise;
- **Origin trust:** unavoidable online delivery trust;
- **Desktop/offline verification:** stronger user-controlled delivery path.

### Gitignore And Hosting Exclusivity Decision

Recommended:

```text
Track:
  src/Event.SetupAssistant.Browser/
  CSP and release configuration
  browser tests
  reproducibility metadata

Ignore:
  bin/
  obj/
  artifacts/
  generated publish/wwwroot output
```

Not recommended:

- gitignore browser source;
- private browser-only implementation;
- claim that hidden source prevents malicious hosting;
- special official binary with secret behavior absent from public source.

This is the approved direction. The security and licensing benefits of
auditable source outweigh the ineffective exclusivity claim.

### Accessibility And Localization

Both targets share:

- semantic names and descriptions;
- keyboard access;
- predictable focus;
- one error summary plus field errors;
- status announcements;
- non-color state;
- 200% text scaling and reflow;
- contrast and target-size requirements;
- reduced motion;
- LTR/RTL parity;
- bundled translations;
- plain-language and expert explanations.

Secret values must never be placed in accessible names, announcements, or
error text.

### Future Capabilities Requiring Separate Approval

- live instance manifest export/import;
- OAuth/device authorization;
- token persistence;
- Infisical read/write;
- provider credential tests;
- encrypted saved secret profiles;
- PWA/service worker;
- auto-update;
- runtime plugins or downloaded configuration packs;
- direct instance-to-instance transfer;
- application-data migration;
- mobile targets.

## Mitigation Register

| Mitigation | Requirement | Findings |
|---|---|---|
| IVSD-M001 | Ship a focused Setup Assistant with separate manifest and environment workspaces | F001 |
| IVSD-M002 | Extract pure shared contracts, catalogue, validators, and renderers; keep runtime authority server-side | F002 |
| IVSD-M003 | Disclose origin trust; identify exact release; recommend desktop for higher assurance | F003 |
| IVSD-M004 | Default every browser session to no-secret mode; require explicit per-session opt-in | F004 |
| IVSD-M005 | Load first, then enforce and test zero requests after secret-mode entry with strict CSP and no reporters | F005 |
| IVSD-M006 | Forbid browser persistence and minimize DOM/memory/clipboard exposure; disclose extension/device limits | F006 |
| IVSD-M007 | Render empty placeholders only for relevant selected secrets and mark readiness incomplete | F007 |
| IVSD-M008 | Introduce one non-secret canonical environment catalogue that generates/validates every consumer | F008 |
| IVSD-M009 | Keep manifests/packages and `.env` separate in data model, UX, files, and sensitivity labels | F009 |
| IVSD-M010 | Use native picker, link refusal, restrictive permissions, atomic write, verification, and no plaintext backup | F010 |
| IVSD-M011 | Minimize secret copies and lifetime; never promise deterministic memory erasure | F011 |
| IVSD-M012 | Remove telemetry, remote logs, crash upload, CSP reports, update calls, and production developer tools | F012 |
| IVSD-M013 | Permit only approved FOSS while proving per-target AGPL, reciprocal, and alternative-outbound compatibility | F013 |
| IVSD-M014 | Sign, attest, checksum, SBOM, archive, and identify every desktop/web release | F014 |
| IVSD-M015 | Publish an evidence-backed OS/architecture/package support matrix | F015 |
| IVSD-M016 | Track browser source and ignore generated output; reject source withholding as hosting protection | F016 |
| IVSD-M017 | Use official origin, legal identity, trademark, source/release provenance, and fork disclosure | F017 |
| IVSD-M018 | Meet WCAG 2.2 AA-aligned interaction and test with real platform assistive technologies | F018 |
| IVSD-M019 | Bundle localization/RTL resources before secret mode and localize all security consequences | F019 |
| IVSD-M020 | Generate or accept new local values only; never retrieve live instance/Infisical secrets | F020 |
| IVSD-M021 | Govern claims to identified behavior/evidence and state remaining trust explicitly | F021 |
| IVSD-M022 | Run adversarial browser, desktop, packaging, license, accessibility, and recovery evidence gates | F022 |
| IVSD-M023 | Add typed, role-scoped legal-document bundles for instance and tenant authority | F023 |
| IVSD-M024 | Separate portable drafts/current source from immutable publication and acceptance evidence | F024 |
| IVSD-M025 | Govern project-owned or approved FOSS templates as non-certifying, versioned starting points | F025 |
| IVSD-M026 | Use one constrained Markdown parser/sanitizer across editor, server, public rendering, and packages | F026 |
| IVSD-M027 | Export owned legal source/metadata and import it as target-reviewed drafts or new versions | F027 |
| IVSD-M028 | Re-baseline bounded contract limits and legal-content diff UX | F028 |
| IVSD-M029 | Ship Terminal.Gui TUI and deterministic CLI as first-class adapters over the shared core | F029 |
| IVSD-M030 | Provide versioned JSON, exit categories, help, dry-run, digests, and bounded diagnostics | F030 |
| IVSD-M031 | Forbid terminal/argument secret transport; allow secret completion only in protected interactive TTY flow | F031 |
| IVSD-M032 | Make the skill default to no-secret machine commands and prohibit agent access to secret-bearing files/values | F032 |
| IVSD-M033 | Keep model SDKs, providers, prompts, chat, inference, and agent loops outside the product | F033 |
| IVSD-M034 | Require human approval for writes, legal publication, live apply, and authority broadening | F034 |
| IVSD-M035 | Publish separate TUI accessibility/terminal evidence and preserve GUI alternatives | F035 |
| IVSD-M036 | Publish the operational skill only after the implemented CLI/version contract is verified | F036 |

### Rejected Alternatives

1. **Desktop only.** Rejected because a no-install web path materially improves
   access, provided secret mode remains optional and transparent.
2. **Web only.** Rejected because hosted delivery and browser downloads cannot
   provide the desktop path’s stronger origin and file-permission controls.
3. **Secret entry enabled by default.** Rejected as unnecessary exposure and a
   dark default.
4. **No secret placeholders in safe mode.** Rejected because the output would
   not guide deployment completion.
5. **Generate every known environment variable.** Rejected because it creates
   noise, unsafe accidental activation, and an unmaintainable file.
6. **Write documented defaults explicitly.** Rejected by default because
   omitted values should continue to receive canonical runtime defaults; an
   advanced explicit-default export may be separately labeled.
7. **Combine manifest and `.env`.** Rejected because a shareable artifact would
   become secret-bearing.
8. **Server-side generation.** Rejected for secret mode because it would
   require transmitting user secrets.
9. **CSP reporting in secret mode.** Rejected because reports are outbound
   requests and can contain contextual data.
10. **Third-party analytics, fonts, or CDN.** Rejected because they add network
    and supply-chain paths to a secret-handling page.
11. **Service worker/PWA in the first release.** Rejected pending a separate
    cache/update threat model.
12. **Automatic plaintext backup.** Rejected because it creates another secret
    copy.
13. **Persist secret projects.** Rejected because convenience expands exposure
    and recovery obligations.
14. **Retrieve existing Infisical/server secrets.** Rejected because it changes
    the tool into a privileged secret client.
15. **Gitignore/withhold browser source.** Rejected as ineffective against
    phishing or reimplementation and harmful to auditability.
16. **Use Avalonia professional/commercial tooling to simplify packaging.**
    Rejected under the no-commercial-dependency policy.
17. **Assume a primary FOSS license makes the graph compatible.** Rejected
    until the exact pinned release graph and target obligations are proven.
18. **Claim “fully safe” or “we cannot access secrets.”** Rejected as
    technically unprovable for hosted code and compromised local devices.
19. **Keep static hard-coded legal pages.** Rejected because self-hosters and
    tenants need accountable, portable, localized operator-owned texts.
20. **Store arbitrary HTML.** Rejected because it introduces executable,
    tracking, sanitization, accessibility, and portability risk.
21. **Copy public legal templates.** Rejected by clean-room and license
    governance; templates require project-owned or approved-FOSS provenance and
    legal review.
22. **Auto-publish imported legal text.** Rejected because target identity,
    jurisdiction, acceptance, and effective-date authority must be reviewed.
23. **Migrate acceptance history in configuration.** Rejected because
    acceptance is immutable application evidence, not portable configuration.
24. **Embed an AI assistant.** Rejected because deterministic setup needs no
    model/provider dependency or secret-bearing inference path.
25. **Have agents drive the full-screen TUI.** Rejected because terminal state
    is unstable and machine commands are safer and testable.
26. **Pass secrets in CLI arguments or captured stdin.** Rejected because
    process listings, shell history, and tool logs can disclose them.
27. **Publish the skill before the CLI exists.** Rejected because it would
    teach fictional commands and unsafe assumptions.

## Common Overlooked Failures And Outcomes

Feature type: cross-platform configuration, legal-content authoring, and
secret-bearing dotenv generation.

### Common overlooked failures

- secret values appear in validation messages;
- browser autofill stores values;
- secret-mode state survives back navigation;
- analytics or crash tooling is inherited from a shared app template;
- remote fonts or icons create requests after secret entry;
- CSP violation reporting contradicts the zero-request promise;
- lazy translation loads after values are entered;
- service worker serves stale or tampered application code;
- SRI covers subresources but not a malicious main document;
- browser extension reads the DOM;
- managed memory retains copied strings;
- browser download permissions are assumed secure;
- desktop writes through a symlink;
- overwrite creates a plaintext backup;
- generated file quotes values incorrectly for Compose;
- empty relevant secret keys are mistaken for valid readiness;
- irrelevant optional secrets clutter output;
- documented defaults are frozen into generated files and later drift;
- manifest accidentally contains a secret;
- generated source maps or support bundles contain form state;
- auto-update runs during a secret session;
- macOS/Windows signing is skipped for “small” releases;
- Linux packaging is claimed from one tested distribution;
- license-incompatible native library enters through a transitive package;
- commercial packaging tooling enters the build unnoticed;
- hidden browser source is presented as protection against malicious hosting;
- unofficial lookalike site uses ISLAMU branding;
- accessibility status announces a secret value;
- a tenant document is shown as an instance/platform promise;
- a source instance’s legal URL remains after migration;
- imported terms overwrite acceptance history;
- template prose is presented as legal compliance;
- raw HTML or remote Markdown content executes or tracks visitors;
- untranslated legal text silently falls back across responsible parties;
- legal Markdown exceeds scanner limits or makes import unusable;
- template placeholders publish unresolved;
- agent parses localized prose instead of versioned JSON;
- agent drives TUI screen state and chooses the wrong action;
- secrets appear in shell history, process arguments, scrollback, or stdout;
- skill asks the user to paste secrets into chat;
- skill and CLI versions drift;
- agent publishes legal text or applies configuration without approval;
- embedded AI adds provider keys, telemetry, or nondeterministic behavior;
- GPL/AGPL dependency is linked into a target expected to remain
  alternatively licensable without explicit approval.

### Possible bad outcomes

- credential theft;
- compromised instance, payment, identity, storage, email, or federation
  infrastructure;
- false confidence in a generated but incomplete `.env`;
- accidental Git commit or cloud backup of plaintext credentials;
- deployment outage;
- cross-platform users receiving unsupported or unverifiable artifacts;
- inaccessible setup for disabled administrators;
- license incompatibility blocking public or alternative distribution;
- reputational and legal harm from an absolute security claim;
- self-hosters becoming more dependent rather than more autonomous;
- support overload caused by noisy or irrelevant generated files;
- erosion of open-source trust through a hidden official web implementation;
- legally misleading operator attribution;
- users bound to text they were never shown or never accepted;
- target deployment relying on obsolete source-instance legal pages;
- cross-jurisdiction misstatement;
- inaccessible or unsafe public legal pages;
- agent-driven destructive or authority-broadening configuration;
- terminal secret disclosure;
- stale skills producing invalid artifacts;
- loss of an intended outbound licensing path;
- AI-provider lock-in and unnecessary privacy obligations.

### Positive outcomes if implemented responsibly

- lower self-hosting barrier;
- safer alternative for operators without Infisical;
- practical no-secret default;
- fewer irrelevant environment variables;
- clearer setup readiness;
- consistent manifest/environment validation;
- credible cross-platform access;
- stronger release provenance and dependency evidence;
- improved accessibility and localization;
- reduced vendor and hosted-service lock-in;
- truthful understanding of web versus desktop assurance;
- portable, localized, role-accurate legal texts;
- easier counsel review through structured templates and diffs;
- preserved historical acceptance integrity;
- safer public rendering through constrained Markdown;
- deterministic automation without embedding AI;
- terminal-first access for remote and low-resource operators;
- auditable human approval around agent-generated drafts;
- broader FOSS participation without commercial dependencies.

### Provider questions before implementation

- Which FOSS licenses are compatible with each executable and outbound path?
- Which exact secrets may be generated rather than provider-issued?
- Which environment variables have safe defaults and activation predicates?
- Which browser versions can enforce the reviewed CSP?
- Can the selected Avalonia Browser build run without any post-load request?
- How are main-document and origin compromise explained?
- Which desktop filesystems cannot enforce expected permissions?
- What Linux distributions and architectures are genuinely supported?
- What source/release evidence is displayed before secret opt-in?
- What event triggers an I-VSD and threat-model refresh?
- Which legal document kinds are instance-, tenant-, or organizer-owned?
- Which imported legal texts become drafts versus scheduled versions?
- Which changes require fresh user acceptance and notification?
- Which template sources and licenses are approved?
- Which commands and JSON schemas are stable enough for the skill?
- Which operations remain human-only?
- Which terminal environments and assistive technologies are supported?

## Stakeholders

| Stakeholder | Interest | Provider-controlled protection |
|---|---|---|
| New self-hoster | Complete setup without mastering hundreds of variables | Progressive profile, relevant-only output, docs, readiness |
| Experienced operator | Deterministic, inspectable, offline tooling | Raw preview, exact catalogue, no hidden defaults, signatures |
| User without Infisical | Safe local secret-entry option | Desktop path, optional web mode, no persistence/network |
| Security-conscious user | Strongest available assurance | Signed desktop, offline bundle, source/digest evidence |
| Web convenience user | No-install experience | Default no-secret mode and explicit trust choice |
| Disabled administrator | Equal ability to configure deployment | Accessible controls, status, review, platform testing |
| Arabic/RTL user | Understandable, correct setup workflow | Bundled localization and logical layout |
| Instance/tenant administrator | Portable non-secret configuration | Manifest/package workspace and strict separation |
| Maintainer | One source of truth and supportable releases | Shared core, generated catalogue, CI drift gates |
| Release operator | Verifiable multi-platform artifacts | lock files, SBOM, signatures, attestations, smoke tests |
| ISLAMU steward | Truthful official trust and legal accountability | official origin, identity, transparent claim boundary |
| Instance operator/legal counsel | Accurate platform texts and responsibilities | typed instance documents, templates, review lifecycle |
| Tenant operator/legal counsel | Local autonomy without false platform claims | tenant-owned documents, additive composition, target review |
| Terminal-first operator | Full functionality over SSH/console | Terminal.Gui TUI, stable commands, no GUI requirement |
| External AI-agent user | Deterministic automation without bundled AI | machine JSON, no-secret defaults, approval gates |
| Skill maintainer | Commands and guidance remain aligned | version range, executable examples, schema/link tests |
| Third-party self-hoster | Freedom to audit and host compliant code | tracked source, AGPL obligations, brand distinction |
| People affected by compromise | Protection from downstream misuse | minimum exposure, incident process, no overclaims |

## I-VSD Principles And Domains

| Principle | Application |
|---|---|
| Amanah / Trust | Secret handling, release identity, source, permissions, and limits are explicit and auditable. |
| Sidq / Truthfulness | Hosted origin trust and memory/browser limitations are never hidden by “client-side” marketing. |
| Adl / Justice | Web, desktop, Linux, accessibility, localization, and self-hosting paths avoid excluding less-resourced users. |
| Non-Harm | No-secret default, network denial, no persistence, local generation, and safe writes reduce foreseeable compromise. |
| Rights of People | Operators retain control of configuration and can avoid sending secrets to ISLAMU. |
| Avoiding Spying | No telemetry, analytics, session replay, remote logs, or secret-bearing reports. |
| Avoiding Gharar | Relevant/omitted/defaulted/incomplete states and trust boundaries are known before download. |
| Promise-Keeping | Cross-platform, FOSS-only, local-only, CLI-parity, and self-hosting claims require concrete release evidence. |
| Ihsan / Excellence | Accessibility, RTL, deterministic generation, signatures, SBOMs, and adversarial tests are core quality. |

Domain review:

- **Strategic:** lower self-hosting barrier without making users dependent on
  official hosting.
- **Design:** no-secret default, explicit trust, progressive disclosure,
  relevant-only output, and accessible review.
- **Technical:** pure shared core, client-side execution, network denial,
  memory minimization, safe file output, versioned CLI, and per-target FOSS
  compatibility.
- **Operational:** signed multi-platform releases, incident response, support,
  package lifecycle, and upgrade evidence.
- **Governance:** official identity, truthful legal copy, AGPL source
  obligations, trademark boundaries, and dependency approval.
- **Evaluation:** request capture, storage inspection, filesystem checks,
  usability studies, accessibility audits, and package smoke tests.

## Validation Gaps

- No Setup Assistant implementation or plan exists.
- No Avalonia version has been selected or pinned.
- The resolved Avalonia and Terminal.Gui runtime/native graphs have not passed
  FOSS and target-outbound compatibility review.
- No exact CSP has been proven against the selected Avalonia Browser/.NET
  output.
- No browser test proves zero requests after secret-mode entry.
- No memory/DOM/storage inspection has been performed.
- No desktop permission writer has been tested across target filesystems.
- No authoritative environment catalogue or activation predicates exist.
- No support matrix has been approved.
- No real users have tested the progressive form or understood omissions.
- No assistive-technology or RTL evidence exists for Avalonia targets.
- No reproducible build, code-signing, notarization, Linux package signature,
  SBOM, or attestation pipeline exists for this product.
- No legal review has approved the official web trust disclosure, privacy
  statement, or trademark wording.
- No legal-document aggregate, typed kind catalogue, Markdown profile,
  lifecycle, acceptance boundary, or template library exists.
- Current Terms and Privacy pages are static application prose.
- No legal-template provenance or qualified legal review exists.
- No content-size analysis proves realistic localized legal documents fit the
  next manifest/package limits.
- No security review has approved hosted secret entry.
- No CLI/TUI project, command schema, exit-code contract, or parity test exists.
- No terminal secret-entry threat model or supported-terminal matrix exists.
- No Setup Assistant skill exists, and it must not be published before the
  command contract is real.
- No explicit decision identifies which Setup executables, if any, may become
  AGPL-only because of reciprocal dependencies.

The current evidence supports design reasoning and repository implementation
traceability only. It does not establish stakeholder or operational validation.

## Escalation Needed

- Security threat model before planning approval.
- Independent security review before enabling online-hosted secret entry.
- IP/legal dependency review after pinning the complete Avalonia and packaging
  graph, Terminal.Gui graph, and any reciprocal-license target.
- Qualified legal review for hosted secret-mode copy, privacy promises,
  official/unofficial attribution, AGPL network-source obligations, trademark,
  and incident notices.
- Qualified legal review for every bundled legal template, role assignment,
  jurisdiction assumption, acceptance rule, and public legal claim.
- Accessibility review before UI architecture approval and real platform
  audits before release.
- Release-engineering approval for signing, notarization, SBOM, provenance,
  package retention, and update policy.
- Fresh I-VSD review before Infisical integration, live credential tests,
  service worker/PWA, auto-update, token persistence, plugins, or live secret
  retrieval.
- Agent-context review before publishing the skill, plus security review of
  every command the skill may invoke.
- Qualified Sunni scholarly review only if future product claims, contracts,
  payment features, or marketing introduce religious-legal conclusions. No
  such ruling is made here.

## Evidence Reviewed

### Repository Evidence

| Evidence ID | Locator | Contribution |
|---|---|---|
| E001 | `.env.example` | Current environment template and user-facing variable surface |
| E002 | `docker-compose.yml` | Compose interpolation and deployment profiles |
| E003 | `docs/CONFIGURATION.md` | Canonical configuration behavior and sources |
| E004 | `docs/SECRETS.md` | Secret-provider and `.env` boundaries |
| E005 | `docs/SECURITY-MODEL.md` | BFF, token, logging, and secret trust patterns |
| E006 | `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs` | Secret keys, scopes, source types, and environment names |
| E007 | `islamic-value-sensitive-design/i-vsd-configuration-manifest.md` | Manifest/package portability and strict non-secret boundary |
| E008 | `Directory.Packages.props` and `Directory.Build.props` | Central versions, lock files, and outbound package posture |
| E009 | `docs/legal/IP_GOVERNANCE.md` and clean-room dependency gate | Complete dependency and provenance requirements |
| E017 | `LICENSE` and `islamic-value-sensitive-design/i-vsd-licensing-and-commercial-strategy.md` | AGPL network-service and open-source governance context |
| E022 | `docs/ACCESSIBILITY.md` | WCAG 2.2 AA-aligned repository standards |
| E023 | `docs/LOCALIZATION.md` | Localization, offline bundles, and RTL behavior |
| E024 | `src/Explore.Blazor.Client/Pages/Legal/TermsOfService.razor` and `PrivacyPolicy.razor` | Current static legal-text implementation |
| E025 | `docs/FOOTER_MANAGEMENT.md` | Existing instance/tenant legal-link and operator-role separation |
| E026 | `islamic-value-sensitive-design/i-vsd-branding-legal-identity-authority.md` | Legal identity, operator attribution, and no-fallback boundaries |
| E027 | `docs/DUAL_VERSIONING.md`, `docs/legal/IP_GOVERNANCE.md`, and `legal/CLA.md` | FOSS/commercial distinction, dependency obligations, and alternative outbound paths |
| E031 | `eng/release/src/ISLAMU.ReleaseEngineering/Program.cs` and schema-generator `Program.cs` | Existing deterministic command, exit, bounded-output, and help conventions |
| E032 | `.agents/skills/_SKILL_SCHEMA.md` | Required skill metadata, progressive disclosure, and verification shape |
| E033 | `.agents/skills/skill-authoring/SKILL.md` and resources | Skill lifecycle, command-evidence, and no-fiction requirements |

### Official Functional References

| Evidence ID | Source | Observed functional fact |
|---|---|---|
| E010 | [Avalonia WebAssembly deployment](https://docs.avaloniaui.net/docs/deployment/webassembly) | Browser publish output is a static client-side WebAssembly site |
| E011 | [Avalonia framework FAQ](https://docs.avaloniaui.net/tools/faq) | Avalonia framework is MIT; professional tooling has separate licensing |
| E012 | [MDN CSP guide](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP) | CSP restricts resource/action channels but is defense in depth |
| E013 | [MDN `connect-src`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/connect-src) | Script connection APIs controlled by `connect-src` |
| E014 | [Microsoft Blazor CSP guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/content-security-policy?view=aspnetcore-10.0) | Client-side WebAssembly can use `connect-src 'none'`; CSP does not guarantee complete security |
| E015 | [.NET Unix file-mode API](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.setunixfilemode?view=net-10.0) | .NET exposes Unix file-mode control |
| E016 | [.NET cryptographic random API](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getbytes?view=net-10.0) | .NET exposes cryptographically strong random byte generation |
| E018 | [Avalonia Linux guide](https://docs.avaloniaui.net/docs/platform-specific-guides/linux) | Linux desktop, backend, and accessibility behavior |
| E019 | [Avalonia macOS guide](https://docs.avaloniaui.net/docs/platform-specific-guides/macos) | macOS backend, app bundle, platform, and accessibility behavior |
| E020 | [.NET RID catalogue](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog) | Portable Windows, Linux, and macOS runtime identifiers |
| E021 | [GNU AGPL-3.0-or-later repository license](../LICENSE) | Network-service source and distribution governance |
| E028 | [GNU license FAQ](https://www.gnu.org/licenses/gpl-faq.html#AGPLGPL) | GPLv3/AGPLv3 linking compatibility and combined-work obligations |
| E029 | [Terminal.Gui documentation](https://tui-cs.github.io/Terminal.Gui/index.html) | Windows/macOS/Linux TUI, editor, wizard, keyboard/mouse, Unicode, and inline/full-screen behavior |
| E030 | [Terminal.Gui 2.4.17 NuGet metadata](https://api.nuget.org/v3/catalog0/data/2026.07.07.12.25.25/terminal.gui.2.4.17.json) | MIT package metadata, net10 target, and transitive dependency inventory |

External research used only official public documentation and standards-style
references. No external source code, AST, schema, tests, migrations, prose,
assets, screenshots, or product implementation structure was retained or
copied.

The reviewed repository input digest covers E001-E006, E008-E009, E017,
E022-E027, and E031-E033 plus the current configuration-manifest
plan/context/tasks and guide.
E007 is cross-report context and is excluded from the digest to avoid circular
report hashes.
User decisions on 2026-08-29 additionally establish:

- web and desktop targets;
- no-secret web mode as default;
- optional official-host secret mode;
- secret values remain client-side;
- relevant empty secret placeholders;
- omission of irrelevant/defaulted variables;
- explanatory generated header;
- open browser source with only generated `wwwroot`/artifacts ignored;
- portable instance and tenant Terms, Privacy, and broader legal texts;
- legal templates and Markdown editing;
- FOSS-only dependencies with no commercial/proprietary components and
  target-specific GPL/AGPL compatibility review;
- Terminal.Gui TUI plus versioned CLI commands;
- an external agentic skill instead of embedded AI.

## Missing Evidence

- exact Avalonia package/version/lock graph;
- exact Terminal.Gui package/version/lock graph and transitive-license review;
- authoritative license record for every transitive/native artifact;
- approved environment catalogue;
- full key/default/activation mapping from current source;
- threat model and misuse cases;
- official host deployment architecture;
- browser compatibility matrix;
- main-document/release reproducibility proof;
- desktop filesystem/ACL behavior matrix;
- code-signing and notarization credentials/process;
- Linux package repository/signing strategy;
- user research;
- accessibility and RTL audits;
- legal privacy/trademark/source-offer review;
- incident and vulnerability response capacity for the new product;
- approved legal-document kind/authority matrix;
- legal Markdown schema and sanitizer;
- legal template provenance and counsel review;
- legal publication/acceptance migration semantics;
- localized legal-content size and usability evidence;
- final CLI command/JSON/exit-code contract;
- terminal security/accessibility matrix;
- skill routing, resources, compatible CLI range, and executable examples;
- Project Steward/legal decision for any reciprocal AGPL-only target.

## Context Inventory

Reviewed:

- current configuration, secrets, self-hosting, security, accessibility,
  localization, footer/legal identity, governance, and licensing
  documentation;
- current `.env.example` and Compose deployment surface;
- secret registry and manifest portability report;
- current static Terms and Privacy pages;
- solution/project/package structure;
- official Avalonia WebAssembly, Linux, macOS, Native AOT, storage, and license
  documentation;
- official Terminal.Gui functional/package metadata and GNU reciprocal-license
  compatibility guidance;
- existing repository CLI and skill-authoring conventions;
- official browser CSP/SRI guidance and .NET file/random APIs;
- user’s initial architecture proposal and resolved web-mode decisions.

Not reviewed:

- Avalonia or third-party source code;
- an exact NuGet restore graph for the proposed app;
- commercial Avalonia tooling terms beyond the official framework/tooling
  distinction;
- external competitor implementation or UI;
- user secrets or private deployment configuration;
- production logs, incidents, support cases, or stakeholder interviews.

## Planning Requirements

A later implementation plan should map every open/accepted finding and
mitigation into:

1. product scenarios and claim vocabulary;
2. pure shared-core extraction;
3. environment catalogue and generated artifacts;
4. manifest/package workspace;
5. no-secret web workflow;
6. secret web threat model and invariant-breaker tests;
7. desktop safe-write adapters;
8. browser CSP/network/storage tests;
9. accessibility/localization/RTL;
10. FOSS/reciprocal per-target dependency proof;
11. Windows/Linux/macOS/web packaging and signing;
12. source/provenance/reproducibility;
13. legal/privacy/trademark review;
14. operator and security documentation;
15. release and incident evidence;
16. typed instance/tenant legal-document contracts and lifecycle;
17. safe Markdown parser/editor and public renderer;
18. project-owned or approved-FOSS legal-template provenance;
19. legal import/publication/acceptance invariant-breaker tests;
20. localized legal-content accessibility and size evidence;
21. Terminal.Gui TUI and noninteractive CLI adapters;
22. versioned machine JSON and stable exit categories;
23. terminal secret invariant-breaker tests;
24. FOSS/reciprocal license target map and SBOMs;
25. external-agent approval and no-secret scenarios;
26. a schema-compliant skill created only after CLI implementation;
27. architecture tests proving no embedded AI/provider dependency.

Planning owns architecture sequencing and task status. This report owns
provider-responsibility constraints and refresh triggers.

## Review Lifecycle

| Date | Previous status | New status | Trigger | Evidence/replacement |
|---|---|---|---|---|
| 2026-08-29 | none | draft | Initial cross-platform Setup Assistant consultation | Repository evidence and official framework/security references |
| 2026-08-29 | draft | current / ready-for-planning | User selected default no-secret web mode plus optional trusted official-host secret mode | Initial report revision |
| 2026-08-29 | current / ready-for-planning | current / ready-for-planning | User added portable instance/tenant legal texts, templates, Markdown editing, and broader legal-content QoL | This revision, reviewed input `sha256:b4ebca52a625ba32daaefd6f2517b0f41ffc3833e2bcda023de608e953b96c11` |
| 2026-08-29 | current / ready-for-planning | current / ready-for-planning | User confirmed open browser source, broadened to compatible FOSS, and added Terminal.Gui CLI/TUI plus an external agentic skill | This revision, reviewed input `sha256:b053b7f69ca3822efbd1dc2333d2138d6361df8dd5eade311a2f43e2532b17ef` |

Refresh this report when:

- a framework/package/version or license policy is selected;
- the environment catalogue changes secret/default/relevance semantics;
- web origin, CSP, service worker, telemetry, crash, update, or hosting behavior
  changes;
- desktop secret persistence or live provider integration enters scope;
- legal kinds, templates, Markdown, publication, acceptance, or operator-role
  composition changes;
- CLI commands, JSON schema, TUI behavior, skill instructions, agent approval,
  or embedded-AI boundary changes;
- dependency/license choice changes an executable’s outbound path;
- packaging, signing, official identity, source availability, or legal copy
  changes;
- implementation evidence, stakeholder feedback, incidents, or audits change
  any IVSD-F001 through IVSD-F036 conclusion.
