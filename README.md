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
- **Frontend** minimal pages to visualize backend features.
> Details documented in the v0.2.0 changelog.  

---

## Work in progress (v0.3.0) — branch `feature/columns-tasks-crud`
Backend Kanban model and endpoints are being implemented. Not released yet.

### Domain
- New entities: `Lane`, `Column`, `TaskItem`, `TaskNote`, `TaskAssignment`, `TaskActivity`.
- Value Objects: `LaneName`, `ColumnName`, `TaskTitle`, `TaskDescription`, `NoteContent`, `ActivityPayload`.
- Enum: `TaskActivityType` (`TaskCreated`, `TaskEdited`, `TaskMoved`, `OwnerChanged`, `CoOwnerChanged`, `NoteAdded`, `NoteEdited`, `NoteRemoved`).
- Concurrency: `[Timestamp] RowVersion` on board entities.
- Ordering: `Lane.Order`, `Column.Order`, and `TaskItem.SortKey` for stable DnD ordering.
- Auditing fields: `TaskItem.CreatedAt`, `TaskItem.UpdatedAt`, optional `TaskItem.DueDate`.

### Application
- Read/Write services for lanes, columns, tasks, notes, assignments, and activities.
- DTOs, mappers, and FluentValidation validators for create/rename/reorder/move/edit flows.
- Business rules enforced (single owner per task; project/lane/column coherence).

### Infrastructure
- EF Core configurations for all new entities.
- Migrations:
  - `ProjectBoardSchemaUpdate` (base board schema).
  - `ProjectBoard_AssignmentsRowVersion_ProviderChecks` (follow-up adjustments).
- Repositories for Lane, Column, TaskItem, TaskNote, TaskAssignment, TaskActivity.

### API (Minimal APIs)
Nested routes under the project → lane → column structure:

- **Lanes**: `/projects/{projectId}/lanes`
  - `GET /` list, `POST /` create, `PATCH /{laneId}` rename, `PATCH /{laneId}/order` reorder, `DELETE /{laneId}` delete.
- **Columns**: `/projects/{projectId}/lanes/{laneId}/columns`
  - `GET /` list, `POST /` create, `PATCH /{columnId}` rename, `PATCH /{columnId}/order` reorder, `DELETE /{columnId}` delete.
- **Tasks**: `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks`
  - `GET /` list, `GET /{taskId}`, `POST /` create, `PATCH /{taskId}` edit,  
    `PATCH /{taskId}/move` move within the board, `DELETE /{taskId}` delete.
- **Assignments**: `/tasks/{taskId}/owner`, `/tasks/{taskId}/coowners`
  - Owner set/unset; add/remove co-owners.
- **Notes**: `/tasks/{taskId}/notes`
  - `GET /` list, `POST /` add, `PATCH /{noteId}` edit, `DELETE /{noteId}` remove.
- **Task activities**: `/tasks/{taskId}/activities`
  - `GET /` list, `POST /` append activity entries.

Authorization uses existing **project role policies**; endpoints declare minimum policy per action.

### Testing
- New unit and integration tests across **Domain**, **Application**, **Infrastructure**, and **API** for board flows.
- Coverage gate still applies to backend (≥60%).

---

## Backend Overview
- **Domain**: Users, Projects, ProjectMembers, and the new Kanban entities and VOs.
- **Application**: Services orchestrating validation, rules, and persistence.
- **Infrastructure**: EF Core, repositories, migrations, Testcontainers helpers.
- **API**: Minimal APIs grouped by feature with validation and ETag/concurrency helpers.

## Frontend Overview
- React + TS + Tailwind. Dashboard and project shell exist; Kanban UI will arrive with v0.3.0.

## Project Structure
```
/.github    -> CI workflows
/api        -> ASP.NET Core backend (src/* solution folders)
/infra      -> Docker Compose and infra configs
/scripts    -> Unified scripts (dev, prod, test, openapi)
/web        -> React + TS + Tailwind
```

## Local Development
**Requirements**: .NET 8 SDK, Node.js 20+, Docker Desktop.

Commands:
```
npm run dev [args]    # development environment
npm run prod [args]   # production profile
```
Common args: `rebuild | up | down | health | logs`

- API: http://localhost:8080  
- Web: http://localhost:8081

## Testing
Backend:
```
npm run test:unit
npm run test:infra
npm run test:all
```
Frontend: tests intentionally removed since v0.2.0.

## Developer Utilities
```
npm run gen:openapi
npm run gen:api
npm run gen:all
npm run check:contract
```

## Continuous Integration
- Build backend and frontend.
- Run backend tests and enforce coverage.
- Validate OpenAPI.

## License
This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.