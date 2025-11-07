# CollabTask

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./README.es.md)

**CollabTask** is a collaborative task management backend built with **ASP.NET Core 8** following **Clean Architecture** principles.

It provides a real-time Kanban board API supporting multi-user collaboration, optimistic concurrency, and strong domain modeling.  
Current stable release: **v1.0.1**


## Key Features

- **Projects & Members** — Project-based collaboration with role-based access policies.  
- **Kanban Board** — Lanes, Columns, Tasks, Notes, Assignments, and Activities.  
- **Realtime Updates** — SignalR hub (`/hubs/board`) for project-scoped event broadcasting.  
- **Optimistic Concurrency** — Enforced through `RowVersion`, `ETag`, and `If-Match` headers.  
- **Automatic Activity Logging** — Task activities (create, edit, move, ownership, notes) logged automatically.  
- **Strong Domain Model** — Value Objects and invariants protecting data consistency.  
- **Clean Architecture** — Strict layering and dependency direction.  
- **Extensive Testing** — Unit, integration, and concurrency tests with enforced coverage.  
- **Comprehensive Documentation** — Technical docs 01–06 and bilingual Technical Overview.  


## Architecture Overview

**CollabTask** is structured into four independent layers:

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Entities, Value Objects, invariants, and business rules. |
| **Application** | Use cases, validation, and transactional orchestration via `IUnitOfWork`. |
| **Infrastructure** | EF Core persistence, repositories, interceptors, migrations, and DI setup. |
| **API** | Minimal APIs exposing project, board, and task endpoints (REST + Realtime). |

Clean boundaries allow for isolated testing and maintainability.  


## Technical Documentation

All documentation resides under the `/docs` folder:

| File | Description |
|------|--------------|
| [01_Domain_Model.md](docs/01_Domain_Model.md) | Domain entities, relationships, and value objects. |
| [02_Authorization_Policies.md](docs/02_Authorization_Policies.md) | System and project-level authorization policies. |
| [03_API_Endpoints.md](docs/03_API_Endpoints.md) | REST endpoints and their HTTP contracts. |
| [04_DTOs.md](docs/04_DTOs.md) | Input/output data transfer objects. |
| [05_Application_Services_and_Repositories.md](docs/05_Application_Services_and_Repositories.md) | Application services and repository interactions. |
| [06_EFCore_Configuration.md](docs/06_EFCore_Configuration.md) | EF Core configuration, constraints, and concurrency control. |
| [TECHNICAL_OVERVIEW.md](docs/TECHNICAL_OVERVIEW.md) | Root technical and architectural overview. |

All documents are available in English and Spanish.


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


## Testing

Run test suites through unified scripts:

```bash
npm run test:unit
npm run test:infra
npm run test:all
```

- Unit tests cover domain and application logic.  
- Integration tests validate persistence, concurrency, and endpoint behavior.  


## Continuous Integration

GitHub Actions pipeline ensures:
- Build and test execution with coverage enforcement (≥75%).  
- Container image build verification.  
- Linting and documentation consistency checks.  


## Project Structure

```
.github/        → CI workflows
/api/           → ASP.NET Core backend (Domain, Application, Infrastructure, API)
/docs/          → Technical documentation (bilingual 01–06 + Technical Overview)
/infra/         → Docker Compose and infrastructure configs
/scripts/       → Unified run/test scripts
```


## License

This project is licensed under the **MIT License**.  
See the [LICENSE](./LICENSE) file for details.


## Related Documentation

- [CHANGELOG.md](./CHANGELOG.md) — Version history.  
- [docs/TECHNICAL_OVERVIEW.md](docs/TECHNICAL_OVERVIEW.md) — Architecture and system design.  
- [docs/01_Domain_Model.md](docs/01_Domain_Model.md) — Domain model reference.  
- [docs/02_Authorization_Policies.md](docs/02_Authorization_Policies.md) — Role-based access model.  
- [docs/03_API_Endpoints.md](docs/03_API_Endpoints.md) — REST endpoint definitions.  
- [docs/04_DTOs.md](docs/04_DTOs.md) — DTO specifications.  
- [docs/05_Application_Services_and_Repositories.md](docs/05_Application_Services_and_Repositories.md) — Application and persistence logic.  
- [docs/06_EFCore_Configuration.md](docs/06_EFCore_Configuration.md) — EF Core mappings and constraints.  