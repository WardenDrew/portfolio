# WMMO Rendering Experiment

Sanitized source excerpt from a small Silk.NET/OpenGL rendering experiment.

## Architecture Summary

This project is a low-level graphics exploration rather than a product system.
It demonstrates window creation, input hooks, OpenGL context setup, vertex
buffer management, shaders, and a basic render loop. It is included to show
systems-level range beyond web and service architecture.

## What This Shows

- Direct rendering setup outside typical web/application frameworks.
- OpenGL context, shader, vertex buffer, and render-loop fundamentals.
- Willingness to work below business-application abstraction layers when the
  project calls for it.

## Sample Map

- [Program.cs](src/Program.cs)
- [wmmo.csproj](src/wmmo.csproj)

## Sanitization Notes

Only the minimal source needed to understand the rendering experiment is
included. Build output and local IDE files are omitted.
