# Secure Offline Platform Product Prototype

Sanitized source excerpt from a TypeScript/Quasar product prototype with shared
offline-safe data packages.

## Architecture Summary

The product was structured around reusable domain packages instead of keeping
all policy inside the frontend or API. Versioned repositories enforce schema
migration, validation, and optimistic concurrency; business policies evaluate
project-scoped encrypted events; the frontend handles browser-side key wrapping
and queued offline synchronization.

This sample is intended to show architecture across layers: data core,
business policy, browser crypto, and synchronization orchestration.

## What This Shows

- Shared data-core packages instead of duplicated frontend/API policy.
- Versioned repository behavior with migration, validation, and optimistic
  concurrency.
- Browser-side key wrapping and encrypted-event policy evaluation.
- Offline synchronization orchestration for queued local work.

## Sample Map

- [repository.ts](src/data-core/core/repository.ts)
- [timeclock-entry-policy.ts](src/business-core/entities/timeclock-entry/timeclock-entry-policy.ts)
- [key-wrapping.ts](src/frontend/services/browser-crypto/key-wrapping.ts)
- [runner.ts](src/frontend/services/offline-sync/runner.ts)

## Sanitization Notes

Runtime configuration, deployment metadata, client context, database data, and
environment-specific settings are omitted.
