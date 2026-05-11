ABOUTME: Documents S3-compatible object storage configuration, API boundaries, and operational impact.
ABOUTME: Separates implemented object flows from UI/client gaps so operators do not over-assume behavior.

# Storage

> **Audience:** Operators | Admins | Contributors
> **Status:** Mixed
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.Infrastructure/Storage/`, `Explore.Infrastructure/Services/ObjectStorageService.cs`, `Explore.API/Controllers/StorageObjectController.cs`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`

Storage uses S3-compatible object storage for uploaded assets and metadata-backed `StorageObject` records for application discovery.

## What Is Implemented

| Area | Implemented Behavior |
|---|---|
| Runtime storage | `ObjectStorageService` generates presigned upload/download URLs, streams files, and tests S3 connectivity. |
| Configuration resolution | `S3ConfigResolver` resolves tenant-aware settings from persisted settings first, then `IConfiguration`, with a five-minute cache. |
| Public reads | `StorageObjectController` exposes public read and presigned-read endpoints for storage objects and object keys. |
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

1. An authenticated caller asks the API for a presigned upload URL, or a browser caller asks the Blazor BFF for an upload session.
2. `ObjectStorageService.GeneratePresignedUploadUrl` creates an object key under `uploads/` with a timestamp, unique id, sanitized name, and original extension.
3. Browser uploads use `/bff/storage/upload-session` first. The BFF stores the exact server-issued destination in distributed cache and returns an opaque `uploadSessionId` instead of asking the browser to trust or replay a raw upload URL.
4. Browser uploads send `uploadSessionId`, `contentType`, and `file` to `/bff/storage/upload-proxy`; the proxy resolves the server-approved destination and rejects arbitrary client-supplied URLs.
5. Server/non-browser upload paths may still upload directly to a trusted presigned URL generated for that request.
6. The application stores or updates `StorageObject` metadata through authenticated write endpoints.
7. Readers use public storage object endpoints, public image endpoints, or presigned download URL endpoints depending on the caller path.

`GetFileStream` translates S3 404 responses into `KeyNotFoundException`, which is useful when diagnosing broken metadata-to-object references.

## API Surface

| Endpoint Group | Authentication Boundary |
|---|---|
| List/read storage objects | Public read behavior is implemented in `StorageObjectController`. |
| Get file/public image/presigned download URL | Public read behavior is implemented for object delivery paths. |
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
