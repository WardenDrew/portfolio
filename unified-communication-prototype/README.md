# Unified Communication Prototypes

Sanitized source excerpt from provider-oriented communication prototypes.

## Architecture Summary

The prototype explores a common abstraction over communication providers:
setup, login continuation, rooms, messages, pagination, and plain-text send
operations. The terminal UI sample sits on top of those provider contracts,
which keeps interaction code separate from provider-specific session mechanics.

The public sample shows the interface boundary and command parsing/application
shell pieces that make multiple providers feel consistent to a caller.

## What This Shows

- Provider abstraction design for communication services with different login
  and message models.
- Terminal UI structure kept separate from provider-specific sessions.
- Command parsing and application shell code for a prototype interface.

## Sample Map

- [IUnifiedCommunicationProvider.cs](src/WUnicom.Common/Abstractions/IUnifiedCommunicationProvider.cs)
- [WUnicomApplication.cs](src/WUnicom.Tui/WUnicomApplication.cs)
- [CommandParsing.cs](src/WUnicom.Tui/Commands/CommandParsing.cs)
- [IWuniProvider.cs](src/Wuni.Common/Contracts/IWuniProvider.cs)

## Sanitization Notes

Prototype credentials, local provider configuration, and generated build output
are omitted.
