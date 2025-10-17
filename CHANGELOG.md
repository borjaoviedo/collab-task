# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
