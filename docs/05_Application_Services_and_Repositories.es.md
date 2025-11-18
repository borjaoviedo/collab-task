# Servicios de aplicación y repositorios

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./05_Application_Services_and_Repositories.md)


## Tabla de Contenidos
- [Visión general de la capa](#visión-general-de-la-capa)
- [Servicios de aplicación transversales](#servicios-de-aplicación-transversales)
  - [ICurrentUserService](#icurrentuserservice)
- [Servicios de aplicación](#servicios-de-aplicación)
  - [Convenciones comunes](#convenciones-comunes)
  - [Servicios de aplicación de User](#servicios-de-aplicación-de-user)
  - [Servicios de aplicación de Project](#servicios-de-aplicación-de-project)
  - [Servicios de aplicación de ProjectMember](#servicios-de-aplicación-de-projectmember)
  - [Servicios de aplicación de Lane](#servicios-de-aplicación-de-lane)
  - [Servicios de aplicación de Column](#servicios-de-aplicación-de-column)
  - [Servicios de aplicación de TaskItem](#servicios-de-aplicación-de-taskitem)
  - [Servicios de aplicación de TaskNote](#servicios-de-aplicación-de-tasknote)
  - [Servicios de aplicación de TaskAssignment](#servicios-de-aplicación-de-taskassignment)
  - [Servicios de aplicación de TaskActivity](#servicios-de-aplicación-de-taskactivity)
- [Interfaces de repositorio](#interfaces-de-repositorio)
  - [Convenciones comunes](#convenciones-comunes-1)
  - [Repositorio de User](#repositorio-de-user)
  - [Repositorio de Project](#repositorio-de-project)
  - [Repositorio de ProjectMember](#repositorio-de-projectmember)
  - [Repositorio de Lane](#repositorio-de-lane)
  - [Repositorio de Column](#repositorio-de-column)
  - [Repositorio de TaskItem](#repositorio-de-taskitem)
  - [Repositorio de TaskNote](#repositorio-de-tasknote)
  - [Repositorio de TaskAssignment](#repositorio-de-taskassignment)
  - [Repositorio de TaskActivity](#repositorio-de-taskactivity)
- [Unidad de trabajo y transacciones](#unidad-de-trabajo-y-transacciones)

---------------------------------------------------------------------------------

Este documento describe las **interfaces de la capa de aplicación** del backend de CollabTask.  
Define los principales [servicios de aplicación](#servicios-de-aplicación) (orquestación de *use cases* de lectura y escritura) y las [interfaces de repositorio](#interfaces-de-repositorio) (abstracciones de persistencia).

> **Notas**
> - Los servicios de aplicación están orientados a casos de uso y trabajan con entidades de dominio y objetos de valor definidos en [`01_Domain_Model.es.md`](01_Domain_Model.es.md).
> - Cada agregado suele exponer dos servicios: un `*ReadService` y un `*WriteService`.
> - Los repositorios exponen operaciones de persistencia de agregados y ocultan detalles de infraestructura como EF Core y SQL Server.
> - Todos los métodos son asíncronos (`Task` o `Task<T>`).
> - La validación y los invariantes se aplican en las capas de dominio y de aplicación, no dentro de los repositorios.


## Visión general de la capa

- **Servicios de aplicación**
  - Orquestan los casos de uso.
  - Coordinan entidades de dominio, repositorios y servicios externos.
  - Mapean entre modelos de dominio y DTOs definidos en [`04_DTOs.es.md`](04_DTOs.es.md).
  - Se definen como interfaces, se implementan en la capa Application y se invocan desde los endpoints de la API descritos en [`03_API_Endpoints.es.md`](03_API_Endpoints.es.md).

- **Repositorios**
  - Proporcionan una abstracción de persistencia por *aggregate root*.
  - Trabajan con entidades de dominio, no con DTOs.
  - Se definen como interfaces y se implementan en la capa Infrastructure.
  - Se inyectan en los servicios de aplicación.


## Servicios de aplicación transversales

### `ICurrentUserService`

Este servicio proporciona acceso a la identidad del usuario autenticado actual dentro de la capa de aplicación.  
Se implementa en la capa de Infrastructure o API y se inyecta en los servicios de aplicación.

**Responsabilidades**

- Exponer la identidad del usuario actual a la capa de aplicación.
- Proporcionar información básica necesaria para decisiones de autorización y auditoría.
- Evitar pasar explícitamente la identidad de usuario por cada método cuando pueda resolverse desde el contexto actual.

**Interfaz**

```csharp
public interface ICurrentUserService
{
    /// <summary>
    /// Identificador del usuario actual, o null si no está autenticado.
    /// </summary>
    Guid? UserId { get; }
}
```


## Servicios de aplicación

### Convenciones comunes

- Todos los servicios se definen como interfaces en la capa Application.
- La convención de nombres para interfaces es `IXxxReadService` e `IXxxWriteService` por agregado.
- Cada servicio encapsula lógica de aplicación relacionada con su agregado, tanto operaciones de lectura como de escritura.
- Los servicios coordinan entidades de dominio, repositorios y componentes externos como autenticación o proveedores de tiempo.
- Los métodos se agrupan por caso de uso, no por endpoint HTTP.
- Modelos de entrada y salida:
  - Entradas: DTOs o modelos de solicitud definidos en la capa Application.
  - Salidas: DTOs consumidos por los endpoints de la API o entidades de dominio cuando se necesitan internamente.
- La concurrencia optimista utiliza `RowVersion` como token binario, mapeado a las cabeceras HTTP `ETag` y `If-Match`.  
  Las violaciones de concurrencia se exponen mediante constructos de dominio como `DomainMutation` y `PrecheckStatus`.


### Servicios de aplicación de User

#### `IUserReadService`

**Responsabilidades**

- Leer usuarios con fines administrativos.
- Leer detalles de usuario para operaciones internas.
- Proporcionar proyecciones básicas de usuario usadas por otros servicios.

**Métodos**

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

**Responsabilidades**

- Operaciones administrativas como cambio de roles globales y eliminación de usuarios.
- Operaciones que modifican los datos de perfil del usuario.

**Métodos**

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


### Servicios de aplicación de Project

#### `IProjectReadService`

**Responsabilidades**

- Leer detalles de proyectos.
- Listar proyectos visibles para un usuario.
- Proporcionar vistas tanto para el usuario actual como para consultas administrativas.

**Métodos**

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

Ejemplo de propiedades de `ProjectFilter`:

- `string? Name`
- `int Page`
- `int PageSize`


#### `IProjectWriteService`

**Responsabilidades**

- Crear nuevos proyectos.
- Renombrar proyectos existentes.
- Eliminar proyectos y sus estructuras de tablero (*board*).

**Métodos**

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


### Servicios de aplicación de ProjectMember

#### `IProjectMemberReadService`

**Responsabilidades**

- Listar miembros de un proyecto.
- Inspeccionar una membresía específica.
- Calcular recuentos de membresías.

**Métodos**

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

**Responsabilidades**

- Añadir usuarios a un proyecto.
- Cambiar roles de proyecto.
- Realizar *soft remove* y restaurar membresías.

**Métodos**

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


### Servicios de aplicación de Lane

#### `ILaneReadService`

**Responsabilidades**

- Leer carriles dentro de un proyecto.
- Proporcionar proyecciones de carriles usados por las vistas de tablero.

**Métodos**

```csharp
Task<LaneReadDto> GetByIdAsync(
    Guid laneId,
    CancellationToken ct = default);

Task<IReadOnlyList<LaneReadDto>> ListByProjectIdAsync(
    Guid projectId,
    CancellationToken ct = default);
```

#### `ILaneWriteService`

**Responsabilidades**

- Crear carriles.
- Renombrar carriles.
- Reordenar carriles dentro de un proyecto.
- Eliminar carriles y sus columnas y tareas.

**Métodos**

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


### Servicios de aplicación de Column

#### `IColumnReadService`

**Responsabilidades**

- Leer columnas dentro de un carril.
- Proporcionar datos necesarios para las vistas de tablero.

**Métodos**

```csharp
Task<ColumnReadDto> GetByIdAsync(
    Guid columnId,
    CancellationToken ct = default);

Task<IReadOnlyList<ColumnReadDto>> ListByLaneIdAsync(
    Guid laneId,
    CancellationToken ct = default);
```

#### `IColumnWriteService`

**Responsabilidades**

- Crear columnas.
- Renombrar columnas.
- Reordenar columnas dentro de un carril.
- Eliminar columnas y sus tareas.

**Métodos**

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


### Servicios de aplicación de TaskItem

#### `ITaskItemReadService`

**Responsabilidades**

- Leer tareas dentro de una columna.
- Leer detalles de tareas individuales.

**Métodos**

```csharp
Task<TaskItemReadDto> GetByIdAsync(
    Guid taskId,
    CancellationToken ct = default);

Task<IReadOnlyList<TaskItemReadDto>> ListByColumnIdAsync(
    Guid columnId,
    CancellationToken ct = default);
```

#### `ITaskItemWriteService`

**Responsabilidades**

- Crear nuevas tareas.
- Editar detalles de tareas.
- Mover tareas entre columnas y carriles.
- Eliminar tareas.

**Métodos**

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


### Servicios de aplicación de TaskNote

#### `ITaskNoteReadService`

**Responsabilidades**

- Leer notas vinculadas a una tarea.
- Leer detalles de notas individuales.

**Métodos**

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

**Responsabilidades**

- Crear notas para tareas.
- Editar contenido de notas.
- Eliminar notas.

**Métodos**

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


### Servicios de aplicación de TaskAssignment

#### `ITaskAssignmentReadService`

**Responsabilidades**

- Leer asignaciones de una tarea.
- Proporcionar vistas centradas en el usuario para asignaciones.

**Métodos**

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

**Responsabilidades**

- Añadir asignaciones a tareas.
- Cambiar roles de asignaciones.
- Eliminar asignaciones.

**Métodos**

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


### Servicios de aplicación de TaskActivity

#### `ITaskActivityReadService`

**Responsabilidades**

- Leer entradas del registro de actividad de una tarea.
- Proporcionar vistas de actividad centradas en el usuario.

**Métodos**

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

**Responsabilidades**

- Añadir actividades a tareas.

**Métodos**

```csharp
Task<TaskActivityReadDto> CreateAsync(
    Guid taskId,
    Guid userId,
    TaskActivityType type,
    ActivityPayload payload,
    CancellationToken ct = default);
```


## Interfaces de repositorio

### Convenciones comunes

- Todos los repositorios se definen como interfaces en la capa Application.
- La convención de nombres es `IXxxRepository`.
- Los repositorios trabajan con entidades de dominio, no con DTOs.
- Los repositorios no contienen lógica de negocio. Proporcionan capacidades de persistencia y consulta.
- Todos los métodos de repositorio son asíncronos.
- La concurrencia se aplica utilizando `RowVersion` en las entidades seguidas (*tracked*), junto con `ETag` y `If-Match` en la capa API.


### Repositorio de User

#### `IUserRepository`

**Responsabilidades**

- Gestionar agregados `User`.
- Proporcionar búsqueda por id y por email normalizado.
- Permitir búsqueda sencilla para vistas administrativas.

**Métodos**

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


### Repositorio de Project

#### `IProjectRepository`

**Responsabilidades**

- Gestionar agregados `Project`.
- Permitir consultas por id y por membresía de usuario.

**Métodos**

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


### Repositorio de ProjectMember

#### `IProjectMemberRepository`

**Responsabilidades**

- Gestionar entidades `ProjectMember`.
- Proporcionar vistas de membresía y recuentos.

**Métodos**

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


### Repositorio de Lane

#### `ILaneRepository`

**Responsabilidades**

- Gestionar entidades `Lane` dentro de un proyecto.

**Métodos**

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


### Repositorio de Column

#### `IColumnRepository`

**Responsabilidades**

- Gestionar entidades `Column` dentro de un carril.

**Métodos**

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


### Repositorio de TaskItem

#### `ITaskItemRepository`

**Responsabilidades**

- Gestionar entidades `TaskItem`.

**Métodos**

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


### Repositorio de TaskNote

#### `ITaskNoteRepository`

**Responsabilidades**

- Gestionar entidades `TaskNote`.

**Métodos**

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


### Repositorio de TaskAssignment

#### `ITaskAssignmentRepository`

**Responsabilidades**

- Gestionar entidades `TaskAssignment` que enlazan usuarios con tareas.

**Métodos**

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


### Repositorio de TaskActivity

#### `ITaskActivityRepository`

**Responsabilidades**

- Gestionar entidades `TaskActivity` usadas como registro de auditoría para tareas.

**Métodos**

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


## Unidad de trabajo y transacciones

La unidad de trabajo coordina la consistencia transaccional entre múltiples repositorios dentro del ámbito de una sola petición.

- Nombre de la interfaz: `IUnitOfWork`.
- Implementada en la capa Infrastructure.
- Invocada una vez por petición desde la capa Application para persistir los cambios de forma atómica.
- Garantiza que todas las operaciones de repositorio que participan en la misma transacción tengan éxito o fallen de forma conjunta.

```csharp
public interface IUnitOfWork
{
    Task<DomainMutation> SaveAsync(MutationKind kind, CancellationToken ct = default);
}
```
