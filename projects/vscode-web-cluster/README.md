# VS Code Web Cluster

Sanitized source excerpt from classroom infrastructure for browser-based
development environments.

## Architecture Summary

This project packaged code-server into repeatable student workspaces so a high
school programming class could practice C# development from Chromebooks. The
container image creates a student user at startup, assigns persistent project
and configuration volumes, and exposes the browser IDE through a predictable
port.

The value is pragmatic deployment design: make the development environment
browser-accessible, persistent, resettable, and simple enough for a classroom
where device control and local installation are constrained.

## What This Shows

- Containerized developer environment design for constrained hardware.
- Startup scripts that create isolated runtime users and assign workspace
  ownership.
- Compose-based test deployment for validating the image locally.
- Small operational scripts for build, start, stop, and cleanup workflows.

## Sample Map

- [Dockerfile](src/Dockerfile) - base image and classroom runtime package.
- [entrypoint.sh](src/startup/entrypoint.sh) - runtime user creation,
  password placeholder, and workspace ownership setup.
- [userentrypoint.sh](src/startup/userentrypoint.sh) - code-server startup
  under the student user.
- [compose.test.yml](src/compose.test.yml) - local test deployment with
  persistent config and project volumes.
- [build](src/build), [up](src/up), [down](src/down), and [clean](src/clean) -
  supporting operational scripts.

## Sanitization Notes

Default credentials have been neutralized, and classroom-specific deployment
details are omitted. The sample is intended to show infrastructure shape, not a
production-ready public hosting configuration.
