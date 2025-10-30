# CollabTask

**CollabTask** is a collaborative task management backend built with **ASP.NET Core 8** following **Clean Architecture** principles.

It provides a real-time Kanban board API supporting multi-user collaboration, optimistic concurrency, and strong domain modeling.

---

## Current Version — v1.0.0

The backend is ready for public release.

- Full documentation and XML comments across all layers.
- Optimistic concurrency with `ETag` / `If-Match` support.
- Domain-driven Unit of Work (`IUnitOfWork`) persistence orchestration.
- Clean separation of Domain, Application, Infrastructure, and API layers.
- Comprehensive test suite (unit + integration, ≥75% coverage).

For detailed technical explanations, see [TECHNICAL_OVERVIEW.md](./TECHNICAL_OVERVIEW.md).  
For version history, see [CHANGELOG.md](./CHANGELOG.md).

---

## Architecture Overview

**CollabTask** is structured into four independent layers:

**Domain** — Entities, Value Objects, invariants, and business rules.
**Application** — Use cases, validation, and transactional orchestration via `IUnitOfWork`.
**Infrastructure** — EF Core persistence, repositories, interceptors, migrations, and DI setup.
**API** — Minimal APIs exposing project, board, and task endpoints (REST + Realtime).

Clean boundaries allow for isolated testing and maintainability.  

---

## Key Features

- **Projects & Members** — Project-based collaboration with role-based access policies.  
- **Kanban Board** — Lanes, Columns, Tasks, Notes, Assignments, and Activities.  
- **Realtime Updates** — SignalR hub (`/hubs/board`) for project-scoped event broadcasting.  
- **Optimistic Concurrency** — Enforced through `RowVersion`, `ETag`, and `If-Match` headers.  
- **Automatic Activity Logging** — Task activities (create, edit, move, ownership, notes) logged automatically.  
- **Strong Domain Model** — Value Objects and invariants protecting data consistency.  
- **Clean Architecture** — Strict layering and dependency direction.  
- **Extensive Testing** — Unit, integration, and concurrency tests with enforced coverage.  

---

## Local Development

**Requirements:**  
- .NET 8 SDK  
- Node.js ≥ 20  
- Docker Desktop

### Commands
```bash
npm run dev [args]     # Run development environment
npm run prod [args]    # Run production profile
```

**Common args:**  
`rebuild | up | down | health | logs`

Default API URL: **http://localhost:8080**

---

## Testing

Run test suites through unified scripts:

```bash
npm run test:unit
npm run test:infra
npm run test:all
```

- Unit tests cover domain and application logic.
- Integration tests validate persistence, concurrency, and endpoint behavior.

---

## Continuous Integration

GitHub Actions pipeline ensures:
- Build and test execution with coverage enforcement (≥75%).
- Container image build verification.

---

## Project Structure

```
.github/        → CI workflows
/api/           → ASP.NET Core backend (Domain, Application, Infrastructure, API)
/infra/         → Docker Compose and infrastructure configs
/scripts/       → Unified run/test scripts
```

---

## License

This project is licensed under the **MIT License**.  
See the [LICENSE](./LICENSE) file for details.

---

## Related Documentation

- [CHANGELOG.md](./CHANGELOG.md) — Full version history.  
- [TECHNICAL_OVERVIEW.md](./TECHNICAL_OVERVIEW.md) — Architecture, patterns, and authorization model.  

---

> **CollabTask** v1.0.0 — backend-ready for public release, documented, and optimized for maintainability.
