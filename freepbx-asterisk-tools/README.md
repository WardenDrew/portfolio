# FreePBX / Asterisk Scheduling Tools

Sanitized source excerpt from a scheduling and dispatch tool built around
FreePBX/Asterisk page groups.

## Architecture Summary

The tool fills a gap where native PBX scheduling was not flexible enough for
real operational use. A Blazor admin UI manages schedules, day plans, page
groups, and date-specific overrides. A hosted background worker checks the
active plan once per minute, resolves overrides, and dispatches pages through
an Asterisk Manager Interface client.

The interesting boundary is the PBX integration. The application owns the
scheduling model and idempotent event loop, then translates selected page-group
events into AMI commands against the existing phone system.

## What This Shows

- Operational workflow design around schedules, overrides, and page groups.
- Long-running .NET worker logic that avoids duplicate minute-level dispatches.
- Custom AMI protocol client with request/response correlation.
- Blazor administration pages for a small but complete internal tool.

## Sample Map

- [AmiClient.cs](src/FreePbxTools.Common/AmiClient.cs) - TCP AMI client,
  message serialization, login/logoff, and ActionID response validation.
- [PageBackgroundWorker.cs](src/FreePbxTools.Web/Services/PageBackgroundWorker.cs)
  - schedule resolution, override handling, and recurring dispatch loop.
- [PagingService.cs](src/FreePbxTools.Web/Services/PagingService.cs) - AMI
  paging workflow boundary.
- [SettingsService.cs](src/FreePbxTools.Web/Services/SettingsService.cs) -
  persisted schedule, plan, page group, and override settings.
- [Schedules.razor](src/FreePbxTools.Web/Components/Pages/Schedules.razor) and
  [Overrides.razor](src/FreePbxTools.Web/Components/Pages/Overrides.razor) -
  representative Blazor management surfaces.

## Sanitization Notes

PBX hostnames, AMI credentials, page group identifiers, organization-specific
labels, and production settings are neutralized. Third-party static web assets
are included only where they were already part of the source excerpt.
