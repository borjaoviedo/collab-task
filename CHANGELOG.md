# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
