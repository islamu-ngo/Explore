ABOUTME: Documents S3-compatible object storage configuration, API boundaries, and operational impact.
ABOUTME: Separates implemented object flows from UI/client gaps so operators do not over-assume behavior.

# Storage

> **Audience:** Operators | Admins | Contributors
> **Status:** Mixed
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-29
> **Source Anchors:** `Explore.Infrastructure/Storage/`, `Explore.Infrastructure/Services/ObjectStorageService.cs`, `Explore.API/Controllers/StorageObjectController.cs`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`

Storage uses S3-compatible object storage for uploaded assets and metadata-backed `StorageObject` records for application discovery.

## What Is Implemented

| Area | Implemented Behavior |
|---|---|
| Runtime storage | Provider-neutral upload sessions and metadata-backed reads are implemented for local-first storage; `ObjectStorageService` remains only for legacy S3-compatible presigned flows. |
| Configuration resolution | `S3ConfigResolver` resolves tenant-aware settings from persisted settings first, then `IConfiguration`, with a five-minute cache. |
| Public reads | `StorageObjectController` streams file content by `StorageObject.Id`; public images use the same metadata reader with public-image visibility checks. Caller-supplied object-key read and presign routes are removed. |
| Authenticated writes | Upload URL generation requires authentication; `StorageObject` create/update/delete operations require authentication and `storage_object` resource authorization. |
| Admin settings | Instance admins can read, update, and test storage settings through instance settings endpoints and the admin UI. |
| Local self-hosting | Docker Compose can run MinIO through the optional `storage` profile. |

The API delete endpoint exists, but `Explore.Blazor.Client/Services/ImageStorageService.cs` still returns `false` from `DeleteImageAsync`. Do not document a completed Blazor client delete flow until that helper is implemented.

## Configuration

Use the current `S3Settings` naming family for runtime configuration.

| Purpose | Key Shape | Notes |
|---|---|---|
| .NET configuration | `S3Settings:*` | Bound by runtime configuration. |
| Environment variables | `S3Settings__*` | Double-underscore form for .NET configuration providers. |
| Persisted settings | `s3.endpoint`, `s3.public_endpoint`, `s3.bucket_name`, `s3.region`, `s3.force_path_style`, `s3.upload_url_expiration_minutes` | Defined by storage setting definitions and surfaced through admin settings. |
| Secrets | `s3.access_key_id`, `s3.secret_access_key` | Sensitive values; see [SECRETS.md](SECRETS.md). |
| Tenant delegation | `governance.lock_tenant_storage` | Controls whether tenant-level storage overrides are locked. |

For external secret providers, keep the naming distinction from [SECRETS.md](SECRETS.md): provider-side storage names map into runtime `S3Settings:*` values. Do not introduce new `Storage__*` examples unless the source code changes.

## Upload And Download Flow

1. Browser callers ask the Blazor BFF for an upload session with filename, content type, and expected byte count.
2. The BFF calls the provider-neutral API upload-session endpoint. The API resolves tenant policy, max upload size, provider, quota, and reservation state.
3. The BFF stores only the API upload-session id, owner, content type, expected size, and expiry in distributed cache, then returns an opaque `uploadSessionId` to the browser.
4. Browser uploads send `uploadSessionId`, `contentType`, and `file` to `/bff/storage/upload-proxy`; the proxy rejects raw destination fields, enforces session owner/content type/exact size, and streams bytes to `/api/storageobject/upload-sessions/{id}/content`.
5. Server/non-browser legacy paths may still upload directly to a trusted presigned URL generated for that request while older S3-compatible flows remain.
6. The application stores or updates `StorageObject` metadata through authenticated write endpoints.
7. Readers use metadata-backed content endpoints (`/api/storageobject/{id}/content`), public image endpoints (`/api/storageobject/{id}/public`), or the ID-based S3-compatible presigned download endpoint depending on the caller path.

`GetFileStream` translates S3 404 responses into `KeyNotFoundException`, which is useful when diagnosing broken metadata-to-object references.

Direct object-key read routes are not part of the local-first contract. The removed `file/{fileKey}` and `presigned-url-by-key/{objectKey}` endpoints bypassed metadata visibility and owner checks; clients must carry a `StorageObject.Id` instead of raw provider keys.

## API Surface

| Endpoint Group | Authentication Boundary |
|---|---|
| List/read storage objects | Public read behavior is implemented in `StorageObjectController`. |
| Get file/public image/presigned download URL | Public file and image reads resolve by `StorageObject.Id`; arbitrary object-key read/presign endpoints are removed. |
| Generate upload URL | Requires authentication. |
| Create/update/delete storage object metadata | Requires authentication and `storage_object` resource authorization. |
| Instance storage settings read/update/test | Instance-admin/setup-secret boundaries are handled by instance settings endpoints. |

HATEOAS policies mark create/delete affordances as authenticated operations. Treat link presence as an authorization hint, not as a replacement for server-side enforcement.

## Backup And Restore Impact

If the `storage` profile is enabled or an external S3 bucket is used, object storage is part of the backup set.

- Back up object data (`minio_data` locally, or provider-native bucket backup/sync externally).
- Back up storage secrets and environment configuration with the same release manifest as the database backup.
- Restore object storage before reopening user traffic, then verify representative object metadata resolves to actual objects.
- During rollback, verify the application version still understands the stored `StorageObject` metadata and key layout.

See [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) for the full operational runbook.

## Troubleshooting

| Symptom | First Checks |
|---|---|
| Upload URL generation fails | Confirm `S3Settings:*` or persisted S3 settings include endpoint, bucket, region, and credentials. |
| Reads return missing-object behavior | Check whether `StorageObject` metadata points to an object key that exists in the bucket. |
| Settings update appears ignored | Wait for the resolver cache window or trigger the settings path that invalidates `S3ConfigResolver`; cache invalidation is tenant-scoped when a tenant id is available. |
| Local MinIO unavailable | Confirm Docker Compose was started with `--profile storage` and that bucket initialization completed. |
| Tenant override confusion | Check `governance.lock_tenant_storage` and whether the runtime is single-tenant or multi-tenant. |

The storage connection test uses `ObjectStorageService.TestConnectionAsync`, which attempts an S3 bucket listing through the resolved configuration.

## Related Documentation

- [CONFIGURATION.md](CONFIGURATION.md) - runtime configuration keys.
- [SECRETS.md](SECRETS.md) - secret-provider naming and sensitive value handling.
- [SELF_HOSTING.md](SELF_HOSTING.md) - Compose `storage` profile and MinIO ports.
- [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) - object storage backup and restore runbook.
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - symptom-first operator triage.
