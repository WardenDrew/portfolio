# IronWatch.MediatR.MinimalEndpoints

Sanitized source excerpt from a .NET helper library that maps MediatR request
handlers into ASP.NET Core minimal API endpoints.

## Architecture Summary

The library reduces repetitive API wiring by treating MediatR requests as the
source of endpoint metadata. Attributes describe route shape, form binding, and
response models; registration code discovers handlers and maps them into
minimal API routes consistently.

The sample shows the reflection/metadata boundary and a small example endpoint
using the helper.

## What This Shows

- Reflection-based endpoint discovery over MediatR request/handler types.
- Attribute-driven route, form-binding, and response metadata.
- Boilerplate reduction for ASP.NET Core APIs while keeping endpoint contracts
  visible in code.

## Sample Map

- [EndpointRegistrationExtensions.cs](src/EndpointRegistrationExtensions.cs)
- [ApiEndpointAttribute.cs](src/ApiEndpointAttribute.cs)
- [AsFormAttribute.cs](src/AsFormAttribute.cs)
- [ResponseModelAttribute.cs](src/ResponseModelAttribute.cs)
- [RegisteredEndpoint.cs](src/RegisteredEndpoint.cs)
- [PostForgeForm.cs](src/Example/Endpoints/PostForgeForm.cs)

## Sanitization Notes

The namespace is retained as project identity. Package publishing metadata,
build output, and unrelated example files are omitted.
