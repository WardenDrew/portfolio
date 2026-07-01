# Industrial SCADA Demo Stack

Sanitized source excerpt from a multi-service SCADA demonstration stack.

## Architecture Summary

This project models a split local/central industrial monitoring system. A
site-local collector reads field tags, normalizes samples, and writes historian
data, while a central API receives site check-ins and supports dashboard
workflows. The architecture separates real-time edge collection from central
coordination so the system can keep collecting locally even when upstream
connectivity is unreliable.

The public sample highlights the integration boundaries: Modbus-style device
access, Influx line protocol writing, long-running worker orchestration, and
central site heartbeat handling.

## What This Shows

- Edge collection separated from central coordination.
- Long-running worker design for local telemetry collection.
- Protocol-specific boundaries for Modbus-style reads and Influx line protocol
  writes.
- Site health and check-in workflows for central visibility.

## Sample Map

- [TagCollectorWorker.cs](src/local-tagcollector/Services/TagCollectorWorker.cs)
- [ModbusTcpDeviceClient.cs](src/local-tagcollector/Modbus/ModbusTcpDeviceClient.cs)
- [InfluxLineProtocolWriter.cs](src/local-tagcollector/Influx/InfluxLineProtocolWriter.cs)
- [SiteCheckInService.cs](src/central-api/Features/Sites/SiteCheckInService.cs)

## Sanitization Notes

Private deployment configuration, database credentials, Influx tokens, customer
network details, and Compose files are intentionally omitted.
