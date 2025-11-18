# Resumen de DTOs

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./04_DTOs.md)


## Tabla de Contenidos
- [Auth](#auth)
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

Este documento define todos los ***Data Transfer Objects* (DTOs)** utilizados por la **API HTTP de CollabTask**.  
Cada DTO representa el contrato serializado entre la capa de API y sus clientes.

> **Notas**
> - Los DTOs son proyecciones aplanadas de entidades de dominio y objetos de valor.
> - Todos los timestamps están en **UTC** y usan `DateTimeOffset`.
> - Las propiedades `RowVersion` son tokens de concurrencia `byte[]` que se serializan en JSON como cadenas Base64.
>   Se asignan a la cabecera HTTP `ETag` en la capa API.
> - La validación y los invariantes de negocio se aplican en la capa Application y se reflejan en la capa API.


## Auth

### `AuthTokenReadDto`
```csharp
/// <summary>
/// Representa el token de acceso emitido y la información básica del usuario
/// devuelta tras la autenticación.
/// </summary>
public sealed class AuthTokenReadDto
{
    public string AccessToken { get; init; } = default!;
    public string TokenType { get; init; } = "Bearer";
    public DateTimeOffset ExpiresAtUtc { get; init; }

    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public string Name { get; init; } = default!;
}
```

### `UserRegisterDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para registrar una nueva cuenta de usuario.
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
/// Cuerpo de la solicitud usado para autenticar un usuario existente.
/// </summary>
public sealed class UserLoginDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
```


## [User](01_Domain_Model.es.md#user)

### `UserReadDto`
```csharp
/// <summary>
/// Representa la información pública de un usuario y sus membresías de proyecto.
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

    public string RowVersion { get; init; } = default!;
}
```

### `UserChangeRoleDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para cambiar el rol global de un usuario.
/// </summary>
public sealed class UserChangeRoleDto
{
    public required UserRole NewRole { get; init; }
}
```

### `UserRenameDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para cambiar el nombre visible de un usuario.
/// </summary>
public sealed class UserRenameDto
{
    public required string NewName { get; init; }
}
```


## [Project](01_Domain_Model.es.md#project)

### `ProjectReadDto`
```csharp
/// <summary>
/// Representa un proyecto y el rol actual del usuario dentro de él.
/// </summary>
public sealed class ProjectReadDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public string RowVersion { get; init; } = default!;

    public int MembersCount { get; init; }
    public ProjectRole CurrentUserRole { get; init; }
}
```

### `ProjectCreateDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para crear un nuevo proyecto.
/// El usuario autenticado se convierte en el propietario del proyecto.
/// </summary>
public sealed class ProjectCreateDto
{
    public required string Name { get; init; }
}
```

### `ProjectRenameDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para renombrar un proyecto existente.
/// </summary>
public sealed class ProjectRenameDto
{
    public required string NewName { get; init; }
}
```


## [ProjectMember](01_Domain_Model.es.md#projectmember)

### `ProjectMemberReadDto`
```csharp
/// <summary>
/// Representa la membresía de un usuario dentro de un proyecto.
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

    public string RowVersion { get; init; } = default!;
}
```

### `ProjectMemberCreateDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para añadir un usuario como miembro de un proyecto.
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
/// Cuerpo de la solicitud usado para cambiar el rol de un miembro del proyecto.
/// </summary>
public sealed class ProjectMemberChangeRoleDto
{
    public required ProjectRole NewRole { get; init; }
}
```

### `ProjectMemberRoleReadDto`
```csharp
/// <summary>
/// Representa el rol efectivo de un usuario dentro de un proyecto.
/// </summary>
public sealed class ProjectMemberRoleReadDto
{
    public required ProjectRole Role { get; init; }
}
```

### `ProjectMemberCountReadDto`
```csharp
/// <summary>
/// Representa el número de membresías de proyecto activas de un usuario.
/// </summary>
public sealed class ProjectMemberCountReadDto
{
    public required int Count { get; init; }
}
```


## [Lane](01_Domain_Model.es.md#lane)

### `LaneReadDto`
```csharp
/// <summary>
/// Representa un lane dentro del panel de un proyecto.
/// </summary>
public sealed class LaneReadDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }

    public string Name { get; init; } = default!;
    public int Order { get; init; }

    public string RowVersion { get; init; } = default!;
}
```

### `LaneCreateDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para crear un nuevo lane en un proyecto.
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
/// Cuerpo de la solicitud usado para renombrar un lane existente.
/// </summary>
public sealed class LaneRenameDto
{
    public required string NewName { get; init; }
}
```

### `LaneReorderDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para cambiar el orden de visualización de un lane dentro de un proyecto.
/// </summary>
public sealed class LaneReorderDto
{
    public required int NewOrder { get; init; }
}
```


## [Column](01_Domain_Model.es.md#column)

### `ColumnReadDto`
```csharp
/// <summary>
/// Representa una columna dentro de un lane del panel de un proyecto.
/// </summary>
public sealed class ColumnReadDto
{
    public Guid Id { get; init; }
    public Guid LaneId { get; init; }
    public Guid ProjectId { get; init; }

    public string Name { get; init; } = default!;
    public int Order { get; init; }

    public string RowVersion { get; init; } = default!;
}
```

### `ColumnCreateDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para crear una nueva columna dentro de un lane.
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
/// Cuerpo de la solicitud usado para renombrar una columna existente.
/// </summary>
public sealed class ColumnRenameDto
{
    public required string NewName { get; init; }
}
```

### `ColumnReorderDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para cambiar el orden de visualización de una columna dentro de su lane.
/// </summary>
public sealed class ColumnReorderDto
{
    public required int NewOrder { get; init; }
}
```


## [TaskItem](01_Domain_Model.es.md#taskitem)

### `TaskItemReadDto`
```csharp
/// <summary>
/// Representa una tarea dentro de una columna y un lane del panel de un proyecto.
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

    public string RowVersion { get; init; } = default!;
}
```

### `TaskItemCreateDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para crear una nueva tarea dentro de una columna.
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
/// Cuerpo de la solicitud usado para editar las propiedades principales de una tarea.
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
/// Cuerpo de la solicitud usado para mover una tarea entre columnas o lanes
/// y actualizar su orden dentro de la columna de destino.
/// </summary>
public sealed class TaskItemMoveDto
{
    public required Guid NewColumnId { get; init; }
    public required Guid NewLaneId { get; init; }
    public required decimal NewSortKey { get; init; }
}
```


## [TaskNote](01_Domain_Model.es.md#tasknote)

### `TaskNoteReadDto`
```csharp
/// <summary>
/// Representa una nota adjunta a una tarea.
/// </summary>
public sealed class TaskNoteReadDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid UserId { get; init; }

    public string Content { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public string RowVersion { get; init; } = default!;
}
```

### `TaskNoteCreateDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para crear una nueva nota para una tarea.
/// </summary>
public sealed class TaskNoteCreateDto
{
    public required string Content { get; init; }
}
```

### `TaskNoteEditDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para editar el contenido de una nota existente.
/// </summary>
public sealed class TaskNoteEditDto
{
    public required string NewContent { get; init; }
}
```


## [TaskAssignment](01_Domain_Model.es.md#taskassignment)

### `TaskAssignmentReadDto`
```csharp
/// <summary>
/// Representa la asignación de un usuario a una tarea con un rol específico.
/// </summary>
public sealed class TaskAssignmentReadDto
{
    public Guid TaskId { get; init; }
    public Guid UserId { get; init; }

    public TaskRole Role { get; init; }

    public string RowVersion { get; init; } = default!;
}
```

### `TaskAssignmentCreateDto`
```csharp
/// <summary>
/// Cuerpo de la solicitud usado para crear una nueva asignación para una tarea.
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
/// Cuerpo de la solicitud usado para cambiar el rol de una asignación existente.
/// </summary>
public sealed class TaskAssignmentChangeRoleDto
{
    public required TaskRole NewRole { get; init; }
}
```


## [TaskActivity](01_Domain_Model.es.md#taskactivity)

### `TaskActivityReadDto`
```csharp
/// <summary>
/// Representa una entrada de actividad en el registro de auditoría de una tarea.
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