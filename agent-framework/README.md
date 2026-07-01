# Agent Framework And Devworkspace Templates

Sanitized source excerpt from reusable agent-enabled development workspace
templates and automation patterns.

## Architecture Summary

The framework combines repository templates, explicit workflow routing, and
service scaffolds so autonomous or semi-autonomous agents can work inside
repeatable project boundaries. The TypeScript REST template gives generated
services a small router/auth/http core, while the workflow example shows how a
mentioned-agent dispatch pattern can route repository events to a selected
automation worker.

This positions the work as platform architecture: development environments,
agent operating rules, CI entry points, and application templates are designed
together.

## What This Shows

- Agent workflow design that routes mentioned repository events to a selected
  automation worker.
- Repeatable TypeScript service scaffolding with router, HTTP, and auth
  boundaries.
- Development workspace thinking that treats prompts, skills, workflows, and
  source templates as one operational surface.

## Sample Map

- [router.ts](src/templates/typescript-rest-api/core/router.ts)
- [http.ts](src/templates/typescript-rest-api/core/http.ts)
- [jwt.ts](src/templates/typescript-rest-api/infrastructure/auth/jwt.ts)
- [mentioned-agent.example.yml](src/workflows/mentioned-agent.example.yml)

## Sanitization Notes

Internal repository names, real workflow secrets, private runner details, and
agent run logs are omitted. The workflow file uses symbolic secret and variable
references only.
