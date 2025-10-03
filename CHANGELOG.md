# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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