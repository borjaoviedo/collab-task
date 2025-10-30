# Changelog

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./CHANGELOG.es.md)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-31

### Added
- **Documentation**
  - Added **comprehensive XML documentation** across the entire backend ensuring **full codebase coverage** and **API clarity**.
- **Concurrency & Contracts**
  - Added **strict handling** for `If-Match` and `ETag` headers across all endpoints.
  - Implemented **`RequireIfMatch()`** and **`RejectIfMatch()`** endpoint filters to enforce **optimistic concurrency**.
  - Added **`428 Precondition Required`** responses to OpenAPI for clarity on precondition enforcement.
- **OpenAPI**
  - Updated **`openapi.json` summaries and descriptions** for all endpoints.
  - Added **detailed endpoint documentation** indicating member/admin roles and concurrency behavior.

### Changed
- **Domain**
  - Added **validation refinements** across value objects (`UserName`, `ProjectName`, etc.) enforcing stricter rules via guard clauses.
  - Extended **domain invariants** for ownership constraints: **exactly one active project owner** per project, validated through **unique filtered index**.
  - Improved consistency of **concurrency tokens** and **audit behavior** across domain entities.
  - Added XML summaries to all **domain types**: entities, value objects, enums, and domain events.

- **Application**
  - Introduced the **Unit of Work (UoW)** pattern via `IUnitOfWork` abstraction to coordinate **atomic persistence boundaries** and unify **transaction outcomes**.
  - Application services now delegate persistence commits to **`IUnitOfWork.SaveAsync()`** instead of direct **`DbContext.SaveChangesAsync()`**.
  - Reorganized namespaces for **`TaskItemChange`** into `Application.TaskItems.Changes`.
  - Replaced **`DomainMutation`** results with refined **`PrecheckStatus`** outcomes for repository operations.
  - Rewrote repository and service interfaces to use **domain value objects** (`TaskTitle`, `TaskDescription`, etc.).
  - Improved **audit interceptor** (`AuditingSaveChangesInterceptor`) integration via **`IDateTimeProvider`**.
  - Standardized **XML documentation** across all interfaces and service abstractions.

- **Infrastructure**
  - Added implementation of **`UnitOfWork`** in `Infrastructure.Data.UnitOfWork`, translating EF Core persistence outcomes into `DomainMutation` results.
  - Extended **dependency injection** to register `IUnitOfWork` and refactored **`DependencyInjection.cs`** with clearer separation between concerns (**DbContext**, interceptors, repositories, services).
  - Moved **`DbInitHostedService`** from `Infrastructure/Initialization` to `Infrastructure/Data/Initialization` for structural consistency.
  - Improved **`AppDbContext`** with provider-specific configurations:
    - **SQL Server:** enforced **CHECK constraints**, **filtered unique indexes** (e.g. active owner rule).
    - **SQLite:** added converters and **rowversion emulation**.
  - Updated **EF Core interceptors** and **auditing logic** for accurate timestamping.
  - Added new migrations **`Rename_TaskNote_AuthorId_To_UserId`** and **`InfraSchemaFinalization`** (final schema cleanup).

- **API**
  - Normalized **endpoint summaries and descriptions** to follow **REST** and **concurrency conventions**.
  - Updated all **Create/Edit/Delete endpoints** to clearly indicate authorization level (**Member-only**, **Admin-only**) and **ETag requirements**.
  - Renamed OpenAPI **`operationId`** fields for consistency:
    - `Tasks_Get` → **`Tasks_Get_ById`**
    - `TaskNotes_Get` → **`TaskNotes_Get_ById`**
    - `TaskNotes_ListMine` → **`TaskNotes_Get_Mine`**
    - `TaskNotes_ListByUser` → **`TaskNotes_Get_ByUser`**
  - Unified **response contracts for concurrency** (`409 Conflict`, `412 PreconditionFailed`, `428 PreconditionRequired`).

- **Testing**
  - Major **refactor of integration and unit tests**:
    - Introduced **`TestHelpers.Api.*`** modules for reusable test operations (**Auth**, **Projects**, **ProjectMembers**, etc.).
    - Added **UoW usage in tests** to verify **transactional persistence** across repositories.
    - Replaced inline setup code with **centralized helpers** improving readability and reusability.
  - Adjusted **test project references** (`TestHelpers.csproj` now references `Api.csproj` for endpoint helpers).
  - Extended **test cases** to cover **concurrency validation** (e.g. `Create_With_IfMatch_Header_Returns_400`).

- **Contracts**
  - Rewritten and normalized the entire **`openapi.json`** file with new summaries, operation IDs, and improved header documentation.

### Fixed
- Corrected multiple inconsistencies between **ETag/If-Match usage** and repository concurrency behavior.
- Fixed **row version propagation** on update and delete operations across all board entities.

### Removed
- Deleted placeholder files `.gitkeep`.

### Notes
- This version completes all refactors, documentation, and preparation for public release.
- Backend is fully documented, consistent with Clean Architecture, Unit of Work pattern, and optimistic concurrency standards.
- **Tag created: `v1.0.0`

## [0.4.0] - 2025-10-17
### Added
- **Backend / Realtime**
  - Integrated **SignalR** for real-time board updates.
  - Added **BoardHub** with group management per `project:{id}`.
  - Introduced **BoardNotifier** service for broadcasting events to clients.
  - Implemented new realtime event models and handlers:
    - `TaskItemCreated`, `TaskItemUpdated`, `TaskItemMoved`, `TaskItemDeleted`
    - `TaskAssignmentCreated`, `TaskAssignmentUpdated`, `TaskAssignmentRemoved`
    - `TaskNoteCreated`, `TaskNoteUpdated`, `TaskNoteDeleted`
  - Added dedicated handlers: `TaskItemChangedHandler`, `TaskNoteChangedHandler`, and `TaskAssignmentChangedHandler`.
  - All events share a unified contract `{ type, projectId, payload }` for consistent serialization.
  - `/hubs/board` endpoint exposed through SignalR integration in the API layer.
  - Extended testing suite:
    - `BoardEventSerializationTests`, `BoardNotifierTests`, and all handler tests (`TaskItem`, `TaskNote`, `TaskAssignment`).

### Changed
- **Application / Write Services**
  - Refactored all board write services to integrate **Mediator-based event publication** after persistence commits:
    - **TaskItemWriteService** → emits `TaskItemCreated`, `TaskItemUpdated`, `TaskItemMoved`, `TaskItemDeleted`.
    - **TaskNoteWriteService** → emits `TaskNoteCreated`, `TaskNoteUpdated`, `TaskNoteDeleted`.
    - **TaskAssignmentWriteService** → emits `TaskAssignmentCreated`, `TaskAssignmentUpdated`, `TaskAssignmentRemoved`.
  - Method signatures now include `projectId` to ensure precise event scoping.
- **API / Composition Root**
  - Added `.AddSignalR()` registration and configured dependency injection for realtime components.
  - `WebApplicationExtensions.MapApiLayer()` exposes the `/hubs/board` SignalR endpoint.
- **Testing & CI**
  - Coverage gate **raised from 60% to 75%**, maintained across all test suites.

### Notes
- This version completes the realtime backend milestone.
- Next phase: refactors, documentation, and optimization before public release (`v1.0.0`).

## [0.3.0] - 2025-10-16

### Added
- **Backend / Kanban**
  - Domain entities: `Lane`, `Column`, `TaskItem`, `TaskNote`, `TaskAssignment`, `TaskActivity`.
  - Value Objects: `LaneName`, `ColumnName`, `TaskTitle`, `TaskDescription`, `NoteContent`, `ActivityPayload`.
  - Enum `TaskActivityType` representing create/edit/move/owner/co-owner/note operations.
  - EF Core configurations and repositories for all entities.
  - Minimal API endpoints for full CRUD and move/reorder flows.
  - **Automatic TaskActivity logging** from write services (`TaskItem`, `TaskAssignment`, `TaskNote`).
  - Concurrency tokens (`RowVersion`) and ordering (`Order`, `SortKey`).
  - Auditing fields on tasks (`CreatedAt`, `UpdatedAt`, optional `DueDate`).

### Changed
- Authorization connected to project-role policies across all endpoints.
- DTOs, mappers, and validators synchronized with domain invariants.
- **Frontend removed**; project is now backend-only.

### Migrations
- `ProjectBoardSchemaUpdate` introducing full board schema.
- `ProjectBoard_AssignmentsRowVersion_ProviderChecks` refining row version handling.

### Testing
- Added and extended tests for all core board flows and automatic activity logging.
- Coverage gate ≥60% maintained.

### Notes
- Backend-only release.
- Tag created: `v0.3.0`.

## [0.2.0] - 2025-10-08

### Added
- **Backend**
  - Full project and membership management (CRUD).
  - Entities: `Project`, `ProjectMember` with role hierarchy (`Owner`, `Admin`, `Member`, `Reader`).
  - `DomainMutation` result type introduced for consistent repository outcomes.
  - Authorization policies by project role (`ProjectOwner`, `ProjectAdmin`, `ProjectMember`, `ProjectReader`).
  - Services: `ProjectMemberService`, `ProjectMembershipReader`, and supporting repositories.
  - Policy registration via `AddProjectAuthorization()` in `AuthorizationExtensions`.
  - Integration and unit tests for:
    - Role-based authorization handlers.
    - Membership reading and role updates.
    - Repository and persistence contracts.
  - Infrastructure refactor for repository and service consistency.

- **Frontend**
  - Expanded the **project management area** with multiple new views that visualize backend features through a clean and minimal interface.
  - Introduced the **ProjectsPage**, providing authenticated users with project listing, creation, and deletion workflows connected to the backend API.
  - Added the foundational **ProjectBoardPage**, establishing layout and routing for future Kanban board functionality (columns and tasks will be implemented in v0.3.0).
  - Implemented the **ProjectMembersPage**, showing all members and their roles within a project; users with **Admin** or **Owner** permissions can invite new members, remove existing ones, and modify roles.
  - Built the **ProjectSettingsPage**, allowing project owners to update basic project information.
  - Added the **UsersPage**, enabling user lookup for member invitation and cross-project role management.
  - Strengthened integration with the authentication layer and API client (`apiFetch`), ensuring bearer token propagation and automatic logout on 401.

### Changed
- **Frontend**
  - **Decision**: frontend serves as a thin visualization layer for the backend.
  - **Tests removed**: all frontend tests were intentionally removed to focus effort on backend development.

- **CI/CD**
  - Workflows updated to **skip frontend tests** and keep coverage gates on backend only.

- **Backend**
  - Unified dependency injection for Application and Infrastructure layers.
  - Repository signatures updated to return `DomainMutation`.
  - CI and test suites updated to reflect new structure.

### Notes
- Consolidated authentication from v0.1.0 with complete project management and membership features.
- Frontend is functional for project operations and intentionally light-weight.

## [0.1.0] - 2025-10-03

### Added
- **Backend**
  - Authentication endpoints: `POST /auth/register`, `POST /auth/login`, `GET /auth/me`.
  - Password hashing with PBKDF2 and JWT token issuance/validation.
  - Swagger with JWT support (development only).
  - Infrastructure seeding for development environment.
  - Docker setup for dev and prod with `compose.yaml`.
  - CI workflows for build, test (unit + integration), and coverage enforcement.
  - Unified scripts (`run.js`, `dev.*`, `prod.*`, `test.*`) for developer workflows.

- **Frontend**
  - Base setup: Vite + React + TypeScript + Tailwind CSS with feature-based folder structure.
  - OpenAPI client generation (`npm run gen:api`) and contract validation (`npm run check:contract`).
  - Session store with token persistence and automatic logout on 401.
  - Protected routes and guards.
  - Pages:
    - Minimal landing page.
    - Login and Register forms with validation.
    - Protected `/me` page fetching profile from API.
  - Docker multi-stage build and infra (`compose.dev.yaml`, `compose.prod.yaml`) for web.
  - CI job for build, type-check, API client generation and contract validation.
  - Basic tests for components and login flow with ≥60% coverage.
  
### Notes
- First functional milestone with backend + frontend integration.
- Tag created: `v0.1.0`.
