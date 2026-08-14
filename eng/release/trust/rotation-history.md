<!-- ABOUTME: Records the append-only signer rotation and revocation contract for release operations. -->
<!-- ABOUTME: States the current activation blockers without inventing production principals or custody. -->

# Release Signer Rotation History

## Current state

No signer entry is currently bundled. Activation is blocked until independent
reviewers approve real principals, unique public keys, custody owners, validity
windows, revocation contacts, and the promoted artifact store. Private keys never
belong in this repository or a trusted bundle.

## Rotation and revocation contract

- A new key receives a new unique principal and fingerprint. An overlap window is
  allowed only for distinct keys and recorded validity periods.
- Revocation is effective on and after its recorded policy date. Remove the key from
  `allowed-signers`, append the reason and replacement here without restricted
  details, promote a new trusted bundle, and retain the prior bundle as evidence.
- A tag is an immutable Git object. Replacing or recreating a tag fails even when its
  commit target and signer are unchanged; operators must investigate and issue a new
  version rather than accepting the moved name.

## Entries

| Principal | Role | Validity | Status | Disposition |
|---|---|---|---|---|
| _none_ | _none_ | _none_ | blocked | `allowed-signers` remains comment-only until reviewed production keys are promoted |
