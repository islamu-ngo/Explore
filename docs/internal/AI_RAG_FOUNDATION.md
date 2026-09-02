ABOUTME: Tenant-safe foundation notes for future AI vector/RAG support.
ABOUTME: Defines the approved summary-only boundary before any vector store or embedding provider is enabled.

# AI RAG Foundation

> Status: implemented foundation contracts, no runtime vector store enabled.
> Last Updated: 2026-06-07

## Boundary

Future RAG must stay behind the same ATCR safety boundary as chat and MCP:

- Application owns provider-neutral ingestion/search contracts.
- Infrastructure owns any future `Microsoft.Extensions.VectorData` collection, vector-store provider, and `IEmbeddingGenerator` adapter.
- Persistence remains the source of tenant-filtered event summaries; vector search never bypasses EF/global tenant filters or API/HAL authorization.
- Private/full event content is excluded unless a later product/security decision explicitly approves a new content scope and tests.

## Implemented Foundation

The current code adds Application-layer guardrails only:

- `AiRagIndexDocument` — the only eligible future indexing shape.
- `AiRagContentScope` — currently only `TenantPublicEventSummary` and `GlobalPublicEventSummary` are allowed.
- `AiRagCitation` — citation metadata required for every index document.
- `AiRagIngestionPolicy` — fail-closed validation for tenant binding, supported kind, reference identity, approved scope, bounded display name/summary, and citation metadata.
- `AiRagSearchFilter` — tenant-bound search metadata that only permits approved public-summary scopes.

These contracts are deliberately provider-neutral. A future VectorData adapter can map them to vector record metadata fields and use `IEmbeddingGenerator<string, Embedding<float>>` behind Infrastructure without leaking SDK types into Application.

## Ingestion Rules

1. Ingest event summaries only; no full body/content, private notes, registration data, attendee data, prompts, responses, proposed-action payloads, provider errors, or tenant/user identifiers in report artifacts.
2. Ingest only after reading from tenant-filtered persistence queries or public/global summary projections.
3. Attach citations to every chunk so generated answers can point back to the source resource.
4. Keep display names and summaries bounded before embeddings are generated.
5. Treat ingestion/update/delete hooks as future work attached to event publish/update/delete flows; do not add background indexing until provider/storage/cost posture is approved.

## Query Rules

1. Every query must construct an `AiRagSearchFilter.ForTenant(tenantId)` or stricter filter.
2. Vector metadata filters must include tenant and content scope; global public summaries may be included only through the explicit allowed scope.
3. RAG results must be converted back into existing selected-reference DTOs before prompt packing.
4. Prompt packing remains bounded and escaped through `AiReferencePromptPacker`.
5. RAG output is advisory context only; it never grants mutation authority, HAL affordances, or confirmation rights.

## Validation

Run:

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-restore
```

Relevant tests: `AiRagIngestionPolicyTests`.
