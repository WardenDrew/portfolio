# UniFi Network Access Control

Sanitized source excerpt from a network access control and captive portal
system for UniFi wireless infrastructure.

## Architecture Summary

The system combines a Blazor captive portal, custom RADIUS authorization and
accounting daemons, DNS handling, EF Core persistence, and admin/user workflows.
Devices are mapped to users and networks, authorization can expire, and RADIUS
responses assign devices into either a registration VLAN or an approved network
VLAN.

The most important engineering work is below the UI: packet parsing and
serialization, daemon lifecycle boundaries, device/session persistence, and the
policy layer that translates device authorization into network assignment.

## What This Shows

- Custom RADIUS packet handling, message authenticators, response
  authenticators, and VLAN attributes.
- DNS packet parsing/serialization used by captive portal and certificate
  challenge workflows.
- Daemonized service design for web, RADIUS authorization, RADIUS accounting,
  and DNS processes.
- EF Core data model for users, devices, networks, network groups, device
  assignment, and user sessions.
- Admin and self-service flows for network/device authorization.

## Sample Map

- [RadiusAuthorizationDaemon.cs](src/CaptivePortal/Daemons/RadiusAuthorizationDaemon.cs)
  - access request handling, device lookup, registration VLAN fallback, and
  dynamic VLAN accept/reject responses.
- [RadiusPacket.cs](src/Radius/RadiusPacket.cs) - RADIUS packet model,
  serialization, authenticator calculation, and attribute handling.
- [RadiusAttributeParser.cs](src/Radius/RadiusAttributeParser.cs) - typed
  attribute parsing boundary.
- [DnsPacket.cs](src/DNS/DnsPacket.cs) - DNS packet parsing and serialization.
- [DnsDaemon.cs](src/CaptivePortal/Daemons/DnsDaemon.cs) - DNS daemon entry
  point for captive portal support.
- [PublicDnsChallengeProvider.cs](src/CaptivePortal/Services/Dns/PublicDnsChallengeProvider.cs)
  - DNS-01 style certificate challenge support.
- [IronNacDbContext.cs](src/CaptivePortal/Database/IronNacDbContext.cs) - EF
  Core model and sanitized seed behavior.
- [PortalUser.razor](src/CaptivePortal/Pages/Portal/PortalUser.razor) and
  [Devices.razor](src/CaptivePortal/Pages/Admin/Devices.razor) - representative
  user/admin workflows.

## Sanitization Notes

Network names, secrets, passwords, certificate details, production hostnames,
and deployment values are replaced with placeholders. The sample keeps the
protocol and authorization logic because that is the core portfolio value.
