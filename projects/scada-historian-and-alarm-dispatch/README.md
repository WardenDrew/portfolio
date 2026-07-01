# SCADA Historian And Alarm Dispatch

Public-safe visual sample from a historian and alarm dispatch system for
industrial monitoring workflows.

## Architecture Summary

The production system collected and presented operational telemetry, alarm
state, communication health, and field alert workflows. The public portfolio
does not include source code because the original project contains sensitive
operational details. Instead, this folder keeps diagrams and screenshots that
show the shape of the system without exposing customer infrastructure.

The relevant architecture is historian-oriented: tag values are collected over
time, communications and device health are surfaced to operators, dashboards
make state visible, and alarm dispatch paths support mission-critical response.

## What This Shows

- Industrial monitoring and historian design vocabulary.
- Operator-facing dashboard and alarm review thinking.
- Public-safe documentation of sensitive field operations software.
- Ability to communicate system architecture even when source release is not
  appropriate.

## Visual Evidence

- [overview.png](overview.png) - sanitized system overview diagram.
- [grafana-1.png](grafana-1.png) - sanitized dashboard screenshot.

## Sanitization Notes

Customer names, facility identifiers, network topology, tag names, alert
targets, and production source code are omitted. The visuals are retained as
architecture evidence only.
