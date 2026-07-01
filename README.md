# Andrew Haskell Portfolio / CV

LinkedIn: [Andrew Haskell](https://www.linkedin.com/in/andrew-haskell-0a0998163)

Multidisciplinarian Solutions Architect with 11 years in IT and cybersecurity,
7 years in software engineering, and 5 years in facilities and physical
infrastructure. Sector experience spans education, real estate, legal,
construction, oil and gas, life safety, and risk management.

My strongest work sits where software, infrastructure, security, facilities,
and operations meet: secure backend platforms, cloud and network migrations,
industrial and field systems, telephony automation, encrypted business
workflows, developer tooling, campus infrastructure, and deployment
infrastructure.

This repository is a sanitized portfolio. It combines public-safe source
excerpts, screenshots, diagrams, and design artifacts from projects where the
full production repositories cannot be shared. Coding projects live under
[`projects`](projects/). Each project folder has a main `README.md` that explains
the architecture, the public sample, and what was removed for confidentiality.

## What This Portfolio Is Meant To Show

- I can design across backend services, frontend workflows, data models,
  security boundaries, protocol integrations, facilities constraints, and
  deployment realities.
- I can work inside domains that require custom engineering rather than only
  assembling off-the-shelf SaaS tools.
- I can lead projects that span software, IT operations, industrial systems,
  facilities, vendors, executives, and end users.

## Skills Range

This portfolio demonstrates the through-lines from my professional experience.
I take ambiguous or complex problems and break them apart into practical systems
and solutions.

- Languages and platforms: `C#`, `.NET`, `ASP.NET Core`, `TypeScript`, `JavaScript`,
  `Vue`, `Quasar`, `Blazor`, `SQL`, `shell scripting`, `Silk.NET/OpenGL`.
- Data and infrastructure: `PostgreSQL`, `MySQL`, `MongoDB`,
  `InfluxDB`, `EF Core`, `MinIO`, `Docker`, `IaaS`, `Kubernetes`.
- Integration domains: `industrial telemetry`, `network access control`, `PBX/page
  systems`, `email ingestion`, `voice callout`, `encrypted collaboration`, `HRIS/safety
  workflows`, `classroom development infrastructure`, and `agent-enabled tooling`.
- Enterprise IT operations: `Server administration`, `Traditional AD Domains`, `network management`, `firewall management`, `endpoint support`,
  `identity`, `remote access`, `Microsoft 365`, `AWS`, `backups`, `monitoring`,
  `vulnerability assessment`, `incident triage`, and `infrastructure support`.
- CIO consulting: `cybersecurity strategy`,
  `disaster recovery planning`, `business continuity`, `infrastructure roadmaps`,
  `vendor evaluation`, `technology upgrades`, `proactive monitoring`, and
  `client-facing technical consulting`.
- Operations and facilities: `SCADA`, `access control`, `alarms`, `VoIP`, `campus
  wireless`, `facilities management`, and `project coordination`.

## Coding Projects

### Industrial And Operational Monitoring

**[Industrial SCADA Demo Stack](projects/industrial-scada-demo)**

Split local/central SCADA-style architecture with .NET services, site check-ins,
local tag collection, Modbus-style reads, Influx line protocol writes, and
dashboard-oriented coordination.

**[SCADA Historian And Alarm Dispatch](projects/scada-historian-and-alarm-dispatch)**

Public-safe visual sample from a historian and alarm dispatch system for tag
trends, communication health, operator dashboards, and field alert workflows.

### Networking, Telephony, And Messaging

**[UniFi Network Access Control](projects/unifi-network-access-control)**

Dynamic VLAN assignment and captive portal system with custom RADIUS/DNS packet
handling, daemonized services, EF Core persistence, device authorization, and
network administration workflows.

**[FreePBX / Asterisk Scheduling Tools](projects/freepbx-asterisk-tools)**

Bell and page scheduling tool layered over FreePBX/Asterisk with a Blazor admin
UI, override calendars, recurring dispatch logic, and a custom AMI client.

**[Voicemail Email Transcription](projects/voicemail-email-transcription)**

Headless SMTP worker that accepts voicemail emails, extracts message content,
and supports transcript-oriented response workflows.

**[Emergency Callout / Twilio Functions](projects/twilio-callout)**

Serverless emergency callout flow using Twilio Functions for inbound calls,
outbound responder dialing, conference join control, and sanitized roster
configuration.

### Secure Business Platforms

**[Enterprise Safety Platform Modernization](projects/enterprise-safety-platform)**

Backend-focused sample from a large safety platform. Highlights custom JWT
handling, request-scoped auth, Argon2 password migration, generated permission
trees, ACL compilation, asset-service integration, redacted request logging,
feature flag signing, push batching, planner normalization, and timeclock review
logic.

**[Encrypted Workspace Prototype](projects/encrypted-workspace-prototype)**

End-to-end encrypted workspace prototype with ASP.NET Core, Quasar/Vue,
OPAQUE-style password flows, encrypted project messages, wrapped key records,
and operator recovery workflows.

**[Secure Offline Platform Product Prototype](projects/secure-offline-platform)**

TypeScript/Quasar prototype with reusable domain packages, versioned
persistence, project-scoped business policies, browser key wrapping, and queued
offline synchronization.

**[Unnamed HRIS Platform](projects/unnamed-hris-project)**

Sanitized visual and architecture sample from a sensitive HRIS platform:
database design, authentication/authorization, DevOps infrastructure, API work,
encryption design, admin workflows, and protected employee-data surfaces.

### Developer Tooling, Education, And Libraries

**[VS Code Web Cluster](projects/vscode-web-cluster)**

Containerized browser-based development environment for classrooms where
students needed persistent C# workspaces from Chromebooks.

**[IronWatch.MediatR.MinimalEndpoints](projects/minimal-endpoints)**

.NET helper library that maps MediatR request handlers into ASP.NET Core
minimal API endpoints using attribute-driven route, form, and response metadata.

**[Agent Framework And Devworkspace Templates](projects/agent-framework)**

Agent-ready repository templates and workflow examples that combine CI routing,
automation conventions, prompts, runbooks, and TypeScript service scaffolds.

### Product Design And Personal Experiments

**[Amazon SES Email Aggregator](projects/amazon-ses-email-aggregator)**

.NET API and background worker for queued outbound email through Amazon SES,
including API key middleware, EF Core persistence, command-line startup verbs,
retry limits, and SES throttling backoff.

**[Unnamed Crypto Token System Design](projects/unnamed-crypto-project)**

Redacted system design and scope artifact for an EVM-compatible,
collateral-backed token product with vaults, DAO governance, redemption flows,
and frontend requirements.

**[Unified Communication Prototypes](projects/unified-communication-prototype)**

Provider-oriented communication abstraction with terminal UI prototypes for
sessions, rooms, messages, login continuation, pagination, and send flows.

**[WMMO Rendering Experiment](projects/wmmo)**

Small Silk.NET/OpenGL experiment showing window setup, input hooks, shaders,
vertex buffers, and a basic render loop.

## Non-Coding Projects

Facilities, engineering, project management, and IT operations work across
church, school, and industrial-energy environments. This work shows the
physical infrastructure, stakeholder coordination, and operational ownership
behind the software and systems work elsewhere in this portfolio.

### Church on the Hill Facilities And Engineering

Church on the Hill work included facilities, process engineering, IT
infrastructure, campus automation, and security systems across church and school
operations.

- Managed infrastructure and reporting requirements for a public drinking water
  small water system, with emphasis on process engineering, system design,
  troubleshooting, and correcting original design issues.
- Designed, implemented, and maintained access control systems, security
  infrastructure, alarms, notification systems, and campus-wide automation.
- Led a ground-up overhaul of church and school server/network infrastructure:
  core data center design, extensive cabling, campus-wide wireless, integrated
  VoIP, firewalls, enterprise switching, and segmented network architecture.
- Supported two campuses with roughly 400-500 concurrent users through Hyper-V,
  Active Directory, RADIUS, remote access, VPN, monitoring, content filtering,
  and other enterprise services.
- Worked directly with organizational leaders to identify technical blockers to
  growth, plan practical improvements, and carry those improvements through
  implementation and support.

### Granite Creek Energy Project Management And IT

Granite Creek Energy work combined project management, industrial IT,
cloud/network modernization, vendor integration, and operational security.

- Led migration of on-premise services to cloud infrastructure to improve
  scalability, reliability, and performance for critical systems.
- Designed and implemented vendor-system integrations to improve data flow
  between platforms and reduce manual coordination.
- Managed and optimized Layer 3 network infrastructure for reliable operations.
- Implemented network monitoring and security improvements to reduce downtime
  and protect sensitive operational data.
- Provided IT security oversight and supported policy/practice improvements
  around operational risk.
- Supported SCADA applications, including Rockwell FactoryTalk environments,
  with attention to reliability, security, and integration with broader IT
  infrastructure.
