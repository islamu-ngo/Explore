<!-- ABOUTME: Reserves the read-only host directory mounted for configuration bootstrap manifests. -->
<!-- ABOUTME: Directs operators to the canonical schema and self-hosting recovery procedure. -->

# Configuration Manifest Mount

Place `configuration-manifest.json` in this directory only when
`CONFIGURATION_MANIFEST_MODE` is `ValidateOnly` or `Bootstrap`.

Validate it against
`schemas/configuration-manifest-v1alpha1.schema.json`, keep the directory
searchable by the container's non-root user, and keep the manifest file
read-only. See `docs/SELF_HOSTING.md` for deployment and recovery procedures.
