# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
  - Infrastructure refactor for repository and service consistency (merged from `refactor/api-repository-services`).

### Changed
- Unified dependency injection for Application and Infrastructure layers.
- Repository signatures updated to return `DomainMutation`.
- Controllers now rely on Application services instead of direct repository calls.
- CI and test suites updated to reflect new structure.

### Notes
- Major backend milestone before tag `v0.2.0`.
- Frontend still pending integration with new endpoints and policies.

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