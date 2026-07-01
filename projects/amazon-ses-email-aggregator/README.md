# Amazon SES Email Aggregator

Sanitized source excerpt from a .NET API and background worker that queues
outbound email and dispatches it through Amazon SES.

## Architecture Summary

The system separates message intake from provider delivery. API requests create
database-backed message records, while a hosted background worker polls unsent
messages in batches, sends them through SES, records attempts, handles
retryable failures, and applies a throttling backoff when AWS indicates rate
limiting.

The project also uses command-line startup verbs so the same executable can
host the service, run migrations, or build migration artifacts. That keeps
operational entry points explicit without needing separate small utilities.

## What This Shows

- Worker-oriented backend design with durable queue state in the application
  database.
- SES integration with retry, permanent failure, and provider throttling
  handling.
- API key middleware and environment-driven configuration boundaries.
- EF Core persistence, migrations, and command-line service operations.

## Sample Map

- [Program.cs](src/Program.cs) - command-line verb dispatch into service modes.
- [Orchestrator.cs](src/Services/Orchestrator.cs) - batch polling, retry state,
  permanent failure handling, and throttling backoff.
- [SESDispatchService.cs](src/Services/SESDispatchService.cs) - Amazon SES
  request creation and provider exception classification.
- [ApiKeyMiddleware.cs](src/Middleware/ApiKeyMiddleware.cs) - lightweight API
  key boundary for message intake.
- [Message.cs](src/Data/Entities/Message.cs) - persisted message state used by
  the worker.
- [MigrateVerb.cs](src/StartupVerbs/MigrateVerb.cs) - operational migration
  entry point.

## Sanitization Notes

SES credentials, sender identities, deployment configuration, private database
connection strings, and real message content are omitted or replaced with
placeholders. The included source is a public-safe excerpt rather than a full
production repository.
