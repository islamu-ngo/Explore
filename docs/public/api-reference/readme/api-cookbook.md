---
description: >-
  Task-oriented request patterns for reads, HAL writes, retries, and privacy
  erasure.
---

# API Cookbook

Set an environment-specific base URL and choose one authentication mechanism:

```bash
export API_URL=https://localhost:7039
export TENANT_SLUG=example
export ACCESS_TOKEN='<redacted>'
```

## Read public events

```bash
curl --fail-with-body \
  -H 'Accept: application/hal+json;v=0.1' \
  -H "X-Tenant-Slug: $TENANT_SLUG" \
  "$API_URL/api/events?page=1&pageSize=20"
```

Public GET operations are anonymous where documented. Tenant resolution still applies. Follow returned links for detail and navigation rather than manufacturing paths from titles or slugs.

## Read as an authenticated user

```bash
curl --fail-with-body \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H 'Accept: application/hal+json;v=0.1' \
  -H "X-Tenant-Slug: $TENANT_SLUG" \
  "$API_URL/api/<resource>"
```

For direct integrations, replace the bearer header with `X-API-Key`; never send both. Keep keys out of shell history, logs, screenshots, and committed files.

## Perform a HAL-driven write

1. Fetch the resource/collection with HAL enabled.
2. Locate the required relation in `_links`.
3. Use its current `href` and documented method.
4. Add a new stable UUIDv7 idempotency key when that write supports retries.
5. Refresh the representation after success or conflict.

```bash
curl --fail-with-body \
  -X POST \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/hal+json;v=0.1' \
  -H "Idempotency-Key: $OPERATION_ID" \
  --data @request.json \
  "$HAL_HREF"
```

Do not infer that a write is allowed because the caller has an admin-like claim. Absence of the relation is the authoritative client signal.

## Handle failures

```bash
curl --include --fail-with-body ...
```

Inspect the HTTP status and ProblemDetails problem code. Preserve `traceId`/`correlationId` for support while removing tokens, keys, receipts, PII, provider payloads, and private paths. Retry only documented transient/idempotent operations; a validation, authorization, or concurrency response usually requires new state or user action.

## Request account erasure

Generate one UUIDv7 for the logical request:

```bash
curl --include --fail-with-body \
  -X DELETE \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Idempotency-Key: $ERASURE_OPERATION_ID" \
  "$API_URL/api/user"
```

The API returns `202 Accepted`, a one-time receipt, `Location: /api/privacy-erasure/status`, and `Retry-After: 5`. Store the receipt only in protected short-lived client state; it cannot be recovered from logs.

Poll with the receipt capability, not the user bearer token:

```bash
curl --fail-with-body \
  -H "Authorization: ErasureReceipt $ERASURE_RECEIPT" \
  "$API_URL/api/privacy-erasure/status"
```

Missing, invalid, wrong, or expired receipts intentionally produce indistinguishable unauthorized responses.

## Keep generated clients current

Build the API in Release to produce `schemas/openapi-islamu-event.json`, then use the repository generation workflow. Do not hand-edit generated OpenAPI inventory or NSwag client files. Every pre-v1 breaking change must be reviewed against the API changelog.
