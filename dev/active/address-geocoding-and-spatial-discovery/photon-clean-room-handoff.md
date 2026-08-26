<!-- ABOUTME: Sanitized Photon interoperability, topology, and provenance handoff. -->
<!-- ABOUTME: Records official-source facts without retaining third-party implementation expression. -->

# Photon Clean-Room Handoff

## Authority And Scope

- **Research date:** 2026-08-26.
- **Access basis:** public project documentation, public GitHub release/repository metadata,
  and public OpenStreetMap licensing documentation.
- **Permitted use:** interoperability facts, observable service behavior, operational
  constraints, license metadata, and independently designed ISLAMU requirements.
- **Excluded:** Photon source code, tests, internal schemas, build structure, comments,
  copied prose, assets, database contents, and third-party implementation organization.
- **Implementation separation:** provider implementation must start in a fresh agent
  context using this handoff and repository-native contracts only.

## Source Register

| Source | URL | Facts retained |
|---|---|---|
| Photon repository metadata | https://api.github.com/repos/komoot/photon | Public project; Apache-2.0 license metadata. |
| Photon latest release metadata | https://api.github.com/repos/komoot/photon/releases/latest | Current stable release `1.3.0`, published 2026-08-07; release JAR SHA-256 `a89707c0045e4807b2a1180e132e68e108d998709f48b6c94b98a6e281f571a5`. |
| Photon project documentation | https://github.com/komoot/photon/blob/master/README.md | Demo-service limitations, runtime capacity guidance, release/dataset setup model, and atomic database replacement requirement. |
| Photon API v1 documentation | https://github.com/komoot/photon/blob/master/docs/api-v1.md | Forward-search/status endpoints, supported bounded query parameters, GeoJSON response contract, and status semantics. |
| Photon operational documentation | https://github.com/komoot/photon/blob/master/docs/usage.md | Embedded/external search-store modes, result/query limits, metrics, import filtering, update isolation, and update endpoint exposure risk. |
| OpenStreetMap copyright and licence | https://www.openstreetmap.org/copyright | OSM-derived data is ODbL; attribution and database-license notice are required, with share-alike obligations when a derived database is distributed. |

## Approved Topology Decision

1. `Geocoding:Provider=None` remains the default and healthy state.
2. Photon is an explicit opt-in external service boundary. Production accepts only an
   operator-owned self-hosted endpoint or a separately contracted endpoint.
3. `photon.komoot.io` is forbidden in production and is never an implicit development
   fallback because the demo has no availability guarantee and may throttle, ban, or
   change without notice.
4. Production requires HTTPS, an operator-owned DNS name, network allowlisting, a
   non-public management plane, and a bounded `/status` readiness request. Readiness
   performs no address lookup and exposes no configured endpoint or dataset detail.
5. Lightweight local profiles keep Photon disabled. `local-full` may opt into an
   explicitly configured local endpoint, but the repository does not auto-download a
   planet database or silently start a heavy service.
6. No unofficial container image is approved. No authoritative project-owned container
   publication was established during this review. A self-hosted operator may package
   the pinned `1.3.0` release artifact in its own reviewed image or run it as a separately
   managed service.
7. A dataset is not approved until its artifact URI, Photon compatibility version,
   source date, geographic scope, SHA-256, ODbL attribution record, and restore test are
   captured in the operator deployment manifest. Weekly or `latest` aliases are not
   production pins.

## Capacity And Lifecycle Contract

- Planet-scale planning starts from approximately 95 GB of database disk in 2026,
  approximately 10% annual growth, SSD/NVMe storage, and at least 64 GB RAM for smooth
  operation. These are planning floors, not acceptance evidence.
- Blue/green dataset replacement reserves at least twice the active database size plus
  extraction headroom. A new dataset is verified, started, checked through `/status`,
  and benchmarked before traffic moves. The previous dataset remains intact until the
  rollback window closes.
- Regional deployments use an operator-produced country-filtered import or a pinned
  compatible country artifact. Application query country/language parameters do not
  reduce deployed storage or memory.
- Production activation requires a representative load test at the configured aggregate
  ingress rate, p95 provider latency at or below 2 seconds, less than 1% provider-side
  error rate, and at least 2x measured capacity headroom while staying inside the
  application 5-second total budget.
- Refresh cadence is an operator decision recorded with dataset age objectives. Update
  management endpoints remain private. Failed refreshes keep serving the last verified
  dataset; rollback changes the endpoint/data slot, never application data.
- Disaster recovery is rebuild-first from the pinned release and dataset manifest.
  Backups may shorten recovery but do not replace checksum, attribution, and restore
  verification.

## Interoperability Specification

- Forward search uses the provider's documented `/api` operation.
- The only outbound user-derived value is the current explicit provider search text.
  Application-owned local suggestions and stored custom addresses are never uploaded,
  synchronized, enriched, or fed back to Photon or any OSM-derived dataset.
- Requests may include only bounded `q`, `limit`, `lang`, and repeated ISO country-code
  filters owned by server configuration/request context. Provider selection, endpoint,
  credentials, retry policy, and result authority remain server-owned.
- Responses are treated as untrusted GeoJSON. The adapter consumes only a bounded feature
  collection, point coordinates, and the minimum address/display properties needed by
  the existing provider-neutral Application model. Unknown fields are ignored.
- Coordinates must be finite and ordered according to the GeoJSON interoperability
  contract before conversion into the repository's provider-neutral latitude/longitude
  model. Missing or malformed required fields discard that feature without failing other
  valid features.
- Provider results retain `Photon` source, OpenStreetMap attribution, provider record
  identity when present, and the configured dataset/version provenance required by the
  Application contract.
- No full geometry, reverse geocoding, update endpoint, category expansion, raw OSM tags,
  or provider feedback path is in scope.

## Privacy, Resilience, And Telemetry Requirements

- Browser requests remain authenticated private POSTs through the existing BFF. Photon
  URI query strings are never logged, tagged, emitted in exceptions, or returned to the
  browser.
- The provider client has a 5-second total budget, at most two retries, and deterministic
  200 ms then 500 ms backoff. Retry only transport failures, 408, 429, and 5xx when the
  next attempt fits the total budget. Caller cancellation stops immediately.
- A bounded `Retry-After` may replace the configured delay only when it fits the remaining
  total budget. Other 4xx responses are terminal.
- Logs and metrics contain only provider name, normalized outcome category, retry count,
  and latency bucket. They contain no query, URI, address, postcode, coordinates,
  provider record ID, tenant/user/organization ID, protected token, or exception text
  capable of carrying those values.
- `Provider=None`, unavailable Photon, or failed Photon never removes eligible local reuse
  or policy-authorized manual entry. There is no silent provider fallback.

## Dependency And License Decision

- **Photon service/artifact:** Apache-2.0; approved only as an optional separately
  operated service pinned to reviewed release metadata. If an operator redistributes the
  JAR or image, retain the applicable license/notices and review patent/trademark duties.
- **OSM-derived dataset:** ODbL; approved for operator-managed service use with visible
  OpenStreetMap attribution and database-license notice. Distribution or publication of
  a derived database requires a separate ODbL compliance review.
- **ISLAMU local data:** remains in the ISLAMU database and is never merged into the
  OSM-derived Photon database, preserving the repository's clean separation and outbound
  licensing options.
- **Repository dependency change:** none for the topology decision. No Photon package,
  container, dataset, source, generated artifact, font, or asset is added.

## Independent ISLAMU Design Choices

- Clean Architecture remains Application port to Infrastructure adapter; Photon types do
  not cross the Infrastructure boundary.
- Data Protection tokens carry the normalized provider-neutral selection instead of a
  browser-trusted record or a server-side provider cache.
- Readiness uses provider status, never a synthetic address query.
- Local-address governance, tenant isolation, HAL affordances, persistence, and failure
  fallback remain authoritative regardless of provider availability.

## Open Activation Evidence

The topology is approved, but a concrete production deployment remains disabled until an
operator supplies:

1. owned/contracted HTTPS endpoint and network owner;
2. pinned release/image digest and dataset manifest SHA-256;
3. capacity benchmark and 2x headroom evidence;
4. refresh, blue/green swap, rollback, and restore rehearsal;
5. OSM attribution placement and ODbL distribution classification;
6. `/status` readiness evidence and alert ownership.

Without all six, configuration validation must keep Photon unavailable and the application
continues in healthy `None` mode.
