<!-- ABOUTME: Feature-specific I-VSD prompts for overlooked moral failures, bad outcomes, and positive outcomes. -->
<!-- ABOUTME: Helps consultancy reports move from generic principles to concrete feature-type implementation risks. -->

# Feature Risk Patterns

Use this resource whenever the user consults about adding, changing, or evaluating a specific feature. The report must include feature-type risks that teams often forget, the likely bad outcomes, and the positive outcomes of implementing the feature with provider responsibility.

## Required Analysis

For the requested feature, identify:

- Common moral and ethical implementation failures for this feature type.
- Overlooked provider duties created by enabling the feature.
- Concrete stakeholder harms if the provider ignores those duties.
- Operational, legal, accessibility, security, privacy, moderation, and support consequences.
- Positive outcomes when the feature is implemented responsibly, such as reduced harm, better trust, clearer rights, improved accessibility, stronger auditability, or easier compliance evidence.

Do not make the list generic. Adapt it to the feature, product type, user population, jurisdiction, provider capacity, and deployment model.

## Prompting Questions

Ask or answer these before recommending the feature design:

- What can users submit, generate, share, store, buy, sell, automate, invite, rank, report, delete, export, or make public through this feature?
- What illegal, harmful, deceptive, inaccessible, abusive, privacy-invasive, or spiritually harmful use becomes possible?
- What happens to users, non-users, moderators, support staff, maintainers, and the provider when the feature is misused?
- What third-party services can suspend, ban, rate-limit, report, or deplatform the provider because of user behavior?
- What evidence, logs, appeals, notices, retention rules, user controls, and escalation paths are needed?
- What would be unfair, confusing, inaccessible, manipulative, or unsafe by default?
- What positive moral, operational, or compliance outcomes become possible if the provider implements the feature well?

## Example Feature Pattern: Image Upload

Use this as an illustration, not a fixed template. For image upload, common overlooked failures include:

- No moderation plan for illegal, abusive, exploitative, copyrighted, deceptive, or privacy-violating images.
- No answer for what happens when a storage, CDN, app-store, payment, hosting, or trust-and-safety provider suspends service because of uploaded content.
- No user-reporting, takedown, appeal, audit-log, or escalation workflow.
- No content retention/deletion policy for removed images, backups, thumbnails, derived metadata, or abuse evidence.
- No privacy review for EXIF/GPS metadata, face exposure, children, documents, screenshots, or bystander data.
- No accessibility path, such as alt-text input, decorative-image marking, captions, or guidance for assistive technologies.
- No limits on file type, size, malware scanning, rate limiting, abuse detection, storage cost, or denial-of-service risk.
- No user communication about acceptable content, review timelines, removal reasons, or provider discretion.

Possible bad outcomes include user harm, illegal content exposure, reputational harm, support overload, cloud/storage account suspension, app-store removal, domain blocking, legal notices, moderation trauma, inaccessible experience for disabled users, and broken trust when users discover hidden review or retention practices.

Positive outcomes of doing it morally include safer participation, clearer user expectations, better accessibility, stronger evidence for privacy/security controls, lower incident risk, more reliable third-party service relationships, and easier support for frameworks or audits such as GDPR, accessibility obligations, SOC 2-style control evidence, trust-and-safety reviews, or internal risk registers.

## Report Section Shape

Add this section to consultancy, design, implementation, compliance, and technical reports when a concrete feature is under review:

```text
## Common Overlooked Failures And Outcomes

Feature type: <requested feature>

Common overlooked failures:
- <feature-specific failure>

Possible bad outcomes:
- <stakeholder/provider/operational/legal/accessibility/security outcome>

Positive outcomes if implemented responsibly:
- <trust, accessibility, safety, compliance evidence, operational resilience, or user-rights outcome>

Provider questions before implementation:
- <question the provider must answer before building or shipping>
```

## Compliance And Audit Language

Mention compliance only as supporting operational evidence, not certification. Use wording such as:

- “This can improve GDPR evidence around consent, deletion, retention, or data minimization if supported by actual controls.”
- “This can support SOC 2-style control evidence for access control, logging, incident response, change management, or vendor risk if the organization maintains the required process evidence.”
- “This can support accessibility obligations when the design includes usable alternatives, labels, keyboard access, and assistive-technology evidence.”

Avoid wording such as “this makes the feature GDPR compliant,” “this gives SOC 2 compliance,” or “this proves the product is ethical.”
