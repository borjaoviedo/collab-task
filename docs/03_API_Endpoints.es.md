# Resumen de endpoints de la API

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./03_API_Endpoints.md)


## Tabla de Contenidos
- [Autenticación](#autenticación)
- [Health](#health)
- [Users](#users)
- [Projects](#projects)
- [Project Members](#project-members)
- [Lanes](#lanes)
- [Columns](#columns)
- [Task Items](#task-items)
- [Task Notes](#task-notes)
- [Task Assignments](#task-assignments)
- [Task Activities](#task-activities)
- [Vistas centradas en el usuario](#vistas-centradas-en-el-usuario)
  - [/auth/me](#authme)
  - [/members/me/count](#membersmecount)
  - [/assignments/me](#assignmentsme)
  - [/notes/me](#notesme)
  - [/activities/me](#activitiesme)

---------------------------------------------------------------------------------

Este documento define el **contrato HTTP público de la API de CollabTask**.  
Todas las rutas se agrupan por área de dominio y se alinean con las entidades definidas en [`01_Domain_Model.es.md`](01_Domain_Model.es.md).  
Las autorizaciones y nombres de políticas hacen referencia a las definiciones en [`02_Authorization_Policies.es.md`](02_Authorization_Policies.es.md).

> **Nota:** Los DTOs se enlazan a sus definiciones en [`04_DTOs.es.md`](04_DTOs.es.md) solo en su primera aparición dentro de cada tabla.  
> Referencias posteriores usan solo el nombre para mejorar la legibilidad y reducir ruido visual.

## Convenciones comunes

- Todos los timestamps están en **UTC** y usan formato ISO-8601.
- Los parámetros de consulta como `?page=&size=`, opciones de filtrado o términos de búsqueda siguen un comportamiento consistente en todos los endpoints:
  - `page` y `size` controlan la paginación (basada en 1 por defecto).
  - Los parámetros de búsqueda de texto (por ejemplo `q`) son *case-insensitive* sobre campos normalizados.
- El control de concurrencia usa las cabeceras `ETag` / `If-Match` con el campo `RowVersion` de los DTOs:
  - Las operaciones de mutación que actualizan o eliminan recursos existentes normalmente requieren `If-Match`.
  - Ante un `RowVersion` obsoleto, la API puede devolver `412 Precondition Failed` o `428 Precondition Required`.


## Autenticación

### `/auth`

| Método | Endpoint         | Auth   | Política | Descripción                                                        | Cuerpo de la petición                                | Respuesta                                                                                                  |
|--------|------------------|--------|--------|---------------------------------------------------------------------|------------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| POST   | `/auth/register` | Public | —      | Registra un nuevo usuario y emite un token de acceso               | [`UserRegisterDto`](04_DTOs.es.md#userregisterdto)   | `201 Created` + [`AuthTokenReadDto`](04_DTOs.es.md#authtokenreaddto) o `400 Bad Request` o `409 Conflict` |
| POST   | `/auth/login`    | Public | —      | Autentica credenciales y emite un nuevo token de acceso            | [`UserLoginDto`](04_DTOs.es.md#userlogindto)         | `200 OK` + `AuthTokenReadDto` o `400 Bad Request` o `401 Unauthorized`                                     |
| GET    | `/auth/me`       | JWT    | —      | Devuelve el perfil del usuario autenticado derivado de los claims  | —                                                    | `200 OK` + [`MeReadDto`](04_DTOs.es.md#mereaddto) o `401 Unauthorized`                                     |


## Health

### `/health`

| Método | Endpoint  | Auth   | Política | Descripción                              | Cuerpo de la petición | Respuesta                                       |
|--------|-----------|--------|--------|------------------------------------------|------------------------|-------------------------------------------------|
| GET    | `/health` | Public | —      | Comprobación básica de salud de la API   | —                      | `200 OK` (payload simple de salud o texto `OK`) |


## [Users](01_Domain_Model.es.md#user)

### `/users`

| Método | Endpoint                 | Auth | Política            | Descripción                                                   | Cuerpo de la petición                                              | Respuesta                                                                                                                                  |
|--------|--------------------------|------|-------------------|---------------------------------------------------------------|--------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/users`                 | JWT  | SystemAdminPolicy | Lista todos los usuarios (vista de administración)           | —                                                                  | `200 OK` + lista de [`UserReadDto`](04_DTOs.es.md#userreaddto) o `401 Unauthorized` o `403 Forbidden`                                     |
| GET    | `/users/{userId}`        | JWT  | SystemAdminPolicy | Obtiene los detalles de un usuario específico por su ID      | —                                                                  | `200 OK` + `UserReadDto` (con `ETag`) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                            |
| GET    | `/users/by-email`        | JWT  | SystemAdminPolicy | Busca un usuario por email normalizado                       | Query: `email`                                                     | `200 OK` + `UserReadDto` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                                         |
| PATCH  | `/users/{userId}/rename` | JWT  | — (authenticated) | Renombra un usuario (concurrencia optimista vía `If-Match`)   | [`UserRenameDto`](04_DTOs.es.md#userrenamedto)                     | `200 OK` + `UserReadDto` (con nuevo `ETag`) o `400 Bad Request` o `401 Unauthorized` o `404 Not Found` o `409 Conflict` o `412/428`      |
| PATCH  | `/users/{userId}/role`   | JWT  | SystemAdminPolicy | Cambia el rol de un usuario                                  | [`UserChangeRoleDto`](04_DTOs.es.md#userchangeroledto)             | `200 OK` + `UserReadDto` o `400 Bad Request` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` o `412/428`       |
| DELETE | `/users/{userId}`        | JWT  | SystemAdminPolicy | Elimina un usuario utilizando concurrencia optimista          | — (requiere cabecera `If-Match`)                                  | `204 No Content` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` o `412 Precondition Failed` o `428`           |


## [Projects](01_Domain_Model.es.md#project)

### `/projects`

| Método | Endpoint                     | Auth | Política             | Descripción                                                               | Cuerpo de la petición                                           | Respuesta                                                                                                                                |
|--------|------------------------------|------|--------------------|---------------------------------------------------------------------------|-----------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects`                  | JWT  | —                  | Lista los proyectos visibles para el usuario autenticado                  | Filtros opcionales (`name`, paginación, etc.)                   | `200 OK` + lista de [`ProjectReadDto`](04_DTOs.es.md#projectreaddto)                                                                    |
| GET    | `/projects/{projectId}`      | JWT  | ProjectReaderPolicy | Obtiene un proyecto por ID (requiere acceso a nivel de proyecto)         | —                                                               | `200 OK` + `ProjectReadDto` (con `ETag`) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                      |
| GET    | `/projects/me`               | JWT  | —                  | Lista los proyectos donde el usuario autenticado es miembro/propietario   | —                                                               | `200 OK` + lista de `ProjectReadDto` o `401 Unauthorized`                                                                               |
| GET    | `/projects/users/{userId}`   | JWT  | SystemAdminPolicy  | Lista los proyectos de un usuario específico (vista global de administrador) | —                                                            | `200 OK` + lista de `ProjectReadDto` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                          |
| POST   | `/projects`                  | JWT  | —                  | Crea un nuevo proyecto cuyo propietario es el usuario autenticado         | [`ProjectCreateDto`](04_DTOs.es.md#projectcreatedto)            | `201 Created` + cabecera `Location` + `ProjectReadDto` o `400 Bad Request` o `401 Unauthorized` o `409 Conflict`                        |
| PATCH  | `/projects/{projectId}/rename` | JWT | ProjectAdminPolicy | Renombra un proyecto (solo admins/owners, requiere `If-Match`)           | [`ProjectRenameDto`](04_DTOs.es.md#projectrenamedto)            | `200 OK` + `ProjectReadDto` (con nuevo `ETag`) o `400 Bad Request` o `401 Unauthorized` o `403 Forbidden` o `404` o `409` o `412/428`  |
| DELETE | `/projects/{projectId}`      | JWT  | ProjectOwnerPolicy | Elimina un proyecto y sus recursos relacionados (solo owner, `If-Match`) | — (requiere cabecera `If-Match`)                               | `204 No Content` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` o `412 Precondition Failed` o `428`        |


## [Project Members](01_Domain_Model.es.md#projectmember)

### `/projects/{projectId}/members`

| Método | Endpoint                                     | Auth | Política              | Descripción                                                                       | Cuerpo de la petición                                                   | Respuesta                                                                                                                                  |
|--------|----------------------------------------------|------|---------------------|-----------------------------------------------------------------------------------|--------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/members`              | JWT  | ProjectReaderPolicy | Lista los miembros de un proyecto (puede incluir usuarios soft-removed opcionalmente) | Query: `includeRemoved`                                             | `200 OK` + lista de [`ProjectMemberReadDto`](04_DTOs.es.md#projectmemberreaddto) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` |
| GET    | `/projects/{projectId}/members/{userId}`     | JWT  | ProjectReaderPolicy | Obtiene una entrada de membresía específica dentro de un proyecto                 | —                                                                        | `200 OK` + `ProjectMemberReadDto` (con `ETag`) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                   |
| GET    | `/projects/{projectId}/members/{userId}/role` | JWT | ProjectReaderPolicy | Obtiene el rol efectivo de proyecto para un usuario dado                          | —                                                                        | `200 OK` + [`ProjectMemberRoleReadDto`](04_DTOs.es.md#projectmemberrolereaddto) o `401` o `403` o `404`                                   |
| POST   | `/projects/{projectId}/members`              | JWT  | ProjectAdminPolicy  | Añade un usuario al proyecto (solo admins, crea la membresía)                     | [`ProjectMemberCreateDto`](04_DTOs.es.md#projectmembercreatedto)        | `201 Created` + cabecera `Location` + `ProjectMemberReadDto` (con `ETag`) o `400` o `401` o `403` o `404` o `409`                          |
| PATCH  | `/projects/{projectId}/members/{userId}/role` | JWT | ProjectAdminPolicy  | Cambia el rol de un miembro de proyecto (requiere `If-Match`)                     | [`ProjectMemberChangeRoleDto`](04_DTOs.es.md#projectmemberchangeroledto) | `200 OK` + `ProjectMemberReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                     |
| PATCH  | `/projects/{projectId}/members/{userId}/remove` | JWT | ProjectAdminPolicy | Marca como eliminado (soft-remove) a un miembro del proyecto (con `If-Match`)     | — (requiere cabecera `If-Match`)                                       | `200 OK` + `ProjectMemberReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                     |
| PATCH  | `/projects/{projectId}/members/{userId}/restore` | JWT | ProjectAdminPolicy | Restaura un miembro previamente eliminado del proyecto (con `If-Match`)           | — (requiere cabecera `If-Match`)                                       | `200 OK` + `ProjectMemberReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                     |


## [Lanes](01_Domain_Model.es.md#lane)

### `/projects/{projectId}/lanes`

| Método | Endpoint                           | Auth | Política              | Descripción                                                    | Cuerpo de la petición                                  | Respuesta                                                                                                                                  |
|--------|------------------------------------|------|---------------------|----------------------------------------------------------------|--------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes`      | JWT  | ProjectReaderPolicy | Lista los carriles de un proyecto                                | —                                                      | `200 OK` + lista de [`LaneReadDto`](04_DTOs.es.md#lanereaddto) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                   |
| GET    | `/projects/{projectId}/lanes/{laneId}` | JWT | ProjectReaderPolicy | Obtiene un carriles concreto                                      | —                                                      | `200 OK` + `LaneReadDto` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                                        |
| POST   | `/projects/{projectId}/lanes`      | JWT  | ProjectAdminPolicy  | Crea un nuevo carril dentro de un proyecto                      | [`LaneCreateDto`](04_DTOs.es.md#lanecreatedto)        | `201 Created` + cabecera `Location` + `LaneReadDto` o `400 Bad Request` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` |
| PUT    | `/projects/{projectId}/lanes/{laneId}/rename` | JWT | ProjectAdminPolicy | Renombra un carril (con `If-Match`)                            | [`LaneRenameDto`](04_DTOs.es.md#lanerenamedto)        | `200 OK` + `LaneReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                             |
| PUT    | `/projects/{projectId}/lanes/{laneId}/reorder` | JWT | ProjectAdminPolicy | Cambia el orden de visualización de un carril en el proyecto    | [`LaneReorderDto`](04_DTOs.es.md#lanereorderdto)      | `200 OK` + `LaneReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                             |
| DELETE | `/projects/{projectId}/lanes/{laneId}` | JWT | ProjectAdminPolicy | Elimina un carril y sus columnas/tareas (con `If-Match`)        | — (requiere cabecera `If-Match`)                      | `204 No Content` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` o `412/428`                                   |


## [Columns](01_Domain_Model.es.md#column)

### `/projects/{projectId}/lanes/{laneId}/columns`

| Método | Endpoint                                                 | Auth | Política              | Descripción                                                    | Cuerpo de la petición                                       | Respuesta                                                                                                                                  |
|--------|----------------------------------------------------------|------|---------------------|----------------------------------------------------------------|--------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns`           | JWT  | ProjectReaderPolicy | Lista las columnas de un lane                                  | —                                                            | `200 OK` + lista de [`ColumnReadDto`](04_DTOs.es.md#columnreaddto) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`               |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}` | JWT | ProjectReaderPolicy | Obtiene una columna concreta                                   | —                                                            | `200 OK` + `ColumnReadDto` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                                       |
| POST   | `/projects/{projectId}/lanes/{laneId}/columns`           | JWT  | ProjectAdminPolicy  | Crea una nueva columna                                        | [`ColumnCreateDto`](04_DTOs.es.md#columncreatedto)          | `201 Created` + cabecera `Location` + `ColumnReadDto` o `400` o `401` o `403` o `404` o `409`                                            |
| PUT    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename` | JWT | ProjectAdminPolicy | Renombra una columna (con `If-Match`)                         | [`ColumnRenameDto`](04_DTOs.es.md#columnrenamedto)          | `200 OK` + `ColumnReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                           |
| PUT    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder` | JWT | ProjectAdminPolicy | Cambia el orden de una columna dentro del lane (con `If-Match`) | [`ColumnReorderDto`](04_DTOs.es.md#columnreorderdto)        | `200 OK` + `ColumnReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                           |
| DELETE | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}` | JWT | ProjectAdminPolicy | Elimina una columna y sus tareas (con `If-Match`)             | — (requiere cabecera `If-Match`)                            | `204 No Content` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` o `412/428`                                   |


## [Task Items](01_Domain_Model.es.md#taskitem)

### `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks`

| Método | Endpoint                                                                | Auth | Política               | Descripción                                                                   | Cuerpo de la petición                                          | Respuesta                                                                                                                                  |
|--------|-------------------------------------------------------------------------|------|----------------------|-------------------------------------------------------------------------------|----------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks`        | JWT  | ProjectReaderPolicy  | Lista las tareas de una columna                                              | —                                                              | `200 OK` + lista de [`TaskItemReadDto`](04_DTOs.es.md#taskitemreaddto) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`          |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}` | JWT | ProjectReaderPolicy  | Obtiene una tarea concreta                                                    | —                                                              | `200 OK` + `TaskItemReadDto` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                                     |
| POST   | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks`        | JWT  | ProjectMemberPolicy  | Crea una nueva tarea en la columna indicada                                   | [`TaskItemCreateDto`](04_DTOs.es.md#taskitemcreatedto)        | `201 Created` + cabecera `Location` + `TaskItemReadDto` (con `ETag`) o `400` o `401` o `403` o `404` o `409`                              |
| PATCH  | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit` | JWT | ProjectMemberPolicy | Edita una tarea (título, descripción, fecha límite, etc., con `If-Match`)    | [`TaskItemEditDto`](04_DTOs.es.md#taskitemeditdto)            | `200 OK` + `TaskItemReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                         |
| PUT    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/move` | JWT | ProjectMemberPolicy | Mueve una tarea (por ejemplo, entre columnas o reordenación dentro de la columna) | [`TaskItemMoveDto`](04_DTOs.es.md#taskitemmovedto)      | `200 OK` + `TaskItemReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                         |
| DELETE | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}` | JWT | ProjectMemberPolicy | Elimina una tarea (con `If-Match`)                                           | — (requiere cabecera `If-Match`)                              | `204 No Content` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` o `412/428`                                   |


## [Task Notes](01_Domain_Model.es.md#tasknote)

### `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes`

| Método | Endpoint                                                                                 | Auth | Política               | Descripción                                                          | Cuerpo de la petición                                          | Respuesta                                                                                                                                   |
|--------|------------------------------------------------------------------------------------------|------|----------------------|----------------------------------------------------------------------|----------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes`          | JWT  | ProjectReaderPolicy  | Lista las notas de una tarea                                        | —                                                              | `200 OK` + lista de [`TaskNoteReadDto`](04_DTOs.es.md#tasknotereaddto) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`           |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}` | JWT  | ProjectReaderPolicy  | Obtiene una nota concreta de una tarea                              | —                                                              | `200 OK` + `TaskNoteReadDto` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                                      |
| POST   | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes`          | JWT  | ProjectMemberPolicy  | Añade una nueva nota a la tarea                                     | [`TaskNoteCreateDto`](04_DTOs.es.md#tasknotecreatedto)        | `201 Created` + cabecera `Location` + `TaskNoteReadDto` (con `ETag`) o `400` o `401` o `403` o `404` o `409`                               |
| PATCH  | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit` | JWT | ProjectMemberPolicy | Edita una nota existente (con `If-Match`)                           | [`TaskNoteEditDto`](04_DTOs.es.md#tasknoteeditdto)            | `200 OK` + `TaskNoteReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                           |
| DELETE | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}` | JWT  | ProjectMemberPolicy  | Elimina una nota (con `If-Match`)                                   | — (requiere cabecera `If-Match`)                              | `204 No Content` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` o `412/428`                                     |


## [Task Assignments](01_Domain_Model.es.md#taskassignment)

### `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments`

| Método | Endpoint                                                                                         | Auth | Política               | Descripción                                                          | Cuerpo de la petición                                                     | Respuesta                                                                                                                                  |
|--------|--------------------------------------------------------------------------------------------------|------|----------------------|----------------------------------------------------------------------|----------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments`            | JWT  | ProjectReaderPolicy  | Lista las asignaciones de una tarea                                 | —                                                                          | `200 OK` + lista de [`TaskAssignmentReadDto`](04_DTOs.es.md#taskassignmentreaddto) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}`   | JWT  | ProjectReaderPolicy  | Obtiene la asignación de un usuario concreto para la tarea          | —                                                                          | `200 OK` + `TaskAssignmentReadDto` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                               |
| POST   | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments`            | JWT  | ProjectMemberPolicy  | Añade una asignación (usuario + rol) a la tarea                     | [`TaskAssignmentCreateDto`](04_DTOs.es.md#taskassignmentcreatedto)       | `201 Created` + cabecera `Location` + `TaskAssignmentReadDto` o `400` o `401` o `403` o `404` o `409`                                     |
| PATCH  | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}/role` | JWT | ProjectMemberPolicy | Cambia el rol de una asignación (con `If-Match`)                    | [`TaskAssignmentChangeRoleDto`](04_DTOs.es.md#taskassignmentchangeroledto) | `200 OK` + `TaskAssignmentReadDto` o `400` o `401` o `403` o `404` o `409` o `412/428`                                                   |
| DELETE | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}`   | JWT  | ProjectMemberPolicy  | Elimina una asignación (con `If-Match`)                             | — (puede requerir cabecera `If-Match` según la implementación)            | `204 No Content` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found` o `409 Conflict` o `412/428`                                   |


## [Task Activities](01_Domain_Model.es.md#taskactivity)

### `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities`

| Método | Endpoint                                                                                            | Auth | Política               | Descripción                                                | Cuerpo de la petición | Respuesta                                                                                                                                    |
|--------|-----------------------------------------------------------------------------------------------------|------|----------------------|------------------------------------------------------------|-----------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities`                | JWT  | ProjectReaderPolicy  | Lista las entradas del registro de actividad de una tarea | —                     | `200 OK` + lista de [`TaskActivityReadDto`](04_DTOs.es.md#taskactivityreaddto) o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`    |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities/{activityId}`   | JWT  | ProjectReaderPolicy  | Obtiene una entrada concreta de actividad de tarea        | —                     | `200 OK` + `TaskActivityReadDto` o `401 Unauthorized` o `403 Forbidden` o `404 Not Found`                                                   |


## Vistas centradas en el usuario

Estos endpoints proporcionan vistas acotadas al **usuario autenticado actual** o a usuarios específicos a través de proyectos.

### `/auth/me`

Ya cubierto en [Autenticación](#autenticación).


### `/members/me/count`

| Método | Endpoint            | Auth | Política | Descripción                                                                  | Cuerpo de la petición | Respuesta                                                                                                              |
|--------|---------------------|------|--------|------------------------------------------------------------------------------|-----------------------|------------------------------------------------------------------------------------------------------------------------|
| GET    | `/members/me/count` | JWT  | —      | Cuenta los proyectos activos donde el usuario autenticado es miembro        | —                     | `200 OK` + [`ProjectMemberCountReadDto`](04_DTOs.es.md#projectmembercountreaddto) o `401 Unauthorized`                |

### `/members/{userId}/count`

| Método | Endpoint                | Auth | Política            | Descripción                                              | Cuerpo de la petición | Respuesta                                                                                                              |
|--------|-------------------------|------|-------------------|----------------------------------------------------------|-----------------------|------------------------------------------------------------------------------------------------------------------------|
| GET    | `/members/{userId}/count` | JWT | SystemAdminPolicy | Cuenta los proyectos activos para un usuario específico | —                     | `200 OK` + `ProjectMemberCountReadDto` o `401 Unauthorized` o `403 Forbidden`                                         |


### `/assignments/me`

| Método | Endpoint            | Auth | Política | Descripción                                                                   | Cuerpo de la petición | Respuesta                                                                                              |
|--------|---------------------|------|--------|-------------------------------------------------------------------------------|-----------------------|--------------------------------------------------------------------------------------------------------|
| GET    | `/assignments/me`   | JWT  | —      | Lista las asignaciones de tareas del usuario autenticado en todos los proyectos | —                   | `200 OK` + lista de `TaskAssignmentReadDto` o `401 Unauthorized`                                      |

### `/assignments/users/{userId}`

| Método | Endpoint                      | Auth | Política            | Descripción                                         | Cuerpo de la petición | Respuesta                                                                                              |
|--------|-------------------------------|------|-------------------|-----------------------------------------------------|-----------------------|--------------------------------------------------------------------------------------------------------|
| GET    | `/assignments/users/{userId}` | JWT  | SystemAdminPolicy | Lista las asignaciones de tareas de un usuario dado | —                     | `200 OK` + lista de `TaskAssignmentReadDto` o `401 Unauthorized` o `403 Forbidden`                    |


### `/notes/me`

| Método | Endpoint       | Auth | Política | Descripción                                         | Cuerpo de la petición | Respuesta                                                                                     |
|--------|----------------|------|--------|-----------------------------------------------------|-----------------------|-----------------------------------------------------------------------------------------------|
| GET    | `/notes/me`    | JWT  | —      | Lista las notas creadas por el usuario autenticado  | —                     | `200 OK` + lista de `TaskNoteReadDto` o `401 Unauthorized`                                   |

### `/notes/users/{userId}`

| Método | Endpoint                 | Auth | Política            | Descripción                                         | Cuerpo de la petición | Respuesta                                                                                     |
|--------|--------------------------|------|-------------------|-----------------------------------------------------|-----------------------|-----------------------------------------------------------------------------------------------|
| GET    | `/notes/users/{userId}` | JWT  | SystemAdminPolicy | Lista las notas creadas por el usuario especificado | —                     | `200 OK` + lista de `TaskNoteReadDto` o `401 Unauthorized` o `403 Forbidden`                 |


### `/activities/me`

| Método | Endpoint          | Auth | Política | Descripción                                                  | Cuerpo de la petición | Respuesta                                                                                     |
|--------|-------------------|------|--------|--------------------------------------------------------------|-----------------------|-----------------------------------------------------------------------------------------------|
| GET    | `/activities/me`  | JWT  | —      | Lista las actividades de tareas realizadas por el usuario autenticado | —             | `200 OK` + lista de `TaskActivityReadDto` o `401 Unauthorized`                               |

### `/activities/users/{userId}`

| Método | Endpoint                       | Auth | Política            | Descripción                                               | Cuerpo de la petición | Respuesta                                                                                     |
|--------|--------------------------------|------|-------------------|-----------------------------------------------------------|-----------------------|-----------------------------------------------------------------------------------------------|
| GET    | `/activities/users/{userId}`  | JWT  | SystemAdminPolicy | Lista las actividades de tareas realizadas por un usuario concreto | —           | `200 OK` + lista de `TaskActivityReadDto` o `401 Unauthorized` o `403 Forbidden`            |
