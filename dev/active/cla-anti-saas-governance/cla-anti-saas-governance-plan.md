<!-- ABOUTME: Implementation plan for CLA Anti-SaaS Governance, README transparency, and licensing documentation synchronization. -->
<!-- ABOUTME: Formally establishes the Project Steward Anti-SaaS covenant and three-pillar ecosystem across all governance artifacts. -->

# CLA Anti-SaaS Governance And README Transparency — Implementation Plan

Last Updated: 2026-08-27 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Write implementation plan to update the CLA, README, and relevant governance files with the Anti-SaaS covenant and Internal-Use-Only alternative licensing boundaries, ensuring contributor trust and mission alignment.
- **Task directory:** `dev/active/cla-anti-saas-governance/`
- **Planning status:** Draft; awaiting user review and approval
- **Primary matched intent:** `documentation-update` / `governance-update`
- **Relevant skills:** `implementation-plan`, `i-vsd`, `clean-architecture-rules`
- **I-VSD Document:** [`islamic-value-sensitive-design/i-vsd-licensing-and-commercial-strategy.md`](../../../islamic-value-sensitive-design/i-vsd-licensing-and-commercial-strategy.md)
- **Complexity:** Medium (High-impact documentation, governance alignment, and trust assurance).

## 1. Executive Summary

This workstream updates ISLAMU Event's root `README.md`, `legal/CLA.md`, `CLA.md`, `CONTRIBUTING.md`, `docs/CONTRIBUTING.md`, `docs/legal/IP_GOVERNANCE.md`, and `docs/legal/CONTRIBUTION_GOVERNANCE.md` to clearly explain the rationale and strict boundaries of the Contributor License Agreement (CLA).

It formally integrates the **Anti-SaaS Governance Covenant**:
1. **Core Open-Source Invariant:** ISLAMU Event is and will perpetually remain free and open-source under `AGPL-3.0-or-later`.
2. **The CLA Rationale (Internal-Use Only):** The CLA exists solely to allow the ISLAMU non-profit to offer alternative bilateral licenses to enterprises facing internal AGPL compliance bans for *private on-premises/VPC internal event operations* (without AGPL Section 13 copyleft contagion to their private internal corporate systems), and gratis grant licenses to humanitarian/educational/nonprofit entities.
3. **The Anti-SaaS Promise:** The Project Steward commits never to grant an alternative license permitting a third party to operate a closed-source, proprietary SaaS or managed cloud service. Any entity providing a SaaS/hosted service must do so under `AGPL-3.0-or-later`, guaranteeing source code reciprocity and total community parity.
4. **Three-Pillar Ecosystem Transparency:** Explicitly outlines (1) AGPLv3 Community Commons, (2) Enterprise Internal-Use License, and (3) Official Partner Program (quality branding & services, 100% AGPL codebase).

---

## 2. Behavioral Delta & Scenarios

### Scenario 1: Prospective Contributor Evaluates the CLA
- **WHEN** a developer inspects `README.md` or `legal/CLA.md` before contributing,
- **THEN** they SHALL find unambiguous assurances that their contributions cannot be repackaged into a proprietary closed-source SaaS by any commercial entity or partner.

### Scenario 2: Enterprise Compliance Evaluates Internal Deployment
- **WHEN** a corporate legal counsel reviews ISLAMU Event for internal company events,
- **THEN** they SHALL find a clear path to an Enterprise Internal-Use License that removes AGPL Section 13 risk for private internal infrastructure, while explicitly noting that SaaS redistribution is prohibited.

### Scenario 3: Commercial SaaS Operator Seeks Market Entry
- **WHEN** a commercial hosting or cloud provider seeks to offer ISLAMU Event as a hosted service/SaaS,
- **THEN** they SHALL be bound to `AGPL-3.0-or-later`, requiring them to publish all source code modifications and participate in the open commons.

---

## 3. Scope of Modifications

### Phase 1: Core Legal & Root Pointer Updates
- `legal/CLA.md`: Update Preamble, Purpose, and Section *Why A CLA Alongside AGPL-3.0-Or-Later?* to codify the Project Steward's Anti-SaaS covenant and Internal-Use scope.
- `CLA.md`: Synchronize the root pointer with the Anti-SaaS governance principles.

### Phase 2: User-Facing README Updates
- `README.md`:
  - Enhance Section `### ✍️ Contributor License Agreement` with a dedicated "Why We Have a CLA & Why You Can Trust It" callout.
  - Enhance Section `## 📄 License` with the Three-Pillar Ecosystem overview (Community AGPLv3, Enterprise Internal-Use, Official Partners).

### Phase 3: Contributor & Legal Governance Sync
- `CONTRIBUTING.md` & `docs/CONTRIBUTING.md`: Update CLA section with contributor protection details.
- `docs/legal/IP_GOVERNANCE.md`: Update Section *Legal Posture And Authority Boundary* to document the Anti-SaaS outbound licensing rule.
- `docs/legal/CONTRIBUTION_GOVERNANCE.md`: Document the Anti-SaaS covenant as an explicit project steward governance commitment.
- `docs/CI_CD_GOVERNANCE.md`: Synchronize references to alternative terms.

---

## 4. Verification Plan

- **Tier 4 Verification Discipline:** Markdown formatting checks, link integrity checks, and validation that no unintended code files were modified.
- **Link Check:** Verify all relative markdown links (`legal/CLA.md`, `docs/legal/IP_GOVERNANCE.md`, `islamic-value-sensitive-design/i-vsd-licensing-and-commercial-strategy.md`) resolve accurately.
