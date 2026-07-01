# Emergency Callout / Twilio Functions

Sanitized source excerpt from a serverless Twilio emergency callout flow.

## Architecture Summary

The callout flow uses small Twilio Function handlers instead of a resident
server. An inbound call can start a conference, the system dials configured
response agents, and accepted agents join the caller through conference control
URLs. The design keeps voice orchestration close to Twilio while treating the
agent roster as external configuration.

The public sample shows function-level call routing, TwiML generation,
conference setup, outbound dialing, and config signature checks.

## What This Shows

- Voice workflow orchestration with small serverless handlers.
- Separation between inbound call handling, outbound responder dialing, answer
  handling, and conference join control.
- Sanitized configuration patterns for operational phone rosters.
- TwiML generation and Twilio client usage at the function boundary.

## Sample Map

- [inbound.js](src/functions/inbound.js)
- [startcallout.js](src/functions/startcallout.js)
- [callagent.js](src/functions/callagent.js)
- [agentanswer.js](src/functions/agentanswer.js)
- [agentjoin.js](src/functions/agentjoin.js)
- [waiturl.js](src/functions/waiturl.js)
- [config.example.json](src/config.example.json)

## Sanitization Notes

The original phone roster, company-specific copy, and operational audio asset
are omitted. The included config file uses reserved example phone numbers.
