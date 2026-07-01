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
full production repositories cannot be shared. Each project folder has a main
`README.md` that explains the architecture, the public sample, and what was
removed for confidentiality.

## Review Guide

- Start with the project README for context, then inspect the linked source or
  visual artifacts inside that project.
- Source samples are curated evidence, not always complete standalone builds.
  They are selected to show architecture, custom logic, protocol work, security
  boundaries, and operational engineering.
- Placeholder values such as `CHANGE_ME`, `REDACTED`, `REPLACE_WITH_*`, and
  reserved example phone numbers are intentional.
- Client names, production credentials, private hostnames, live deployment
  topology, customer data, operational phone rosters, and proprietary business
  records are omitted or generalized.

## Experience Alignment

This portfolio demonstrates the through-lines from my professional experience. I take ambiguous or complex problems and break them apart into practical systems and solutions.

- IronWatch IT consulting and cybersecurity: founded and operate an IT
  consulting practice focused on cybersecurity, disaster recovery, monitoring,
  infrastructure improvement, technology strategy, and tailor-fit software
  integrations.
- Ranes safety and risk-management platform modernization: backend service
  architecture, DevOps practices, secure/high-availability service design,
  containerized microservices transitions, grounding AI tools for
  risk-sensitive clients, and maintainable systems built from early-stage
  product ideas.
- Devmatics cloud-first product engineering: .NET APIs, Blazor apps, WPF
  applications, Vue user experiences, payments, reporting, diagnostics,
  CI/testing pipelines, automated deployment workflows, Kubernetes, and
  monolith-to-service decomposition.
- Granite Creek Energy and industrial IT: on-premise-to-cloud migration,
  vendor-system integrations, Layer 3 network operations, monitoring, security
  oversight, and SCADA application support including Rockwell FactoryTalk
  environments.
- Church on the Hill facilities, engineering, and campus infrastructure: public
  drinking water small-system infrastructure/reporting, process
  troubleshooting, access control, alarms, campus automation, structured
  cabling, data center design, wireless coverage, VoIP, segmented networks,
  Hyper-V, Active Directory, RADIUS, remote access, VPN, network monitoring,
  and content filtering.

## Architecture Signature

The work in this portfolio is deliberately cross-domain. The common thread is
turning messy operational constraints into software systems that can be
deployed, maintained, secured, and explained.

- Backend platform design in .NET and TypeScript: APIs, workers, persistence,
  authentication, authorization, background jobs, data migration, and service
  integration.
- Security-aware systems: custom token handling, password hash migration,
  generated permissions, ACL materialization, encrypted records, key wrapping,
  request redaction, and secrets boundaries.
- Protocol and integration work: Modbus-style collection, Influx line protocol,
  RADIUS, DNS, Asterisk AMI, SMTP, Twilio Functions, AWS SES, gRPC, object
  storage, and browser-based cryptography.
- Frontend and workflow implementation in Quasar, Vue, Blazor, terminal UIs,
  dashboards, admin tools, and constrained classroom/deployment surfaces.
- DevOps and runtime packaging: Docker Compose, containerized development
  environments, reverse-proxy-aware frontend builds, repeatable local stacks,
  deployment scripts, and agent-ready repository templates.
- Physical and operational systems: campus networks, access control, alarm and
  notification workflows, facilities engineering, water-system compliance, and
  project management across technical and non-technical stakeholders.

## Featured Projects

### Industrial And Operational Monitoring

**[Industrial SCADA Demo Stack](industrial-scada-demo)**

Split local/central SCADA-style architecture with .NET services, site check-ins,
local tag collection, Modbus-style reads, Influx line protocol writes, and
dashboard-oriented coordination.

**[SCADA Historian And Alarm Dispatch](scada-historian-and-alarm-dispatch)**

Public-safe visual sample from a historian and alarm dispatch system for tag
trends, communication health, operator dashboards, and field alert workflows.

### Networking, Telephony, And Messaging

**[UniFi Network Access Control](unifi-network-access-control)**

Dynamic VLAN assignment and captive portal system with custom RADIUS/DNS packet
handling, daemonized services, EF Core persistence, device authorization, and
network administration workflows.

**[FreePBX / Asterisk Scheduling Tools](freepbx-asterisk-tools)**

Bell and page scheduling tool layered over FreePBX/Asterisk with a Blazor admin
UI, override calendars, recurring dispatch logic, and a custom AMI client.

**[Voicemail Email Transcription](voicemail-email-transcription)**

Headless SMTP worker that accepts voicemail emails, extracts message content,
and supports transcript-oriented response workflows.

**[Emergency Callout / Twilio Functions](twilio-callout)**

Serverless emergency callout flow using Twilio Functions for inbound calls,
outbound responder dialing, conference join control, and sanitized roster
configuration.

### Secure Business Platforms

**[Enterprise Safety Platform Modernization](enterprise-safety-platform)**

Backend-focused sample from a large safety platform. Highlights custom JWT
handling, request-scoped auth, Argon2 password migration, generated permission
trees, ACL compilation, asset-service integration, redacted request logging,
feature flag signing, push batching, planner normalization, and timeclock review
logic.

**[Encrypted Workspace Prototype](encrypted-workspace-prototype)**

End-to-end encrypted workspace prototype with ASP.NET Core, Quasar/Vue,
OPAQUE-style password flows, encrypted project messages, wrapped key records,
and operator recovery workflows.

**[Secure Offline Platform Product Prototype](secure-offline-platform)**

TypeScript/Quasar prototype with reusable domain packages, versioned
persistence, project-scoped business policies, browser key wrapping, and queued
offline synchronization.

**[Unnamed HRIS Platform](unnamed-hris-project)**

Sanitized visual and architecture sample from a sensitive HRIS platform:
database design, authentication/authorization, DevOps infrastructure, API work,
encryption design, admin workflows, and protected employee-data surfaces.

### Developer Tooling, Education, And Libraries

**[VS Code Web Cluster](vscode-web-cluster)**

Containerized browser-based development environment for classrooms where
students needed persistent C# workspaces from Chromebooks.

**[IronWatch.MediatR.MinimalEndpoints](minimal-endpoints)**

.NET helper library that maps MediatR request handlers into ASP.NET Core
minimal API endpoints using attribute-driven route, form, and response metadata.

**[Agent Framework And Devworkspace Templates](agent-framework)**

Agent-ready repository templates and workflow examples that combine CI routing,
automation conventions, prompts, runbooks, and TypeScript service scaffolds.

### Product Design And Personal Experiments

**[Amazon SES Email Aggregator](amazon-ses-email-aggregator)**

.NET API and background worker for queued outbound email through Amazon SES,
including API key middleware, EF Core persistence, command-line startup verbs,
retry limits, and SES throttling backoff.

**[Unnamed Crypto Token System Design](unnamed-crypto-project)**

Redacted system design and scope artifact for an EVM-compatible,
collateral-backed token product with vaults, DAO governance, redemption flows,
and frontend requirements.

**[Unified Communication Prototypes](unified-communication-prototype)**

Provider-oriented communication abstraction with terminal UI prototypes for
sessions, rooms, messages, login continuation, pagination, and send flows.

**[WMMO Rendering Experiment](wmmo)**

Small Silk.NET/OpenGL experiment showing window setup, input hooks, shaders,
vertex buffers, and a basic render loop.

**[Non-Coding Projects](non-coding-projects)**

Facilities, engineering, project-management, and IT work that does not fit a
source-code sample, including Church on the Hill campus infrastructure and
water-system work plus Granite Creek Energy IT/project delivery.

## Technology Range

- Languages and platforms: C#, .NET, ASP.NET Core, TypeScript, JavaScript,
  Vue, Quasar, Blazor, SQL, shell scripting, Silk.NET/OpenGL.
- Data and infrastructure: PostgreSQL, MySQL, MongoDB-style persistence,
  InfluxDB, EF Core, MinIO/object storage, Docker, Docker Compose, reverse
  proxies, containerized development environments.
- Integration domains: industrial telemetry, network access control, PBX/page
  systems, email ingestion, voice callout, encrypted collaboration, HRIS/safety
  workflows, classroom development infrastructure, and agent-enabled tooling.
- Operations and facilities: Layer 3 networks, SCADA support, firewalling,
  monitoring, backup/disaster recovery, access control, alarms, VoIP, campus
  wireless, public water-system reporting, and physical infrastructure
  coordination.

## What This Portfolio Is Meant To Show

- I can design across backend services, frontend workflows, data models,
  security boundaries, protocol integrations, facilities constraints, and
  deployment realities.
- I can work inside domains that require custom engineering rather than only
  assembling off-the-shelf SaaS tools.
- I can lead projects that span software, IT operations, industrial systems,
  facilities, vendors, executives, and end users.
- I can sanitize and communicate complex systems clearly enough for human
  reviewers and AI-assisted hiring workflows without exposing private data.
