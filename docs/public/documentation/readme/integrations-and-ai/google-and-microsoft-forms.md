---
description: >-
  Integrate Google Workspace and Microsoft 365 Forms through their bounded
  supported contracts.
---

# Google & Microsoft Forms

The two integrations intentionally use different provider contracts. Neither treats a callback correlation value as proof of identity.

## Google Forms

Google support targets Google Workspace and uses:

* tenant-owned OAuth;
* Forms API schema, provisioning, and response reads;
* OIDC-authenticated Pub/Sub notifications;
* watch renewal and recovery sweeps;
* strict response/correlation mapping.

Pub/Sub bodies are notify-only. They queue a source read and are not trusted as submitted answers. Drive/file-upload questions, response submission writes, and automatic finalization are not supported. Google callbacks do not use a shared webhook secret.

## Microsoft Forms

Microsoft support targets Microsoft 365 organizational accounts through an organizer-owned `POWER_AUTOMATE_V1` flow. Personal forms use link, embed, or manual reconciliation.

Activation requires:

1. a bounded completion envelope;
2. a binding-scoped callback key;
3. required field mapping and correlation;
4. at least one successfully processed callback.

The platform does not provision Microsoft Forms, read its schema, call a Graph Forms response API, claim a first-party Forms webhook, or ship a fabricated importable Power Platform solution.

## Reconciliation

CSV reconciliation is bounded and idempotent for supported workflows. Correlation connects provider responses to local records; it does not authenticate the participant.

## Acceptance

Use provider-owned test forms and non-production data. Verify OAuth/key scope, callback authentication, replay handling, mapping failures, watch or flow recovery, duplicate delivery, and a source read before activation. Document unsupported question types and the operator-owned provider configuration.
