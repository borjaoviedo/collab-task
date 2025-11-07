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
Task<User?> GetAsync(
    Guid userId,
    CancellationToken ct = default);

Task<User?> GetByEmailAsync(
    string email,
    CancellationToken ct = default);

Task<IReadOnlyList<User>> ListAsync(
    CancellationToken ct = default);
```


#### `IUserWriteService`

**Responsibilities**

- Administrative operations such as changing global roles and deleting users.
- Operations that modify user profile data.

**Methods**

```csharp
Task<(DomainMutation, User?)> CreateAsync(
    Email email,
    UserName name,
    byte[] hash,
    byte[] salt,
    UserRole role,
    CancellationToken ct = default);

Task<DomainMutation> RenameAsync(
    Guid id,
    UserName newName,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> ChangeRoleAsync(
    Guid id,
    UserRole newRole,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> DeleteAsync(
    Guid id,
    byte[] rowVersion,
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
Task<Project?> GetAsync(
    Guid projectId,
    CancellationToken ct = default);

Task<IReadOnlyList<Project>> ListByUserAsync(
    Guid userId,
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
Task<(DomainMutation, Project?)> CreateAsync(
    Guid userId,
    ProjectName name,
    CancellationToken ct = default);

Task<DomainMutation> RenameAsync(
    Guid projectId,
    ProjectName newName,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> DeleteAsync(
    Guid projectId,
    byte[] rowVersion,
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
Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(
    Guid projectId,
    bool includeRemoved = false,
    CancellationToken ct = default);

Task<ProjectMember?> GetAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);

Task<ProjectRole?> GetRoleAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);

Task<int> CountActiveAsync(
    Guid userId,
    CancellationToken ct = default);
```

#### `IProjectMemberWriteService`

**Responsibilities**

- Add users to a project.
- Change project roles.
- Soft remove and restore memberships.

**Methods**

```csharp
Task<(DomainMutation, ProjectMember?)> CreateAsync(
    Guid projectId,
    Guid userId,
    ProjectRole role,
    CancellationToken ct = default);

Task<DomainMutation> ChangeRoleAsync(
    Guid projectId,
    Guid userId,
    ProjectRole newRole,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> RemoveAsync(
    Guid projectId,
    Guid userId,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> RestoreAsync(
    Guid projectId,
    Guid userId,
    byte[] rowVersion,
    CancellationToken ct = default);
```


### Lane Application Services

#### `ILaneReadService`

**Responsibilities**

- Read lanes within a project.
- Provide lane projections used by board views.

**Methods**

```csharp
Task<IReadOnlyList<Lane>> ListByProjectAsync(
    Guid projectId,
    CancellationToken ct = default);

Task<Lane?> GetAsync(
    Guid laneId,
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
Task<(DomainMutation, Lane?)> CreateAsync(
    Guid projectId,
    LaneName name,
    int? order = null,
    CancellationToken ct = default);

Task<DomainMutation> RenameAsync(
    Guid laneId,
    LaneName newName,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> ReorderAsync(
    Guid laneId,
    int newOrder,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> DeleteAsync(
    Guid laneId,
    byte[] rowVersion,
    CancellationToken ct = default);
```


### Column Application Services

#### `IColumnReadService`

**Responsibilities**

- Read columns within a lane.
- Provide data required for board views.

**Methods**

```csharp
Task<IReadOnlyList<Column>> ListByLaneAsync(
    Guid laneId,
    CancellationToken ct = default);

Task<Column?> GetAsync(
    Guid columnId,
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
Task<(DomainMutation, Column?)> CreateAsync(
    Guid projectId,
    Guid laneId,
    ColumnName name,
    int? order = null,
    CancellationToken ct = default);

Task<DomainMutation> RenameAsync(
    Guid columnId,
    ColumnName newName,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> ReorderAsync(
    Guid columnId,
    int newOrder,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> DeleteAsync(
    Guid columnId,
    byte[] rowVersion,
    CancellationToken ct = default);
```


### TaskItem Application Services

#### `ITaskItemReadService`

**Responsibilities**

- Read tasks within a column.
- Read individual task details.

**Methods**

```csharp
Task<IReadOnlyList<TaskItem>> ListByColumnAsync(
    Guid columnId,
    CancellationToken ct = default);

Task<TaskItem?> GetAsync(
    Guid taskId,
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
Task<(DomainMutation, TaskItem?)> CreateAsync(
    Guid projectId,
    Guid laneId,
    Guid columnId,
    Guid userId,
    TaskTitle title,
    TaskDescription description,
    DateTimeOffset? dueDate = null,
    decimal? sortKey = null,
    CancellationToken ct = default);

Task<DomainMutation> EditAsync(
    Guid projectId,
    Guid taskId,
    Guid userId,
    TaskTitle? newTitle,
    TaskDescription? newDescription,
    DateTimeOffset? newDueDate,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> MoveAsync(
    Guid projectId,
    Guid taskId,
    Guid targetColumnId,
    Guid targetLaneId,
    Guid userId,
    decimal targetSortKey,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> DeleteAsync(
    Guid projectId,
    Guid taskId,
    byte[] rowVersion,
    CancellationToken ct = default);
```

### TaskNote Application Services

#### `ITaskNoteReadService`

**Responsibilities**

- Read notes linked to a task.
- Read individual note details.

**Methods**

```csharp
Task<IReadOnlyList<TaskNote>> ListByTaskAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskNote>> ListByUserAsync(
    Guid userId,
    CancellationToken ct = default);

Task<TaskNote?> GetAsync(
    Guid noteId,
    CancellationToken ct = default);
```


#### `ITaskNoteWriteService`

**Responsibilities**

- Create notes for tasks.
- Edit note content.
- Delete notes.

**Methods**

```csharp
Task<(DomainMutation, TaskNote?)> CreateAsync(
    Guid projectId,
    Guid taskId,
    Guid userId,
    NoteContent content,
    CancellationToken ct = default);

Task<DomainMutation> EditAsync(
    Guid projectId,
    Guid taskId,
    Guid noteId,
    Guid userId,
    NoteContent content,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> DeleteAsync(
    Guid projectId,
    Guid noteId,
    Guid userId,
    byte[] rowVersion,
    CancellationToken ct = default);
```


### TaskAssignment Application Services

#### `ITaskAssignmentReadService`

**Responsibilities**

- Read assignments for a task.
- Provide user centric views for assignments.

**Methods**

```csharp
Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(
    Guid userId,
    CancellationToken ct = default);

Task<TaskAssignment?> GetAsync(
    Guid taskId,
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
Task<(DomainMutation, TaskAssignment?)> CreateAsync(
    Guid projectId,
    Guid taskId,
    Guid targetUserId,
    TaskRole role,
    Guid executedBy,
    CancellationToken ct = default);

Task<DomainMutation> ChangeRoleAsync(
    Guid projectId,
    Guid taskId,
    Guid targetUserId,
    TaskRole newRole,
    Guid executedBy,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<DomainMutation> DeleteAsync(
    Guid projectId,
    Guid taskId,
    Guid targetUserId,
    Guid executedBy,
    byte[] rowVersion,
    CancellationToken ct = default);
```


### TaskActivity Application Services

#### `ITaskActivityReadService`

**Responsibilities**

- Read activity log entries for a task.
- Provide user centric activity views.

**Methods**

```csharp
Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivity>> ListByTaskTypeAsync(
    Guid taskId,
    TaskActivityType type,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivity>> ListByUserAsync(
    Guid userId,
    CancellationToken ct = default);

Task<TaskActivity?> GetAsync(
    Guid activityId,
    CancellationToken ct = default);
```

#### `ITaskActivityWriteService`

**Responsibilities**

- Add activities to task.

**Methods**

```csharp
Task<(DomainMutation, TaskActivity?)> CreateAsync(
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
Task<User?> GetByIdAsync(
    Guid id,
    CancellationToken ct = default);

Task<User?> GetTrackedByIdAsync(
    Guid id,
    CancellationToken ct = default);

Task<User?> GetByEmailAsync(
    Email email,
    CancellationToken ct = default);

Task<IReadOnlyList<User>> ListAsync(
    CancellationToken ct = default);

Task AddAsync(
    User item,
    CancellationToken ct = default);

Task<PrecheckStatus> RenameAsync(
    Guid id,
    UserName newName,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> ChangeRoleAsync(
    Guid id,
    UserRole newRole,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> DeleteAsync(
    Guid id,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<bool> ExistsWithEmailAsync(
    Email email,
    Guid? excludeUserId = null,
    CancellationToken ct = default);

Task<bool> ExistsWithNameAsync(
    UserName name,
    Guid? excludeUserId = null,
    CancellationToken ct = default);
```


### Project Repository

#### `IProjectRepository`

**Responsibilities**

- Manage `Project` aggregates.
- Support queries by id and user membership.

**Methods**

```csharp
Task<Project?> GetByIdAsync(
    Guid id,
    CancellationToken ct = default);

Task<Project?> GetTrackedByIdAsync(
    Guid id,
    CancellationToken ct = default);

Task<IReadOnlyList<Project>> ListByUserAsync(
    Guid userId,
    ProjectFilter? filter = null,
    CancellationToken ct = default);

Task AddAsync(
    Project project,
    CancellationToken ct = default);

Task<PrecheckStatus> RenameAsync(
    Guid id,
    ProjectName newName,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> DeleteAsync(
    Guid id,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<bool> ExistsByNameAsync(
    Guid ownerId,
    ProjectName name,
    CancellationToken ct = default);
```


### ProjectMember Repository

#### `IProjectMemberRepository`

**Responsibilities**

- Manage `ProjectMember` entities.
- Provide membership views and counts.

**Methods**

```csharp
Task<ProjectMember?> GetByProjectAndUserIdAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);
    
Task<ProjectMember?> GetTrackedByProjectAndUserIdAsync(
    Guid projectId,
    Guid userId,
    CancellationToken ct = default);

Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(
    Guid projectId,
    bool includeRemoved = false,
    CancellationToken ct = default);

Task<ProjectRole?> GetRoleAsync(
    Guid projectId, 
    Guid userId, 
    CancellationToken ct = default);

Task AddAsync(
    ProjectMember member, 
    CancellationToken ct = default);

Task<PrecheckStatus> UpdateRoleAsync(
    Guid projectId,
    Guid userId,
    ProjectRole newRole,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> SetRemovedAsync(
    Guid projectId,
    Guid userId,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> SetRestoredAsync(
    Guid projectId,
    Guid userId,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<bool> ExistsAsync(
    Guid projectId, 
    Guid userId,
    CancellationToken ct = default);

Task<int> CountUserActiveMembershipsAsync(
    Guid userId, 
    CancellationToken ct = default);
```


### Lane Repository

#### `ILaneRepository`

**Responsibilities**

- Manage `Lane` entities in a project.

**Methods**

```csharp
Task<Lane?> GetByIdAsync(
    Guid laneId,
    CancellationToken ct = default);
    
Task<Lane?> GetTrackedByIdAsync(
    Guid laneId,
    CancellationToken ct = default);

Task<IReadOnlyList<Lane>> ListByProjectAsync(
    Guid projectId,
    CancellationToken ct = default);

Task AddAsync(
    Lane lane,
    CancellationToken ct = default);

Task<PrecheckStatus> RenameAsync(
    Guid laneId,
    LaneName newName,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> ReorderPhase1Async(
    Guid laneId,
    int newOrder,
    byte[] rowVersion,
    CancellationToken ct = default);

Task ApplyReorderPhase2Async(
    Guid laneId, 
    CancellationToken ct = default);

Task<PrecheckStatus> DeleteAsync(
    Guid laneId,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<bool> ExistsWithNameAsync(
    Guid projectId,
    LaneName name,
    Guid? excludeLaneId = null,
    CancellationToken ct = default);

Task<int> GetMaxOrderAsync(
    Guid projectId, 
    CancellationToken ct = default);
```


### Column Repository

#### `IColumnRepository`

**Responsibilities**

- Manage `Column` entities inside a lane.

**Methods**

```csharp
Task<Column?> GetByIdAsync(
    Guid columnId,
    CancellationToken ct = default);
    
Task<Column?> GetTrackedByIdAsync(
    Guid columnId,
    CancellationToken ct = default);

Task<IReadOnlyList<Column>> ListByLaneAsync(
    Guid laneId,
    CancellationToken ct = default);

Task AddAsync(
    Column column,
    CancellationToken ct = default);

Task<PrecheckStatus> RenameAsync(
    Guid columnId,
    ColumnName newName,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> ReorderPhase1Async(
    Guid columnId,
    int newOrder,
    byte[] rowVersion,
    CancellationToken ct = default);

Task ApplyReorderPhase2Async(
    Guid columnId, 
    CancellationToken ct = default);

Task<PrecheckStatus> DeleteAsync(
    Guid columnId,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<bool> ExistsWithNameAsync(
    Guid laneId,
    ColumnName name,
    Guid? excludeColumnId = null,
    CancellationToken ct = default);

Task<int> GetMaxOrderAsync(
    Guid laneId, 
    CancellationToken ct = default);
```


### TaskItem Repository

#### `ITaskItemRepository`

**Responsibilities**

- Manage `TaskItem` entities.

**Methods**

```csharp
Task<TaskItem?> GetByIdAsync(
    Guid taskId,
    CancellationToken ct = default);
    
Task<TaskItem?> GetTrackedByIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskItem>> ListByColumnAsync(
    Guid columnId,
    CancellationToken ct = default);

Task AddAsync(
    TaskItem task,
    CancellationToken ct = default);

Task<(PrecheckStatus Status, TaskItemEditedChange? Change)> EditAsync(
    Guid taskId,
    TaskTitle? newTitle,
    TaskDescription? newDescription,
    DateTimeOffset? newDueDate,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<(PrecheckStatus Status, TaskItemMovedChange? Change)> MoveAsync(
    Guid taskId,
    Guid targetColumnId,
    Guid targetLaneId,
    Guid targetProjectId,
    decimal targetSortKey,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> DeleteAsync(
    Guid taskId,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<bool> ExistsWithTitleAsync(
    Guid columnId,
    TaskTitle title,
    Guid? excludeTaskId = null,
    CancellationToken ct = default);

Task<decimal> GetNextSortKeyAsync(
    Guid columnId, 
    CancellationToken ct = default);
```


### TaskNote Repository

#### `ITaskNoteRepository`

**Responsibilities**

- Manage `TaskNote` entities.

**Methods**

```csharp
Task<TaskNote?> GetByIdAsync(
    Guid noteId,
    CancellationToken ct = default);
    
Task<TaskNote?> GetTrackedByIdAsync(
    Guid noteId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskNote>> ListByTaskAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskNote>> ListByUserAsync(
    Guid userId,
    CancellationToken ct = default);

Task AddAsync(
    TaskNote note,
    CancellationToken ct = default);

Task<PrecheckStatus> EditAsync(
    Guid noteId,
    NoteContent newContent,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> DeleteAsync(
    Guid noteId,
    byte[] rowVersion,
    CancellationToken ct = default);
```


### TaskAssignment Repository

#### `ITaskAssignmentRepository`

**Responsibilities**

- Manage `TaskAssignment` entities linking users to tasks.

**Methods**

```csharp
Task<TaskAssignment?> GetByTaskAndUserIdAsync(
    Guid taskId,
    Guid userId,
    CancellationToken ct = default);
    
Task<TaskAssignment?> GetTrackedByTaskAndUserIdAsync(
    Guid taskId,
    Guid userId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(
    Guid userId,
    CancellationToken ct = default);

Task AddAsync(
    TaskAssignment assignment,
    CancellationToken ct = default);

Task<(PrecheckStatus Status, AssignmentChange? Change)> ChangeRoleAsync(
    Guid taskId,
    Guid userId,
    TaskRole newRole,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<PrecheckStatus> DeleteAsync(
    Guid taskId,
    Guid userId,
    byte[] rowVersion,
    CancellationToken ct = default);

Task<bool> ExistsAsync(
    Guid taskId,
    Guid userId,
    CancellationToken ct = default);

Task<bool> AnyOwnerAsync(
    Guid taskId,
    Guid? excludeUserId = null,
    CancellationToken ct = default);
```


### TaskActivity Repository

#### `ITaskActivityRepository`

**Responsibilities**

- Manage `TaskActivity` entities used as an audit log for tasks.

**Methods**

```csharp
Task<TaskActivity?> GetByIdAsync(
    Guid activityId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(
    Guid taskId,
    TaskActivityType type,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskActivity>> ListByUserAsync(
    Guid userId,
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
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```