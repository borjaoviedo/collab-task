# Application Services and Repositories

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./05_Application_Services_and_Repositories.es.md)


## Table of Contents
- [Layer Overview](#layer-overview)
- [Cross-cutting Application Services](#cross-cutting-application-services)
  - [ICurrentUserService](#icurrentuserservice)
- [Application Services](#application-services)
  - [Common Conventions](#common-conventions)
  - [User Application Services](#user-application-services)
  - [Project Application Services](#project-application-services)
  - [ProjectMember Application Services](#projectmember-application-services)
  - [Lane Application Services](#lane-application-services)
  - [Column Application Services](#column-application-services)
  - [TaskItem Application Services](#taskitem-application-services)
  - [TaskNote Application Services](#tasknote-application-services)
  - [TaskAssignment Application Services](#taskassignment-application-services)
  - [TaskActivity Application Services](#taskactivity-application-services)
- [Repository Interfaces](#repository-interfaces)
  - [Common Conventions](#common-conventions-1)
  - [User Repository](#user-repository)
  - [Project Repository](#project-repository)
  - [ProjectMember Repository](#projectmember-repository)
  - [Lane Repository](#lane-repository)
  - [Column Repository](#column-repository)
  - [TaskItem Repository](#taskitem-repository)
  - [TaskNote Repository](#tasknote-repository)
  - [TaskAssignment Repository](#taskassignment-repository)
  - [TaskActivity Repository](#taskactivity-repository)
- [Unit of Work and Transactions](#unit-of-work-and-transactions)

---------------------------------------------------------------------------------

This document describes the **interfaces of the application layer** of the CollabTask backend.  
It defines the main [application services](#application-services) (read and write use case orchestration) and [repository interfaces](#repository-interfaces) (persistence abstractions).

> **Notes**
> - Application services are use-case oriented and work with domain entities and value objects defined in [`01_Domain_Model.md`](01_Domain_Model.md).
> - Each aggregate typically exposes two services: a `*ReadService` and a `*WriteService`.
> - Repositories expose aggregate persistence operations and hide infrastructure details such as EF Core and SQL Server.
> - All methods are asynchronous (`Task` or `Task<T>`).
> - Validation and invariants are enforced in the domain and application layers, not inside repositories.


## Layer Overview

- **Application Services**
  - Orchestrate use cases.
  - Coordinate domain entities, repositories, and external services.
  - Map between domain models and DTOs defined in [`04_DTOs.md`](04_DTOs.md).
  - Are defined as interfaces, implemented in the Application layer, and called from API endpoints described in [`03_API_Endpoints.md`](03_API_Endpoints.md).

- **Repositories**
  - Provide a persistence abstraction per aggregate root.
  - Work with domain entities, not DTOs.
  - Are defined as interfaces and implemented in the Infrastructure layer.
  - Are injected into application services.


## Cross-cutting Application Services

### `ICurrentUserService`

This service provides access to the identity of the current authenticated user within the application layer.  
It is implemented in the Infrastructure or API layer and injected into application services.

**Responsibilities**

- Expose the current user identity to the application layer.
- Provide basic information required for authorization and auditing decisions.
- Avoid passing user identity explicitly through every method when it can be resolved from the current context.

**Interface**

```csharp
public interface ICurrentUserService
{
    /// <summary>
    /// Identifier of the current user, or null if not authenticated.
    /// </summary>
    Guid? UserId { get; }
}
```


## Application Services

### Common Conventions

- All services are defined as interfaces in the Application layer.
- Naming convention for interfaces is `IXxxReadService` and `IXxxWriteService` per aggregate.
- Each service encapsulates application logic related to its aggregate, both read and write operations.
- Services coordinate domain entities, repositories, and external components such as authentication or clock providers.
- Methods are grouped by use case, not by HTTP endpoint.
- Input and output models:
  - Inputs: DTOs or request models defined in the Application layer.
  - Outputs: DTOs consumed by API endpoints or domain entities when needed internally.
- Optimistic concurrency uses `RowVersion` as a binary token mapped to HTTP `ETag` and `If-Match` headers.  
  Concurrency violations are surfaced using domain level constructs such as `DomainMutation` and `PrecheckStatus`.


### User Application Services

#### `IUserReadService`

**Responsibilities**

- Read users for administrative purposes.
- Read user details for internal operations.
- Provide basic user projections used by other services.

**Methods**

```csharp
Task<UserReadDto> GetByIdAsync(
    Guid userId,
    CancellationToken ct = default);

Task<UserReadDto> GetCurrentAsync(
    CancellationToken ct = default);

Task<IReadOnlyList<UserReadDto>> ListAsync(
    CancellationToken ct = default);
```


#### `IUserWriteService`

**Responsibilities**

- Administrative operations such as changing global roles and deleting users.
- Operations that modify user profile data.

**Methods**

```csharp
Task<AuthTokenReadDto> RegisterAsync(
    UserRegisterDto dto,
    CancellationToken ct = default);

Task<AuthTokenReadDto> LoginAsync(
    UserLoginDto dto,
    CancellationToken ct = default);

Task<UserReadDto> RenameAsync(
    UserRenameDto dto,
    CancellationToken ct = default);

Task<UserReadDto> ChangeRoleAsync(
    Guid userId,
    UserChangeRoleDto dto,
    CancellationToken ct = default);

Task DeleteAsync(
    Guid userId,
    CancellationToken ct = default);
```


### Project Application Services

#### `IProjectReadService`

**Responsibilities**

- Read project details.
- List projects visible to a user.
- Provide views for both the current user and administrative queries.

**Methods**

```csharp
Task<ProjectReadDto> GetByIdAsync(
    Guid projectId,
    CancellationToken ct = default);

Task<IReadOnlyList<ProjectReadDto>> ListByUserIdAsync(
    Guid userId,
    ProjectFilter? filter = null,
    CancellationToken ct = default);

Task<IReadOnlyList<ProjectReadDto>> ListSelfAsync(
    ProjectFilter? filter = null,
    CancellationToken ct = default);   
```

`ProjectFilter` example properties:

- `string? Name`
- `int Page`
- `int PageSize`


#### `IProjectWriteService`

**Responsibilities**

- Create new projects.
- Rename existing projects.
- Delete projects and their board structures.

**Methods**

```csharp
Task<ProjectReadDto> CreateAsync(
    ProjectCreateDto dto,
    CancellationToken ct = default);

Task<ProjectReadDto> RenameAsync(
    Guid projectId,
    ProjectRenameDto dto,
    CancellationToken ct = default);

Task DeleteAsync(
    Guid projectId,
    CancellationToken ct = default);
```


### ProjectMember Application Services

#### `IProjectMemberReadService`

**Responsibilities**

- List members of a project.
- Inspect a specific membership.
- Compute membership counts.

**Methods**

```csharp
Task<ProjectMemberReadDto> GetByProjectAndUserIdAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);

Task<IReadOnlyList<ProjectMemberReadDto>> ListByProjectIdAsync(
    Guid projectId,
    bool includeRemoved = false,
    CancellationToken ct = default);

Task<ProjectMemberReadDto> GetUserRoleAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);

Task<ProjectMemberCountReadDto> CountActiveUsersAsync(
    Guid userId,
    CancellationToken ct = default);

Task<ProjectMemberCountReadDto> CountActiveSelfAsync(
    CancellationToken ct = default);
```

#### `IProjectMemberWriteService`

**Responsibilities**

- Add users to a project.
- Change project roles.
- Soft remove and restore memberships.

**Methods**

```csharp
Task<ProjectMemberReadDto> CreateAsync(
    Guid projectId,
    ProjectMemberCreateDto dto,
    CancellationToken ct = default);

Task<ProjectMemberReadDto> ChangeRoleAsync(
    Guid projectId,
    Guid userId,
    ProjectMemberChangeRoleDto dto,
    CancellationToken ct = default);

Task<ProjectMemberReadDto> RemoveAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);

Task<ProjectMemberReadDto> RestoreAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);
```


### Lane Application Services

#### `ILaneReadService`

**Responsibilities**

- Read lanes within a project.
- Provide lane projections used by board views.

**Methods**

```csharp
Task<LaneReadDto> GetByIdAsync(
    Guid laneId,
    CancellationToken ct = default);

Task<IReadOnlyList<LaneReadDto>> ListByProjectIdAsync(
    Guid projectId,
    CancellationToken ct = default);
```

#### `ILaneWriteService`

**Responsibilities**

- Create lanes.
- Rename lanes.
- Reorder lanes inside a project.
- Delete lanes and their columns and tasks.

**Methods**

```csharp
Task<LaneReadDto> CreateAsync(
    Guid projectId,
    LaneCreateDto dto,
    CancellationToken ct = default);

Task<LaneReadDto> RenameAsync(
    Guid laneId,
    LaneCreateDto dto,
    CancellationToken ct = default);

Task<LaneReadDto> ReorderAsync(
    Guid laneId,
    LaneCreateDto dto,
    CancellationToken ct = default);

Task DeleteAsync(
    Guid laneId,
    CancellationToken ct = default);
```


### Column Application Services

#### `IColumnReadService`

**Responsibilities**

- Read columns within a lane.
- Provide data required for board views.

**Methods**

```csharp
Task<ColumnReadDto> GetByIdAsync(
    Guid columnId,
    CancellationToken ct = default);

Task<IReadOnlyList<ColumnReadDto>> ListByLaneIdAsync(
    Guid laneId,
    CancellationToken ct = default);
```

#### `IColumnWriteService`

**Responsibilities**

- Create columns.
- Rename columns.
- Reorder columns inside a lane.
- Delete columns and their tasks.

**Methods**

```csharp
Task<ColumnReadDto> CreateAsync(
    Guid projectId,
    Guid laneId,
    ColumnCreateDto dto,
    CancellationToken ct = default);

Task<ColumnReadDto> RenameAsync(
    Guid columnId,
    ColumnRenameDto dto,
    CancellationToken ct = default);

Task<ColumnReadDto> ReorderAsync(
    Guid columnId,
    ColumnReorderDto dto,
    CancellationToken ct = default);

Task DeleteAsync(
    Guid columnId,
    CancellationToken ct = default);
```


### TaskItem Application Services

#### `ITaskItemReadService`

**Responsibilities**

- Read tasks within a column.
- Read individual task details.

**Methods**

```csharp
Task<TaskItemReadDto> GetByIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskItemReadDto>> ListByColumnIdAsync(
    Guid columnId,
    CancellationToken ct = default);
```

#### `ITaskItemWriteService`

**Responsibilities**

- Create new tasks.
- Edit task details.
- Move tasks between columns and lanes.
- Delete tasks.

**Methods**

```csharp
Task<TaskItemReadDto> CreateAsync(
    Guid projectId,
    Guid laneId,
    Guid columnId,
    TaskItemCreateDto dto,
    CancellationToken ct = default);

Task<TaskItemReadDto> EditAsync(
    Guid projectId,
    Guid taskId,
    TaskItemEditDto dto,
    CancellationToken ct = default);

Task<TaskItemReadDto> MoveAsync(
    Guid projectId,
    Guid taskId,
    TaskItemMoveDto dto,
    CancellationToken ct = default);

Task DeleteAsync(
    Guid projectId,
    Guid taskId,
    CancellationToken ct = default);
```

### TaskNote Application Services

#### `ITaskNoteReadService`

**Responsibilities**

- Read notes linked to a task.
- Read individual note details.

**Methods**

```csharp
Task<TaskNoteReadDto> GetByIdAsync(
    Guid noteId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskNoteReadDto>> ListByTaskIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskNoteReadDto>> ListByUserIdAsync(
    Guid userId,
    CancellationToken ct = default);
```


#### `ITaskNoteWriteService`

**Responsibilities**

- Create notes for tasks.
- Edit note content.
- Delete notes.

**Methods**

```csharp
Task<TaskNoteReadDto> CreateAsync(
    Guid projectId,
    Guid taskId,
    TaskNoteCreateDto dto,
    CancellationToken ct = default);

Task<TaskNoteReadDto> EditAsync(
    Guid projectId,
    Guid taskId,
    Guid noteId,
    TaskNoteEditDto dto,
    CancellationToken ct = default);

Task DeleteAsync(
    Guid projectId,
    Guid noteId,
    Guid userId,
    CancellationToken ct = default);
```


### TaskAssignment Application Services

#### `ITaskAssignmentReadService`

**Responsibilities**

- Read assignments for a task.
- Provide user centric views for assignments.

**Methods**

```csharp
Task<TaskAssignmentReadDto> GetByTaskAndUserIdAsync(
    Guid taskId,
    Guid userId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskAssignmentReadDto>> ListByTaskIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskAssignmentReadDto>> ListByUserIdAsync(
    Guid userId,
    CancellationToken ct = default);
```


#### `ITaskAssignmentWriteService`

**Responsibilities**

- Add assignments to tasks.
- Change assignment roles.
- Remove assignments.

**Methods**

```csharp
Task<TaskAssignmentReadDto> CreateAsync(
    Guid projectId,
    Guid taskId,
    TaskAssignmentCreateDto dto,
    CancellationToken ct = default);

Task<TaskAssignmentReadDto> ChangeRoleAsync(
    Guid projectId,
    Guid taskId,
    Guid targetUserId,
    TaskAssignmentChangeRoleDto dto,
    CancellationToken ct = default);

Task DeleteAsync(
    Guid projectId,
    Guid taskId,
    Guid targetUserId,
    CancellationToken ct = default);
```


### TaskActivity Application Services

#### `ITaskActivityReadService`

**Responsibilities**

- Read activity log entries for a task.
- Provide user centric activity views.

**Methods**

```csharp
Task<TaskActivityReadDto> GetByIdAsync(
    Guid activityId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivityReadDto>> ListByTaskIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivityReadDto>> ListByUserIdAsync(
    Guid userId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivityReadDto>> ListSelfAsync(
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivityReadDto>> ListByActivityTypeAsync(
    Guid taskId,
    TaskActivityType type,
    CancellationToken ct = default);
```

#### `ITaskActivityWriteService`

**Responsibilities**

- Add activities to task.

**Methods**

```csharp
Task<TaskActivityReadDto> CreateAsync(
    Guid taskId,
    Guid userId,
    TaskActivityType type,
    ActivityPayload payload,
    CancellationToken ct = default);
```


## Repository Interfaces

### Common Conventions

- All repositories are defined as interfaces in the Application layer.
- Naming convention is `IXxxRepository`.
- Repositories work with domain entities, not DTOs.
- Repositories do not contain business logic. They provide persistence and querying capabilities.
- All repository methods are asynchronous.
- Concurrency is enforced using `RowVersion` on tracked entities, combined with `ETag` and `If-Match` in the API layer.


### User Repository

#### `IUserRepository`

**Responsibilities**

- Manage `User` aggregates.
- Provide lookup by id and normalized email.
- Support simple search for administrative views.

**Methods**

```csharp
Task<IReadOnlyList<User>> ListAsync(
    CancellationToken ct = default);

Task<User?> GetByIdAsync(
    Guid userId,
    CancellationToken ct = default);

Task<User?> GetByIdForUpdateAsync(
    Guid userId,
    CancellationToken ct = default);

Task<User?> GetByEmailAsync(
    Email email,
    CancellationToken ct = default);

Task AddAsync(
    User item,
    CancellationToken ct = default);

Task UpdateAsync(
    User user, 
    CancellationToken ct = default);

Task RemoveAsync(
    User user, 
    CancellationToken ct = default);
```


### Project Repository

#### `IProjectRepository`

**Responsibilities**

- Manage `Project` aggregates.
- Support queries by id and user membership.

**Methods**

```csharp
Task<IReadOnlyList<Project>> ListByUserIdAsync(
    Guid userId,
    ProjectFilter? filter = null,
    CancellationToken ct = default);

Task<Project?> GetByIdAsync(
    Guid projectId,
    CancellationToken ct = default);

Task<Project?> GetByIdForUpdateAsync(
    Guid projectId,
    CancellationToken ct = default);

Task<bool> ExistsByNameAsync(
    string name, 
    CancellationToken ct = default);

Task AddAsync(
    Project project,
    CancellationToken ct = default);

Task UpdateAsync(
    Project project, 
    CancellationToken ct = default);

Task RemoveAsync(
    Project project, 
    CancellationToken ct = default);
```


### ProjectMember Repository

#### `IProjectMemberRepository`

**Responsibilities**

- Manage `ProjectMember` entities.
- Provide membership views and counts.

**Methods**

```csharp
Task<IReadOnlyList<ProjectMember>> ListByProjectIdAsync(
    Guid projectId,
    bool includeRemoved = false,
    CancellationToken ct = default);

Task<ProjectMember?> GetByProjectAndUserIdAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);
    
Task<ProjectMember?> GetByProjectAndUserIdForUpdateAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);

Task<ProjectRole?> GetUserRoleAsync(
    Guid projectId, 
    Guid userId, 
    CancellationToken ct = default);

Task<bool> ExistsAsync(
    Guid projectId,
    Guid userId, 
    CancellationToken ct = default);

Task<int> CountUserActiveMembershipsAsync(
    Guid userId,
    CancellationToken ct = default);

Task AddAsync(
    ProjectMember member, 
    CancellationToken ct = default);

Task UpdateAsync(
    ProjectMember member, 
    CancellationToken ct = default);
```


### Lane Repository

#### `ILaneRepository`

**Responsibilities**

- Manage `Lane` entities in a project.

**Methods**

```csharp
Task<IReadOnlyList<Lane>> ListByProjectIdAsync(
    Guid projectId,
    CancellationToken ct = default);

Task<Lane?> GetByIdAsync(
    Guid laneId,
    CancellationToken ct = default);
    
Task<Lane?> GetByIdForUpdateAsync(
    Guid laneId,
    CancellationToken ct = default);

Task<PrecheckStatus> PrepareReorderAsync(
    Guid laneId,
    int newOrder,
    CancellationToken ct = default);

Task FinalizeReorderAsync(
    Guid laneId, 
    CancellationToken ct = default);

Task<bool> ExistsWithNameAsync(
    Guid projectId,
    string laneName,
    Guid? excludeLaneId = null,
    CancellationToken ct = default);

Task<int> GetMaxOrderAsync(
    Guid projectId, 
    CancellationToken ct = default);

Task AddAsync(
    Lane lane,
    CancellationToken ct = default);

Task UpdateAsync(
    Lane lane,
    CancellationToken ct = default);

Task RemoveAsync(
    Lane lane,
    CancellationToken ct = default);
```


### Column Repository

#### `IColumnRepository`

**Responsibilities**

- Manage `Column` entities inside a lane.

**Methods**

```csharp
Task<IReadOnlyList<Column>> ListByLaneIdAsync(
    Guid laneId,
    CancellationToken ct = default);

Task<Column?> GetByIdAsync(
    Guid columnId,
    CancellationToken ct = default);
    
Task<Column?> GetByIdForUpdateAsync(
    Guid columnId,
    CancellationToken ct = default);

Task<PrecheckStatus> PrepareReorderAsync(
    Guid columnId,
    int newOrder,
    CancellationToken ct = default);

Task FinalizeReorderAsync(
    Guid columnId,
    CancellationToken ct = default);

Task<bool> ExistsWithNameAsync(
    Guid laneId,
    string columnName,
    Guid? excludeColumnId = null,
    CancellationToken ct = default);

Task<int> GetMaxOrderAsync(
    Guid laneId, 
    CancellationToken ct = default);

Task AddAsync(
    Column column,
    CancellationToken ct = default);

Task UpdateAsync(
    Column column,
    CancellationToken ct = default);

Task RemoveAsync(
    Column column,
    CancellationToken ct = default);
```


### TaskItem Repository

#### `ITaskItemRepository`

**Responsibilities**

- Manage `TaskItem` entities.

**Methods**

```csharp
Task<IReadOnlyList<TaskItem>> ListByColumnIdAsync(
    Guid columnId,
    CancellationToken ct = default);

Task<TaskItem?> GetByIdAsync(
    Guid taskId,
    CancellationToken ct = default);
    
Task<TaskItem?> GetByIdForUpdateAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<bool> ExistsWithTitleAsync(
    Guid columnId,
    string taskTitle,
    Guid? excludeTaskId = null,
    CancellationToken ct = default);

Task<decimal> GetNextSortKeyAsync(
    Guid columnId,
    CancellationToken ct = default);

Task AddAsync(
    TaskItem task,
    CancellationToken ct = default);

Task UpdateAsync(
    TaskItem task, 
    CancellationToken ct = default);
    
Task RemoveAsync(
    TaskItem task, 
    CancellationToken ct = default);
```


### TaskNote Repository

#### `ITaskNoteRepository`

**Responsibilities**

- Manage `TaskNote` entities.

**Methods**

```csharp
Task<IReadOnlyList<TaskNote>> ListByTaskIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskNote>> ListByUserIdAsync(
    Guid userId,
    CancellationToken ct = default);

Task<TaskNote?> GetByIdAsync(
    Guid noteId,
    CancellationToken ct = default);
    
Task<TaskNote?> GetByIdForUpdateAsync(
    Guid noteId,
    CancellationToken ct = default);

Task AddAsync(
    TaskNote note,
    CancellationToken ct = default);

Task UpdateAsync(
    TaskNote note, 
    CancellationToken ct = default);

Task RemoveAsync(
    TaskNote note, 
    CancellationToken ct = default);
```


### TaskAssignment Repository

#### `ITaskAssignmentRepository`

**Responsibilities**

- Manage `TaskAssignment` entities linking users to tasks.

**Methods**

```csharp
Task<IReadOnlyList<TaskAssignment>> ListByTaskIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskAssignment>> ListByUserIdAsync(
    Guid userId,
    CancellationToken ct = default);

Task<TaskAssignment?> GetByTaskAndUserIdAsync(
    Guid taskId,
    Guid userId,
    CancellationToken ct = default);
    
Task<TaskAssignment?> GetByTaskAndUserIdForUpdateAsync(
    Guid taskId,
    Guid userId,
    CancellationToken ct = default);

Task<bool> ExistsAsync(
    Guid taskId,
    Guid userId,
    CancellationToken ct = default);

Task<bool> AnyOwnerAsync(
    Guid taskId,
    Guid? excludeUserId = null,
    CancellationToken ct = default);

Task AddAsync(
    TaskAssignment assignment,
    CancellationToken ct = default);

Task UpdateAsync(
    TaskAssignment assignment,
    CancellationToken ct = default);

Task RemoveAsync(
    TaskAssignment assignment,
    CancellationToken ct = default);
```


### TaskActivity Repository

#### `ITaskActivityRepository`

**Responsibilities**

- Manage `TaskActivity` entities used as an audit log for tasks.

**Methods**

```csharp
Task<IReadOnlyList<TaskActivity>> ListByTaskIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivity>> ListByTaskTypeAsync(
    Guid taskId,
    TaskActivityType type,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivity>> ListByUserIdAsync(
    Guid userId,
    CancellationToken ct = default);

Task<TaskActivity?> GetByIdAsync(
    Guid activityId,
    CancellationToken ct = default);

Task AddAsync(
    TaskActivity activity,
    CancellationToken ct = default);
```


## Unit of Work and Transactions

The Unit of Work coordinates transactional consistency across multiple repositories within a single request scope.

- Interface name: `IUnitOfWork`.
- Implemented in the Infrastructure layer.
- Called once per request from the Application layer to persist changes atomically.
- Ensures that all repository operations participating in the same transaction either succeed or fail together.

```csharp
public interface IUnitOfWork
{
    Task<DomainMutation> SaveAsync(MutationKind kind, CancellationToken ct = default);
}
```