# collab-task

Collaborative task management backend built with **ASP.NET Core** under **Clean Architecture** principles.

---

## Version v0.4.0
Backend now includes **real-time collaboration** via SignalR.

### Realtime System
- Integrated **SignalR** hub: `/hubs/board`.
- Groups: `project:{projectId}` for scoped event delivery.
- Service: `BoardNotifier` for broadcasting events to connected clients.
- Events implemented:
  - **TaskItem** → `task.created`, `task.updated`, `task.moved`, `task.deleted`
  - **TaskAssignment** → `assignment.created`, `assignment.updated`, `assignment.removed`
  - **TaskNote** → `note.created`, `note.updated`, `note.deleted`
- All events follow the schema:
  ```json
  { "type": "note.updated", "projectId": "guid", "payload": { ... } }
  ```
- Tested serialization, handler logic, and hub broadcasting for all event types.

### Domain
- Entities: `Lane`, `Column`, `TaskItem`, `TaskNote`, `TaskAssignment`, `TaskActivity`.
- Value Objects: `LaneName`, `ColumnName`, `TaskTitle`, `TaskDescription`, `NoteContent`, `ActivityPayload`.
- Enum: `TaskActivityType` (`TaskCreated`, `TaskEdited`, `TaskMoved`, `OwnerChanged`, `CoOwnerChanged`, `NoteAdded`, `NoteEdited`, `NoteRemoved`).
- Concurrency control via `[Timestamp] RowVersion`.
- Ordering: `Lane.Order`, `Column.Order`, and `TaskItem.SortKey` for stable board ordering.

### Application
- Read/Write services for lanes, columns, tasks, notes, assignments, and activities.
- Business rules: single owner per task, project consistency, validation on moves and deletions.
- **Realtime publishing** integrated using Mediator notifications.
- **Automatic TaskActivity logging** preserved from v0.3.0.
- DTOs, mappers, and validators for create/rename/reorder/move/edit/delete flows.

### Infrastructure
- EF Core configurations and migrations maintained from v0.3.0.
- Added SignalR services in DI container.
- `WebApplicationExtensions.MapApiLayer()` registers `/hubs/board`.

### API (Minimal APIs)
- Nested endpoints under `/projects/{projectId}/lanes/{laneId}/columns/{columnId}`.
- CRUD + realtime publishing for tasks, assignments, and notes.
- Authorization based on project role policies (`ProjectReader`, `ProjectMember`, etc.).

### Testing
- Unit and integration tests for all event serialization and handler pipelines.
- `BoardNotifierTests` verifying SignalR broadcasting.
- Coverage ≥60% enforced.

---

## Backend Overview
- **Domain**: Users, Projects, Members, and full Kanban entities.
- **Application**: Validation, business rules, persistence orchestration, realtime publication.
- **Infrastructure**: EF Core persistence, repositories, migrations, SignalR integration.
- **API**: Minimal API routes grouped per feature and `/hubs/board` for realtime.

## Frontend
Frontend removed since v0.3.0. The project is backend-only.

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
