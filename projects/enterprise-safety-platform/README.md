# Enterprise Safety Platform Modernization

Sanitized backend source excerpt from a large safety platform modernization
effort.

## Architecture Summary

This sample focuses on backend architecture only. Frontend code is intentionally
excluded. The included files highlight custom security, identity,
authorization, domain workflow, code generation, request guardrails, and service
integration work rather than off-the-shelf CRUD plumbing.

The platform backend was split across shared .NET libraries, a safety API,
domain/data packages, a code generation utility, an asset microservice, and
protobuf service contracts. The most important architectural boundaries in the
sample are:

- request-scoped authentication that accepts bearer or cookie tokens;
- custom JWT key preparation, `kid` routing, algorithm checks, and typed token
  models;
- Argon2 password verification with migration from older identity hashes;
- scope and role modeling through generated constants and a fluent permission
  tree;
- ACL materialization into effective-user rows for faster authorization checks;
- signed feature flag and asset-access token workflows;
- request logging with secret/body redaction for sensitive auth routes;
- backend workflow logic for planners, timeclock review, asset upload/download,
  push notification batching, and validation.

This is a portfolio slice, not a full source release, because the original
project contains proprietary business logic and deployment details.

## Backend Architecture Notes

### Security Boundary

The shared backend library contains the request identity layer:

- `JwtSerializer` prepares configured signing/verifying keys, validates `kid`
  and algorithm consistency, and serializes typed token models.
- `EndpointAuthService` normalizes bearer-token and cookie-token access into a
  request-scoped authorization helper.
- `PlatformAuthenticationHandler` bridges the custom token model into ASP.NET
  Core authentication.
- `PlatformAuthorizationMiddlewareResultHandler` centralizes
  challenge/forbidden responses for the custom scheme.
- `Argon2PasswordHasher` supports stronger password hashing while still
  migrating successful legacy Identity hashes forward.

The interesting part is not that the system uses JWTs. The interesting part is
that the backend owns the token model, key routing, legacy transition path,
scope interpretation, cookie/header compatibility, and failure semantics.

### Authorization Model

The sample includes two complementary authorization layers.

The first is scope/role authorization. `Scope.implementation.cs` defines a
fluent scope builder with validation, suffix support, parent inheritance, and a
global lookup table. `ScopesGenerator`, `RolesGenerator`, and the `PTree` model
turn structured permission trees into generated constants such as `Modules.cs`
and `Roles.cs`.

The second is entity-level ACL authorization. `AccessControlList` can reference
users, groups, jobs, or whole-company access. `AclCompilerService` expands
those rules into `AccessControlListEffectiveUser` rows so read paths can avoid
recomputing nested membership rules for every protected object.

### Workflow Logic

The API excerpts emphasize backend logic that is specific to the product:

- `PostAssetUploadBegin` creates asset records and asks the asset service for
  upload URLs.
- `PostDownloadAsset` combines organization boundaries, operator bypass rules,
  asset-token access, thumbnail/canvas detection, and gRPC asset-provider
  calls.
- `PostTimeClockReview` enforces mutually exclusive review actions and
  company-scoped approval rules.
- `PlannerJsonProcessor` upgrades and normalizes planner schemas for task,
  hazard, inspection, and regulatory evidence structures.
- `PushService` batches Firebase multicast sends, strips HTML for notification
  display, and clears invalid device tokens.

### Service Integration

Assets are handled through a separate gRPC service. The protobuf contract
describes signed upload/download request workflows, and provider
implementations exist for both S3-style object storage and local disk
development. This keeps the safety API focused on authorization and metadata
while the asset service owns storage-specific request generation.

### Operational Guardrails

The backend includes practical guardrails that matter in production:

- `RequestLoggingMiddleware` redacts sensitive request bodies and auth headers
  before structured exception logging.
- `RequestValidationPipeline` turns FluentValidation failures into consistent
  API responses.
- `PagingExtensions` rejects sorting fields that expose password, security, or
  concurrency data.
- `FeatureFlagService` signs developer-only feature flag payloads instead of
  trusting raw client-controlled flags.

## What This Shows

- Custom identity and token handling beyond basic framework defaults.
- Security migration work that preserves legacy compatibility while moving
  password storage forward.
- Generated authorization constants and fluent permission-tree modeling.
- ACL compilation for entity-level permissions at scale.
- Backend workflows with product-specific validation and domain rules.
- Service boundaries between API metadata, object storage, protobuf contracts,
  and signed asset access.

## Included Source Samples

### Security And Identity

- [JwtSerializer.cs](src/backend/projects/libraries/common-dotnet/Jwt/JwtSerializer.cs)
- [EndpointAuthService.cs](src/backend/projects/libraries/common-dotnet/Auth/EndpointAuthService.cs)
- [PlatformAuthenticationHandler.cs](src/backend/projects/libraries/common-dotnet/Auth/PlatformAuthenticationHandler.cs)
- [PlatformAuthorizationMiddlewareResultHandler.cs](src/backend/projects/libraries/common-dotnet/Auth/PlatformAuthorizationMiddlewareResultHandler.cs)
- [Argon2PasswordHasher.cs](src/backend/projects/services/safety/safety-api-core/Extensions/Argon2PasswordHasher.cs)
- [AuthorizationService.cs](src/backend/projects/services/safety/safety-api-core/Services/AuthorizationService.cs)

### Authorization, ACLs, And Generated Permissions

- [Scope.implementation.cs](src/backend/projects/libraries/common-dotnet/Permissions/Scope.implementation.cs)
- [ScopesGenerator.cs](src/backend/projects/services/safety/safety-codegen/Generators/ScopesGenerator.cs)
- [RolesGenerator.cs](src/backend/projects/services/safety/safety-codegen/Generators/RolesGenerator.cs)
- [PTreeNodeBuilder.cs](src/backend/projects/services/safety/safety-codegen/Models/PTreeNodeBuilder.cs)
- [Modules.cs](src/backend/projects/services/safety/safety-api-common/Constants/Scopes/Modules.cs)
- [Roles.cs](src/backend/projects/services/safety/safety-api-common/Constants/Scopes/Roles.cs)
- [AclCompilerService.cs](src/backend/projects/services/safety/safety-api-core/Services/AclCompilerService.cs)
- [AccessControlList.cs](src/backend/projects/services/safety/safety-data/Entities/AccessControlLists/AccessControlList.cs)
- [AccessControlListEffectiveUser.cs](src/backend/projects/services/safety/safety-data/Entities/AccessControlLists/AccessControlListEffectiveUser.cs)

### Backend Workflow And Integration

- [RequestValidationPipeline.cs](src/backend/projects/services/safety/safety-api-core/Mediator/Pipelines/RequestValidationPipeline.cs)
- [RequestLoggingMiddleware.cs](src/backend/projects/services/safety/safety-api/Middleware/RequestLoggingMiddleware.cs)
- [PostAssetUploadBegin.cs](src/backend/projects/services/safety/safety-api/Endpoints/Crud/Assets/PostAssetUploadBegin.cs)
- [PostDownloadAsset.cs](src/backend/projects/services/safety/safety-api/Endpoints/Crud/Assets/PostDownloadAsset.cs)
- [AssetTokenService.cs](src/backend/projects/services/safety/safety-api-core/Services/AssetTokenService.cs)
- [PostTimeClockReview.cs](src/backend/projects/services/safety/safety-api/Endpoints/Crud/TimeClock/PostTimeClockReview.cs)
- [PlannerJsonProcessor.cs](src/backend/projects/services/safety/safety-api-core/Helpers/PlannerJsonProcessor.cs)
- [PushService.cs](src/backend/projects/services/safety/safety-api-core/Services/PushService.cs)
- [FeatureFlagService.cs](src/backend/projects/services/safety/safety-api/Services/FeatureFlagService.cs)
- [PagingExtensions.cs](src/backend/projects/services/safety/safety-api-core/Extensions/IQueryable/PagingExtensions.cs)

### Asset Microservice Contract

- [asset.proto](src/backend/projects/libraries/platform-protobuf/protos/asset.proto)
- [asset-api Program.cs](src/backend/projects/services/asset/asset-api/Program.cs)
- [AwsS3Provider.cs](src/backend/projects/services/asset/asset-api/Providers/AwsS3Provider.cs)
- [LocalDiskProvider.cs](src/backend/projects/services/asset/asset-api/Providers/LocalDiskProvider.cs)

## Sanitization Notes

Generated metadata, production hostnames, deployment settings, seed data,
customer-specific workflows, secrets, and frontend code are omitted or
neutralized. The included files are curated excerpts and are not expected to
build as a standalone project.
