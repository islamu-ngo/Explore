<!-- ABOUTME: Ingesting, structuring, configuring, and publishing OpenAPI specs in GitBook. -->
<!-- ABOUTME: Covers Scalar "Test it" runner configuration, x-* extensions, tag navigation, and CI/CD pipelines. -->

# GitBook OpenAPI & API Reference Integration

GitBook transforms OpenAPI documents into interactive, testable API reference documentation powered by Scalar. You provide a spec (JSON or YAML), and GitBook renders endpoints, parameters, schemas, authentication schemes, and an in-browser request runner. Most customization occurs directly within the OpenAPI spec via `x-*` extensions.

## Supported Specifications & Ingestion

- **Supported Versions**: OpenAPI 3.0.x and Swagger 2.0. OpenAPI 3.1 is required for webhooks. Tag nesting uses `parent` (OpenAPI 3.2+) or `x-parent` (3.0/3.1).
- **Spec Source Types**:
  - **URL Sources**: GitBook polls the hosted spec URL every 6 hours automatically.
  - **File Sources**: Static snapshot that updates only when re-uploaded via CLI, API, or MCP.

### Ingesting Specs
- **GitBook MCP**: Call spec tools directly if connected.
- **REST API**:
  - URL source: `POST /orgs/$ORG_ID/openapi` with `{"slug": "my-spec", "source": {"url": "https://..."}}`
  - File source: `POST /orgs/$ORG_ID/openapi` with multipart form fields `slug` and `file`.
- **GitBook CLI**:
  ```bash
  gitbook openapi publish --spec <spec-slug> --organization <org-id> <path-or-url>
  ```

## Surfacing API References in Documentation

1. **Auto-Generated Page Trees (Recommended)**:
   In `SUMMARY.md`, declare the spec reference using a fenced YAML block:
   ```markdown
   # Table of contents
   * [API Overview](README.md)
   * ```yaml
     type: builtin:openapi
     spec: my-spec
     ```
   ```
   GitBook automatically generates one page per tag in the spec and keeps pages updated as the spec evolves.

2. **Inline Endpoint Blocks**:
   Embed individual operations inside existing markdown pages:
   ```
   {% openapi src="https://api.example.com/openapi.json" path="/users" method="get" %}
   https://api.example.com/openapi.json
   {% endopenapi %}
   ```

3. **Inline Schema Blocks**:
   Embed data models or component schemas:
   ```
   {% openapi-schemas spec="my-spec" schemas="User,Account" grouped="false" %}
   The User and Account models
   {% endopenapi-schemas %}
   ```

## Structuring API Navigation via Tags

GitBook constructs the API reference navigation directly from the spec's `tags` array:

- **Page Partitioning**: Operations sharing a tag are grouped onto that tag's page.
- **Page Ordering**: Follows the order of items in the top-level `tags:` array.
- **Nested Groups**: Use `x-parent` to nest child tags under parent tags:
  ```yaml
  tags:
    - name: payments
    - name: refunds
      x-parent: payments
  ```
- **Page Titles, Icons, and Descriptions**:
  ```yaml
  tags:
    - name: payments
      x-page-title: Payments API
      x-page-icon: credit-card
      x-page-description: Process card payments and payouts
      description: Overview of payment lifecycles and idempotency keys.
  ```

## Configuring the "Test it" Runner

The interactive request runner executes requests directly from the reader's browser to the targets in `servers:`.

- **CORS Requirements**: Reader browsers will block requests unless the target API allows cross-origin requests (`Access-Control-Allow-Origin: *` or your GitBook domain).
- **GitBook Proxy**: If the backend API cannot enable CORS, route requests through GitBook's proxy:
  ```yaml
  x-enable-proxy: true # Set at root for entire spec, or per operation
  ```
  The proxy only forwards requests to domains explicitly declared in `servers:`.
- **Auth Setup**: Configure under `components.securitySchemes`. Customize header prefixes with `x-gitbook-prefix` (e.g. `Token`) and placeholder hints with `x-gitbook-token-placeholder`.
- **Hide Runner**: Set `x-hideTryItPanel: true` on private or sensitive operations.

See [openapi-test-it.md](openapi-test-it.md) for full configuration recipes.

## Operation Lifecycle Management

Manage staging, deprecation, and visibility directly in OpenAPI operations:
```yaml
paths:
  /transactions/legacy:
    post:
      deprecated: true
      x-deprecated-sunset: "2027-01-01"
      x-stability: beta # experimental | alpha | beta
      x-internal: true  # Completely hides endpoint from reference
```

## CI/CD Pipeline Automation

Automate spec updates on every merge to `main` via GitHub Actions:
```yaml
name: Publish OpenAPI to GitBook
on:
  push:
    branches: ["main"]
    paths: ["**/*.yaml", "**/*.json"]
jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Publish Spec
        env:
          GITBOOK_TOKEN: ${{ secrets.GITBOOK_TOKEN }}
        run: |
          npx -y @gitbook/cli@latest openapi publish \
            --spec "event-api" \
            --organization "${{ vars.GITBOOK_ORG_ID }}" \
            schemas/openapi.json
```

## Related Resources

- [openapi-extensions.md](openapi-extensions.md) — Comprehensive reference of all `x-*` extensions with YAML snippets.
- [openapi-test-it.md](openapi-test-it.md) — Scalar interactive runner setup, auth schemes, CORS, and proxy configurations.
- [blocks.md](blocks.md) — Syntax for `{% openapi %}` and `{% openapi-schemas %}` markdown blocks.
