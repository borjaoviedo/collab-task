# collab-task

Collaborative real-time task management app built with **ASP.NET Core** (backend) and **React + TypeScript + Tailwind** (frontend).

---

## Features (in progress for v0.2.0)

- Everything from v0.1.0, plus:
  - Project management with CRUD endpoints.
  - Membership system linking users to projects.
  - Role hierarchy: Owner, Admin, Member, Reader.
  - Authorization policies applied to project endpoints.
  - Consistent repository/service layer (refactor integrated).
  - Comprehensive backend tests (unit, integration, policy).
  - DomainMutation result type for repository operations.
- Frontend integration with these features pending.

---

## Backend Overview

**Domain**
- Entities: `User`, `Project`, `ProjectMember`.
- Enums: `UserRole`, `ProjectRole`.
- Value Objects for data integrity.
- Domain rules (unique project name per user, creator = Owner).

**Application**
- Services for users, auth, and project membership.
- Interfaces: `IProjectMemberService`, `IProjectMembershipReader`.
- Authorization handlers enforcing minimum role access.

**Infrastructure**
- EF Core repositories and configuration.
- SQL Server migrations and testcontainers for integration tests.
- Dependency injection configured via `AddInfrastructure()`.

**API**
- Endpoints for project and membership operations.
- Policies registered through `AddProjectAuthorization()`.
- Middleware configured for JWT authentication + role-based access.

---

## Features (v0.1.0)

- User authentication with registration, login, and profile retrieval.  
- Secure password hashing (PBKDF2) and JWT-based authentication.  
- Backend and frontend served via Docker (dev + prod).  
- Feature-based frontend architecture with protected routes.  
- Minimal UI: landing, login, register, and `/me` profile page.  
- Session management with token persistence and auto-logout on 401.  
- API client generated from OpenAPI contract.  
- Continuous Integration with build, type-check, contract validation, and tests.  
- Test coverage ≥60% across backend and frontend.

---

## Project Structure

```
/.github    -> GitHub Actions workflows
/api        -> ASP.NET Core backend
/infra      -> Docker Compose definitions and infra configs
/scripts    -> Unified scripts (dev, prod, test, openapi)
/web        -> Vite + React + TypeScript + Tailwind frontend
```

---

## Local Development

**Requirements**: .NET 8 SDK, Node.js 20+, Docker Desktop.

### Commands

```
npm run dev [args]    # development environment
npm run prod [args]   # production profile
```

### Arguments

The optional [args] can be used to control the behavior:

```
- rebuild   -> Rebuild images before starting
- up        -> Start containers
- down      -> Stop containers
- health    -> Check containers health status
- logs      -> Show container logs
```

Examples:

```
npm run dev rebuild   # rebuild dev containers
npm run dev up        # start dev environment
npm run prod down     # stop prod containers
npm run prod health   # check health of prod containers
npm run prod logs     # view logs for prod environment
```

- Backend API available at http://localhost:8080 (dev).  
- Frontend available at http://localhost:8081 (dev/prod).  

---

## Testing

### Backend
- **Unit tests**: Domain, Application, API.  
- **Integration tests**: Infrastructure with SQL Server Testcontainers.  

```
npm run test:unit     # backend unit tests
npm run test:infra    # backend infra tests
```

### Frontend
- Component tests and login flow tests.  

```
npm run test:web   # frontend tests
```

### Combined
```
npm run test:all   # backend + frontend tests
```

**Notes**  
- Integration tests require Docker running locally.  
- Coverage thresholds (≥60%) are enforced via CI.  

---

## Developer Utilities

```
npm run gen:api         # generate TypeScript types from OpenAPI (web/src/shared/api/types.ts)
npm run check:contract  # validate OpenAPI contract consistency
```

---

## Continuous Integration

- GitHub Actions workflow (`ci.yml`) runs on push/PR:  
  - Build backend and frontend.  
  - Run all tests (unit, infra, web).  
  - Validate OpenAPI contract.  
  - Enforce coverage threshold.  

---

## License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.
