# Voicemail Email Transcription

Sanitized source excerpt from a headless SMTP worker that receives voicemail
emails, processes attachments, and emits transcript-oriented responses.

## Architecture Summary

The service is built around an internal SMTP listener instead of a polling
mailbox. That keeps the integration simple for PBX-style voicemail systems:
voicemail email arrives at the worker, the message store extracts MIME content,
and downstream processing can transcribe audio and send a reply.

The public sample focuses on service hosting, SMTP session handling, and
message-store boundaries rather than provider credentials or production routing.

## What This Shows

- Mail-first integration where SMTP is the ingestion boundary.
- Worker-hosted service design for headless operational tooling.
- MIME/message-store separation before downstream transcription work.
- A small test SMTP sender for exercising the service boundary.

## Sample Map

- [Program.cs](src/Server/Program.cs)
- [SmtpBackgroundService.cs](src/Server/SmtpBackgroundService.cs)
- [ConsoleMessageStore.cs](src/Server/ConsoleMessageStore.cs)
- [TestSmtp Program.cs](src/TestSmtp/Program.cs)

## Sanitization Notes

The SMTP banner was neutralized, and production mail routing, API keys, audio
files, and real voicemail payloads are omitted.
