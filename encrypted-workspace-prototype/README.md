# Encrypted Workspace Prototype

Sanitized source excerpt from an end-to-end encrypted workspace prototype.

## Architecture Summary

The system combines an ASP.NET Core API, a Quasar/Vue frontend, encrypted
project records, OPAQUE-style password flows, and operator recovery paths. The
important architectural boundary is that project content is treated as
application-level encrypted data, while the API coordinates access, membership,
wrapped keys, and encrypted message envelopes.

The public sample highlights the pieces that make that model concrete:
messaging services, key-unlock workflows, browser registration crypto, and an
OPAQUE server component.

## What This Shows

- Application-level encryption modeled as part of project membership and
  message workflows.
- Wrapped-key access and unlock flows instead of plain server-side content
  ownership.
- Browser registration crypto paired with backend OPAQUE-style account
  handling.
- Recovery-path thinking for operators without making project content a normal
  plaintext API concern.

## Sample Map

- [ProjectMessagingService.cs](src/api/services/ProjectMessagingService.cs)
- [KeyUnlockService.cs](src/api/services/KeyUnlockService.cs)
- [registration-crypto.ts](src/app/lib/registration-crypto.ts)
- [OpaqueServer.cs](src/opaque-dotnet/OpaqueServer.cs)

## Sanitization Notes

Deployment files, secrets, database configuration, and any project data are
omitted. Cryptographic terminology remains because it is part of the code's
domain model, not a leaked secret.
