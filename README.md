# collab-task

Collaborative task management backend built with **ASP.NET Core** under **Clean Architecture** principles.

---

## Version v0.3.0
Kanban backend fully implemented and stable.

### Domain
- Entities: `Lane`, `Column`, `TaskItem`, `TaskNote`, `TaskAssignment`, `TaskActivity`.
- Value Objects: `LaneName`, `ColumnName`, `TaskTitle`, `TaskDescription`, `NoteContent`, `ActivityPayload`.
- Enum: `TaskActivityType` (`TaskCreated`, `TaskEdited`, `TaskMoved`, `OwnerChanged`, `CoOwnerChanged`, `NoteAdded`, `NoteEdited`, `NoteRemoved`).
- Concurrency control via `[Timestamp] RowVersion`.
- Ordering: `Lane.Order`, `Column.Order`, and `TaskItem.SortKey` for stable board ordering.

### Application
- Read/Write services for lanes, columns, tasks, notes, assignments, and activities.
- Business rules: single owner per task, consistency among project/lane/column relationships.
- **Automatic TaskActivity logging** integrated in write services (TaskItem, TaskAssignment, TaskNote).
- DTOs, mappers, and validators for create/rename/reorder/move/edit flows.

### Infrastructure
- EF Core configurations and migrations:
  - `ProjectBoardSchemaUpdate` (initial Kanban schema).
  - `ProjectBoard_AssignmentsRowVersion_ProviderChecks` (row version + provider fixes).
- Repositories for all entities.
- Database integrity validated for every board entity linkage.

### API (Minimal APIs)
- Nested endpoints under `/projects/{projectId}/lanes/{laneId}/columns/{columnId}`.
- Full CRUD and business flows for lanes, columns, tasks, assignments, notes, and activities.
- Authorization based on project role policies.

### Testing
- Extensive tests across **Domain**, **Application**, **Infrastructure**, and **API**.
- Activity autolog verified through integration tests.
- Coverage ≥60% enforced via CI.

---

## Backend Overview
- **Domain**: Users, Projects, Members, and full Kanban entities.
- **Application**: Services applying validation, rules, and persistence orchestration.
- **Infrastructure**: EF Core persistence, repositories, migrations, and Testcontainers.
- **API**: Minimal API routes grouped per feature.

## Frontend
Frontend removed since v0.3.0. The project is now backend-only.

## Project Structure
```
/.github    -> CI workflows
/api        -> ASP.NET Core backend
/infra      -> Docker Compose and infra configs
/scripts    -> Unified scripts (dev, prod, test)
```

## Local Development
**Requirements**: .NET 8 SDK, Node.js 20+, Docker Desktop.

Commands:
```
npm run dev [args]    # development environment
npm run prod [args]   # production profile
```
Common args: `rebuild | up | down | health | logs`

API: http://localhost:8080

## Testing
```
npm run test:unit
npm run test:infra
npm run test:all
```

## Continuous Integration
- Build backend container.
- Run backend tests with coverage enforcement.
- Validate OpenAPI schema consistency.

## License
This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.
