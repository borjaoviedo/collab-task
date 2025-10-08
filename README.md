# collab-task

Collaborative real-time task management app built with **ASP.NET Core** (backend) and **React + TypeScript + Tailwind** (frontend).

---

## Features (v0.2.0)

- Authentication from v0.1.0 preserved.
- **Project management** with CRUD endpoints.
- **Membership system** linking users to projects.
- **Project role hierarchy**: Owner, Admin, Member, Reader.
- **Authorization policies** applied to project endpoints.
- **Consistent repository/service layer** (`DomainMutation`, DI, refactors).
- **Backend tests**: unit, integration, and policy coverage.
- **Frontend**:
  - **ProjectsPage**: main view listing user projects with create/delete actions.
  - **ProjectBoardPage**: base implementation prepared for future Kanban view integration (columns and tasks not yet developed).
  - **ProjectMembersPage**: displays project members, their roles, and access rules; project **Admins** and **Owners** can manage membership (add, remove, or change roles).
  - **ProjectSettingsPage**: allows the project owner to modify project details.
  - **UsersPage**: lists users in the system for selection or invitation to projects.
- **Intentional scope**: the frontend is a thin visualization layer to showcase backend capabilities.

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
- SQL Server migrations and Testcontainers for integration tests.
- Dependency injection configured via `AddInfrastructure()`.

**API**
- Endpoints for project and membership operations.
- Policies registered through `AddProjectAuthorization()`.
- Middleware configured for JWT authentication + role-based access.

---

## Frontend Overview

- Vite + React + TypeScript + Tailwind.
- **Projects Dashboard**:
  - List projects for the authenticated user.
  - Create and delete projects with proper validation and feedback.
  - Loading, empty, and error states.
- Auth integration with token persistence and automatic logout on 401.
- Uses a small set of reusable UI components.
- **Testing policy**: no frontend tests by design starting in v0.2.0.

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
npm run test:unit     # unit tests
npm run test:infra    # infra tests
npm run test:all      # unit + infra tests
```

### Frontend
- **No tests by design** starting in v0.2.0. The frontend is a thin visualization layer for the backend.

### Coverage
- Coverage threshold (≥60%) enforced **only** for backend modules via CI.

---

## Developer Utilities

```
npm run gen:openapi     # generate OpenAPI (contracts/openapi.json)
npm run gen:api         # generate TypeScript types from OpenAPI (web/src/shared/api/types.ts)
npm run gen:all         # generate OpenAPI + TS types
npm run check:contract  # validate OpenAPI contract consistency
```

---

## Continuous Integration

- GitHub Actions workflow runs on push/PR:  
  - Build backend and frontend.  
  - Run backend tests (unit + infra).  
  - Validate OpenAPI contract.  
  - Enforce backend coverage threshold.  
- Frontend tests are intentionally **not** executed since v0.2.0.

---

## License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.
