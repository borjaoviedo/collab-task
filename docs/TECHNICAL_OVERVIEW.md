# Technical Overview — CollabTask v1.0.0

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./TECHNICAL_OVERVIEW.es.md)

This document provides a detailed overview of the **CollabTask backend architecture**, patterns, and internal design principles.

It complements the [README.md](../README.md) file by focusing on the **technical and architectural aspects** of the system.

---

## 1. Architectural Style

**CollabTask** follows **Clean Architecture**, ensuring:
- Separation of concerns across **Domain**, **Application**, **Infrastructure**, and **API** layers.
- Dependency inversion: inner layers have no reference to outer layers.
- Testability, maintainability, and isolation of business rules.

### Layer Responsibilities

| Layer | Description |
|-------|--------------|
| **Domain** | Core business logic: entities, value objects, invariants, and domain rules. |
| **Application** | Orchestrates use cases, handles validation, manages transactions through `IUnitOfWork`. |
| **Infrastructure** | Persistence (EF Core), interceptors, repositories, migrations, and external integrations. |
| **API** | Exposes minimal REST endpoints and SignalR hubs. Handles errors, filters, and OpenAPI documentation. |

Dependency direction:

```
API → Application → Domain
API → Infrastructure (for DI only)
```

---

## 2. Core Patterns

### 2.1 Domain-Driven Design (DDD)
Entities and Value Objects represent the business core.  
All invariants and rules are enforced in constructors or static factories (e.g., `User.Create()`, `TaskItem.Create()`).

**Value Objects** include `Email`, `UserName`, `ProjectName`, `LaneName`, `ColumnName`, `TaskTitle`, `TaskDescription`, `NoteContent`, and more.

All domain entities use **optimistic concurrency tokens** (`RowVersion`) and contain **auditing fields** (`CreatedAt`, `UpdatedAt`).

---

### 2.2 Unit of Work (UoW)

Introduced in v1.0.0, the **Unit of Work** pattern centralizes persistence control:

```csharp
public interface IUnitOfWork
{
    Task<DomainMutation> SaveAsync(CancellationToken ct = default);
}
```

- Application services no longer call `DbContext.SaveChangesAsync()` directly.
- Repositories perform changes; the UoW commits atomically.
- Ensures consistent `PrecheckStatus` outcomes and concurrency handling.
- Enables transaction-scoped domain consistency and simplifies testing.

---

### 2.3 DomainMutation & PrecheckStatus

To unify mutation results and concurrency handling, repositories return standardized result types:

- `DomainMutation` → Wraps outcomes (NoOp, NotFound, Updated, Created, Deleted, Conflict).  
- `PrecheckStatus` → Represents preliminary checks before performing the mutation (NotFound, NoOp, Conflict, Ready).

These types simplify mapping to HTTP responses in the API layer.

---

### 2.4 Concurrency Control

The backend enforces **optimistic concurrency** using EF Core row versions and HTTP preconditions:

| Mechanism | Description |
|------------|-------------|
| `RowVersion` | Byte array updated on each modification. |
| `ETag` | Encoded RowVersion exposed via HTTP headers. |
| `If-Match` | Header required for update/delete operations. |
| `RequireIfMatch` filter | Ensures precondition presence. |
| `RejectIfMatch` filter | Used for endpoints that must not include preconditions (e.g., create). |

HTTP responses:
- `412 Precondition Failed` → RowVersion mismatch.
- `428 Precondition Required` → Missing `If-Match`.
- `409 Conflict` → Logical or domain conflict.

---

### 2.5 Automatic Activity Logging

Each modification to a task automatically triggers creation of a `TaskActivity` entry representing actions like:
- Task creation, edit, move.
- Owner or co-owner assignment changes.
- Note creation, edition, move or deletion.

Logged at the Application layer, persisted atomically with the main entity.

---

### 2.6 Real-Time Communication

Realtime updates are implemented via **SignalR**:
- Hub: `/hubs/board`
- Group: `project:{projectId}`
- Events broadcast via `BoardNotifier`
- Contract format:
  ```json
  { "type": "task.updated", "projectId": "guid", "payload": { ... } }
  ```

Realtime behavior is fully decoupled via Mediator notifications from the write services.

---

## 3. Authorization Model

Authorization in **CollabTask** is scoped at both **system** and **project** levels.

### 3.1 System Roles
| Role | Description |
|------|-------------|
| **SystemAdmin** | Global administrator; unrestricted access across all projects. |
| **User** | Default authenticated user. |

### 3.2 Project Roles
| Role | Capabilities |
|------|---------------|
| **ProjectOwner** | Full permissions; may delete the project, manage all members, and modify any board item. |
| **ProjectAdmin** | Can invite/remove members (except Owner) and manage all lanes, columns and tasks. |
| **ProjectMember** | Can create/edit/move tasks and notes. |
| **ProjectReader** | Read-only access to all board data. |

### 3.3 Authorization Mechanism
Authorization policies are registered via `AddProjectAuthorization()`:
```csharp
services.AddProjectAuthorization();
```
Policies:
- `ProjectOwner`
- `ProjectAdmin`
- `ProjectMember`
- `ProjectReader`

Each endpoint specifies its required policy explicitly:
```csharp
group.MapPut("/{taskId}", UpdateTask)
     .RequireAuthorization(ProjectPolicies.ProjectMember);
```

Claims are extracted from the JWT token; project membership is verified through the `ProjectMemberReadService`.

---

## 4. Persistence Layer

Persistence is implemented using **Entity Framework Core 8**.

### Features
- `AppDbContext` configured with concurrency tokens, constraints, and provider-specific behaviors.
- SQL Server: CHECK constraints and filtered indexes.
- SQLite (test mode): value converters and RowVersion emulation.
- Auditing handled via `AuditingSaveChangesInterceptor` and `IDateTimeProvider`.

### Migrations
Each release may include a migration file under `Infrastructure/Data/Migrations/`.

Final v1.0.0 migrations:
- `Rename_TaskNote_AuthorId_To_UserId`
- `InfraSchemaFinalization`

---

## 5. Testing Strategy

- **Unit tests:** Domain and Application layers.
- **Integration tests:** Persistence, concurrency, and endpoint correctness.
- **Realtime tests:** Serialization and hub event broadcasting.
- Coverage gate ≥ 75% enforced in CI.

Tests are organized by feature with reusable helpers in `TestHelpers`.

---

## 6. CI/CD Pipeline

Automated through GitHub Actions:

1. **Build and test** the backend using .NET 8 SDK.
2. **Run unit and integration tests** with coverage enforcement.
3. **Build Docker image** for backend container.

---

## 7. API Overview

All endpoints are defined using Minimal APIs with a hierarchical route structure:

```
/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}
/projects/{projectId}/members
/auth/login
/auth/register
```

Each module (Tasks, Notes, Columns, etc.) includes:
- DTOs for input/output.
- Mappers.
- Validators (FluentValidation).
- Endpoint filters for concurrency and authorization.

OpenAPI documentation (`openapi.json`) is automatically generated and kept versioned.

---

## 8. Folder Structure (Backend)

The repository is organized by Clean Architecture layers. Folders may differ slightly across modules, but the main structure is:

```
/src/
 ├─ Api/
 │   ├─ Endpoints/
 │   ├─ Errors/
 │   ├─ Filters/
 │   └─ Realtime/
 ├─ Application/
 │   └─ Entity/
 │       ├─ Abstractions/
 │       ├─ DTOs/
 │       ├─ Mapping/
 │       ├─ Services/
 │       └─ Validation/
 ├─ Domain/
 │   ├─ Common/
 │   ├─ Entities/
 │   ├─ ValueObjects/
 │   └─ Enums/
 └─ Infrastructure/
     ├─ Common/
     └─ Data/
         ├─ Configurations/
         ├─ Initialization/
         ├─ Interceptors/
         ├─ Repositories/
         └─ Seeders/
```

---

## 9. Security & Authentication

- Authentication: JWT-based, using `JwtTokenService`.
- Passwords hashed with PBKDF2 (`Pbkdf2PasswordHasher`).
- Token includes user ID, email, and role claims.
- Default users seeded in development mode (`DevSeeder`).

---

## 10. Summary

**CollabTask v1.0.0** consolidates:
- A complete backend API for collaborative task management.
- Robust domain model and transactional consistency.
- Real-time collaboration with SignalR.
- Full documentation and tests.
- Production-ready architecture under Clean Architecture and DDD principles.

---

> **CollabTask** — Clean, concurrent, and collaborative by design.
