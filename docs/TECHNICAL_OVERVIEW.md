# Technical Overview — CollabTask

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./TECHNICAL_OVERVIEW.es.md)

## Table of Contents
- [Architectural Vision](#1-architectural-vision)
- [Global Architecture Diagram](#2-global-architecture-diagram)
- [Core Technical Concepts](#3-core-technical-concepts)
- [Documentation Map](#4-documentation-map)
- [Technical Stack & Environment](#5-technical-stack--environment)
- [Quality and Maintenance](#6-quality-and-maintenance)
- [Summary](#7-summary)

---------------------------------------------------------------------------------

This document provides the **technical and architectural overview** of the CollabTask backend.  
It serves as the **root technical reference** for all documents under the `/docs` directory.


## 1. Architectural Vision

**CollabTask** is built upon **Clean Architecture** and **Domain-Driven Design (DDD)** principles.  
Its goal is to isolate the business logic from infrastructure concerns, ensuring maintainability, scalability, and testability.

### Key Architectural Principles
- **Dependency Inversion:** outer layers depend on abstractions from inner layers.  
- **High Cohesion, Low Coupling:** each layer owns a clear responsibility.  
- **Transaction and Concurrency Safety:** optimistic concurrency with `RowVersion` + `ETag`.  
- **Cross-layer consistency:** business rules enforced through domain invariants and application services.


## 2. Global Architecture Diagram

Vertical representation of dependency flow:

```
+------------------------------------------------------+
|                     API Layer                        |
|------------------------------------------------------|
| • Minimal REST Endpoints (Projects, Tasks, Notes)    |
| • Filters: RequireIfMatch / RejectIfMatch            |
| • Authorization Policies (ProjectOwner, Member...)   |
| • SignalR Hub: /hubs/board                           |
| • OpenAPI / Error Handling / DTO Validation          |
+----------------------------↓-------------------------+
|                 Application Layer                    |
|------------------------------------------------------|
| • Use Case Services (CreateTask, MoveTask...)        |
| • Unit of Work (IUnitOfWork.SaveAsync)               |
| • DTO Mapping & Validation (FluentValidation)        |
| • PrecheckStatus / DomainMutation Results            |
| • BoardNotifier for Realtime Updates                 |
+----------------------------↓-------------------------+
|                    Domain Layer                      |
|------------------------------------------------------|
| • Entities (User, Project, Lane, Column, Task...)    |
| • Value Objects (Email, UserName, ProjectName...)    |
| • Domain Invariants and Business Rules               |
| • RowVersion Concurrency Tokens                      |
| • Domain Events and Audit Fields                     |
+----------------------------↓-------------------------+
|                 Infrastructure Layer                 |
|------------------------------------------------------|
| • EF Core 8 Persistence via AppDbContext             |
| • Repositories and Configurations                    |
| • AuditingSaveChangesInterceptor                     |
| • Migrations and Seeders                             |
| • Integration with SQL Server & SQLite (tests)       |
+------------------------------------------------------+
```

Dependency direction:

```
API → Application → Domain
API → Infrastructure (for DI only)
```


## 3. Core Technical Concepts

### Domain-Driven Design (DDD)
Entities and Value Objects capture the business core.  
Rules and invariants are enforced at construction level.

### Unit of Work
Centralizes persistence, ensuring atomic saves:
```csharp
Task<DomainMutation> SaveAsync(MutationKind kind, CancellationToken ct = default);
```

### DomainMutation & PrecheckStatus
Encapsulate the outcome of domain operations, allowing direct HTTP mapping.

### Optimistic Concurrency
- `RowVersion` handled by EF Core.  
- `ETag` exposed via HTTP.  
- `If-Match` required for updates/deletes.  
- Standard responses: `412`, `428`.

### Realtime Collaboration
SignalR broadcasts board events per project group:
```json
{ "type": "task.updated", "projectId": "guid", "payload": { ... } }
```


## 4. Documentation Map

| File | Purpose |
|------|----------|
| **01_Domain_Model.md** | Defines entities, relationships, and value objects. |
| **02_Authorization_Policies.md** | Describes system and project-level authorization. |
| **03_API_Endpoints.md** | Enumerates REST endpoints and their contracts. |
| **04_DTOs.md** | Lists all input/output data structures used in the API. |
| **05_Application_Services_and_Repositories.md** | Explains how use cases and persistence interact. |
| **06_EFCore_Configuration.md** | Documents EF Core mapping, constraints, and concurrency setup. |

These six documents complement the present overview by expanding each subsystem in detail.


## 5. Technical Stack & Environment

| Area | Technology |
|------|-------------|
| **Framework** | .NET 8 |
| **ORM** | Entity Framework Core 8 |
| **Database** | SQL Server (dev/prod), SQLite (tests) |
| **Realtime** | SignalR |
| **Testing** | xUnit + Testcontainers |
| **CI/CD** | GitHub Actions |
| **Containerization** | Docker + Docker Compose |
| **Auth** | JWT Bearer (PBKDF2 password hashing) |


## 6. Quality and Maintenance

- **Test coverage ≥ 75%** enforced in CI.  
- **Code style** follows SOLID and Clean Architecture principles.  
- **Auditing** via timestamps and interceptors.  
- **Branching model:** feature → PR → merge → tag release.  
- **OpenAPI schema** versioned with every release.  


## 7. Summary

**CollabTask v1.0.2** provides:
- A modular and maintainable backend for collaborative task management.  
- Clean separation between Domain, Application, Infrastructure, and API.  
- Optimistic concurrency and real-time updates.  
- Consistent documentation across all layers.  