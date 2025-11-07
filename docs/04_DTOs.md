# DTOs Overview

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./04_DTOs.es.md)


## Table of Contents
- [Auth](#auth)
- [Me](#me)
- [User](#user)
- [Project](#project)
- [ProjectMember](#projectmember)
- [Lane](#lane)
- [Column](#column)
- [TaskItem](#taskitem)
- [TaskNote](#tasknote)
- [TaskAssignment](#taskassignment)
- [TaskActivity](#taskactivity)

---------------------------------------------------------------------------------

This document defines all **Data Transfer Objects (DTOs)** used by the **CollabTask HTTP API**.  
Each DTO represents the serialized contract between the API layer and its clients.

> **Notes**
> - DTOs are flattened projections of domain entities and value objects.
> - All timestamps are in **UTC** and use `DateTimeOffset`.
> - `RowVersion` properties are `byte[]` concurrency tokens that JSON serializes as Base64 strings.
>   They are mapped to the HTTP `ETag` header in the API layer.
> - Validation and business invariants are enforced in the Application layer and mirrored in the API layer.


## Auth

### `AuthTokenReadDto`
```csharp
/// <summary>
/// Represents the issued access token and basic user information
/// returned after authentication.
/// </summary>
public sealed class AuthTokenReadDto
{
    public string AccessToken { get; init; } = default!;
    public string TokenType { get; init; } = "Bearer";
    public DateTimeOffset ExpiresAtUtc { get; init; }

    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public string Name { get; init; } = default!;
    public UserRole Role { get; init; }
}
```

### `UserRegisterDto`
```csharp
/// <summary>
/// Request body used to register a new user account.
/// </summary>
public sealed class UserRegisterDto
{
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string Password { get; init; }
}
```

### `UserLoginDto`
```csharp
/// <summary>
/// Request body used to authenticate an existing user.
/// </summary>
public sealed class UserLoginDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
```


## Me

### `MeReadDto`
```csharp
/// <summary>
/// Represents the authenticated user profile derived from JWT claims.
/// </summary>
public sealed class MeReadDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = default!;
    public string Name { get; init; } = default!;
    public UserRole Role { get; init; }
    public int ProjectMembershipsCount { get; init; }
}
```


## [User](01_Domain_Model.md#user)

### `UserReadDto`
```csharp
/// <summary>
/// Represents public information about a user and their project memberships.
/// </summary>
public sealed class UserReadDto
{
    public Guid Id { get; init; }

    public string Email { get; init; } = default!;
    public string Name { get; init; } = default!;
    public UserRole Role { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public int ProjectMembershipsCount { get; init; }

    public byte[] RowVersion { get; init; } = default!;
}
```

### `UserChangeRoleDto`
```csharp
/// <summary>
/// Request body used to change the global role of a user.
/// </summary>
public sealed class UserChangeRoleDto
{
    public required UserRole NewRole { get; init; }
}
```

### `UserRenameDto`
```csharp
/// <summary>
/// Request body used to change the display name of a user.
/// </summary>
public sealed class UserRenameDto
{
    public required string NewName { get; init; }
}
```


## [Project](01_Domain_Model.md#project)

### `ProjectReadDto`
```csharp
/// <summary>
/// Represents a project and the current user's role within it.
/// </summary>
public sealed class ProjectReadDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public byte[] RowVersion { get; init; } = default!;

    public int MembersCount { get; init; }
    public ProjectRole CurrentUserRole { get; init; }
}
```

### `ProjectCreateDto`
```csharp
/// <summary>
/// Request body used to create a new project.
/// The authenticated user becomes the project owner.
/// </summary>
public sealed class ProjectCreateDto
{
    public required string Name { get; init; }
}
```

### `ProjectRenameDto`
```csharp
/// <summary>
/// Request body used to rename an existing project.
/// </summary>
public sealed class ProjectRenameDto
{
    public required string NewName { get; init; }
}
```


## [ProjectMember](01_Domain_Model.md#projectmember)

### `ProjectMemberReadDto`
```csharp
/// <summary>
/// Represents a membership of a user within a project.
/// </summary>
public sealed class ProjectMemberReadDto
{
    public Guid ProjectId { get; init; }
    public Guid UserId { get; init; }

    public string UserName { get; init; } = default!;
    public string Email { get; init; } = default!;

    public ProjectRole Role { get; init; }

    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? RemovedAt { get; init; }

    public byte[] RowVersion { get; init; } = default!;
}
```

### `ProjectMemberCreateDto`
```csharp
/// <summary>
/// Request body used to add a user as a member of a project.
/// </summary>
public sealed class ProjectMemberCreateDto
{
    public required Guid UserId { get; init; }
    public required ProjectRole Role { get; init; }
}
```

### `ProjectMemberChangeRoleDto`
```csharp
/// <summary>
/// Request body used to change the role of a project member.
/// </summary>
public sealed class ProjectMemberChangeRoleDto
{
    public required ProjectRole NewRole { get; init; }
}
```

### `ProjectMemberRoleReadDto`
```csharp
/// <summary>
/// Represents the effective role of a user within a project.
/// </summary>
public sealed class ProjectMemberRoleReadDto
{
    public required ProjectRole Role { get; init; }
}
```

### `ProjectMemberCountReadDto`
```csharp
/// <summary>
/// Represents the count of active project memberships for a user.
/// </summary>
public sealed class ProjectMemberCountReadDto
{
    public required int Count { get; init; }
}
```


## [Lane](01_Domain_Model.md#lane)

### `LaneReadDto`
```csharp
/// <summary>
/// Represents a lane within a project board.
/// </summary>
public sealed class LaneReadDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }

    public string Name { get; init; } = default!;
    public int Order { get; init; }

    public byte[] RowVersion { get; init; } = default!;
}
```

### `LaneCreateDto`
```csharp
/// <summary>
/// Request body used to create a new lane in a project.
/// </summary>
public sealed class LaneCreateDto
{
    public required string Name { get; init; }
    public required int Order { get; init; }
}
```

### `LaneRenameDto`
```csharp
/// <summary>
/// Request body used to rename an existing lane.
/// </summary>
public sealed class LaneRenameDto
{
    public required string NewName { get; init; }
}
```

### `LaneReorderDto`
```csharp
/// <summary>
/// Request body used to change the display order of a lane within a project.
/// </summary>
public sealed class LaneReorderDto
{
    public required int NewOrder { get; init; }
}
```


## [Column](01_Domain_Model.md#column)

### `ColumnReadDto`
```csharp
/// <summary>
/// Represents a column within a lane of a project board.
/// </summary>
public sealed class ColumnReadDto
{
    public Guid Id { get; init; }
    public Guid LaneId { get; init; }
    public Guid ProjectId { get; init; }

    public string Name { get; init; } = default!;
    public int Order { get; init; }

    public byte[] RowVersion { get; init; } = default!;
}
```

### `ColumnCreateDto`
```csharp
/// <summary>
/// Request body used to create a new column within a lane.
/// </summary>
public sealed class ColumnCreateDto
{
    public required string Name { get; init; }
    public required int Order { get; init; }
}
```

### `ColumnRenameDto`
```csharp
/// <summary>
/// Request body used to rename an existing column.
/// </summary>
public sealed class ColumnRenameDto
{
    public required string NewName { get; init; }
}
```

### `ColumnReorderDto`
```csharp
/// <summary>
/// Request body used to change the display order of a column within its lane.
/// </summary>
public sealed class ColumnReorderDto
{
    public required int NewOrder { get; init; }
}
```


## [TaskItem](01_Domain_Model.md#taskitem)

### `TaskItemReadDto`
```csharp
/// <summary>
/// Represents a task item within a column and lane of a project board.
/// </summary>
public sealed class TaskItemReadDto
{
    public Guid Id { get; init; }

    public Guid ColumnId { get; init; }
    public Guid LaneId { get; init; }
    public Guid ProjectId { get; init; }

    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? DueDate { get; init; }

    public decimal SortKey { get; init; }

    public byte[] RowVersion { get; init; } = default!;
}
```

### `TaskItemCreateDto`
```csharp
/// <summary>
/// Request body used to create a new task item within a column.
/// </summary>
public sealed class TaskItemCreateDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }

    public DateTimeOffset? DueDate { get; init; }

    public required decimal SortKey { get; init; }
}
```

### `TaskItemEditDto`
```csharp
/// <summary>
/// Request body used to edit the main properties of a task item.
/// </summary>
public sealed class TaskItemEditDto
{
    public string? NewTitle { get; init; } = default!;
    public string? NewDescription { get; init; } = default!;
    public DateTimeOffset? NewDueDate { get; init; }
}
```

### `TaskItemMoveDto`
```csharp
/// <summary>
/// Request body used to move a task item between columns or lanes
/// and update its ordering within the target column.
/// </summary>
public sealed class TaskItemMoveDto
{
    public required Guid NewColumnId { get; init; }
    public required Guid NewLaneId { get; init; }
    public required decimal NewSortKey { get; init; }
}
```


## [TaskNote](01_Domain_Model.md#tasknote)

### `TaskNoteReadDto`
```csharp
/// <summary>
/// Represents a note attached to a task item.
/// </summary>
public sealed class TaskNoteReadDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid UserId { get; init; }

    public string Content { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public byte[] RowVersion { get; init; } = default!;
}
```

### `TaskNoteCreateDto`
```csharp
/// <summary>
/// Request body used to create a new note for a task item.
/// </summary>
public sealed class TaskNoteCreateDto
{
    public required string Content { get; init; }
}
```

### `TaskNoteEditDto`
```csharp
/// <summary>
/// Request body used to edit the content of an existing task note.
/// </summary>
public sealed class TaskNoteEditDto
{
    public required string NewContent { get; init; }
}
```


## [TaskAssignment](01_Domain_Model.md#taskassignment)

### `TaskAssignmentReadDto`
```csharp
/// <summary>
/// Represents a user assignment to a task item with a specific role.
/// </summary>
public sealed class TaskAssignmentReadDto
{
    public Guid TaskId { get; init; }
    public Guid UserId { get; init; }

    public TaskRole Role { get; init; }

    public byte[] RowVersion { get; init; } = default!;
}
```

### `TaskAssignmentCreateDto`
```csharp
/// <summary>
/// Request body used to create a new assignment for a task item.
/// </summary>
public sealed class TaskAssignmentCreateDto
{
    public required Guid UserId { get; init; }
    public required TaskRole Role { get; init; }
}
```

### `TaskAssignmentChangeRoleDto`
```csharp
/// <summary>
/// Request body used to change the role of an existing task assignment.
/// </summary>
public sealed class TaskAssignmentChangeRoleDto
{
    public required TaskRole NewRole { get; init; }
}
```


## [TaskActivity](01_Domain_Model.md#taskactivity)

### `TaskActivityReadDto`
```csharp
/// <summary>
/// Represents an activity entry in the audit log of a task item.
/// </summary>
public sealed class TaskActivityReadDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid ActorId { get; init; }

    public TaskActivityType Type { get; init; }
    public string Payload { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }
}
```