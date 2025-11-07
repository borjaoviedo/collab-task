# API Endpoints Overview

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./03_API_Endpoints.es.md)


## Table of Contents
- [Authentication](#authentication)
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
- [Me & User-centric views](#me--user-centric-views)
  - [/auth/me](#authme)
  - [/members/me/count](#membersmecount)
  - [/assignments/me](#assignmentsme)
  - [/notes/me](#notesme)
  - [/activities/me](#activitiesme)

---------------------------------------------------------------------------------

This document defines the **public HTTP contract of the CollabTask API**.  
All routes are grouped by domain area and aligned with the entities defined in [`01_Domain_Model.md`](01_Domain_Model.md).  
Authorization and policy names refer to the definitions in [`02_Authorization_Policies.md`](02_Authorization_Policies.md).

> **Note:** DTOs are linked to their definitions in [`04_DTOs.md`](04_DTOs.md) only upon their first appearance within each table.  
> Subsequent references use plain names to improve readability and reduce visual clutter.

## Common conventions

- All timestamps are in **UTC** and use ISO-8601 format.
- Query parameters such as `?page=&size=`, filtering options, or search terms follow consistent behavior across endpoints:
  - `page` and `size` control pagination (1-based by default).
  - Text search parameters (e.g. `q`) are case-insensitive over normalized fields.
- Concurrency control uses the `ETag` / `If-Match` headers with the `RowVersion` field from DTOs:
  - Mutating operations that update or delete existing resources typically require `If-Match`.
  - On stale `RowVersion`, the API may return `412 Precondition Failed` or `428 Precondition Required`.


## Authentication

### `/auth`

| Method | Endpoint         | Auth   | Policy | Description                                                   | Request Body                                    | Response                                                                                                 |
|--------|------------------|--------|--------|---------------------------------------------------------------|-------------------------------------------------|----------------------------------------------------------------------------------------------------------|
| POST   | `/auth/register` | Public | —      | Register a new user and issue an access token                 | [`UserRegisterDto`](04_DTOs.md#userregisterdto) | `201 Created` + [`AuthTokenReadDto`](04_DTOs.md#authtokenreaddto) or `400 Bad Request` or `409 Conflict` |
| POST   | `/auth/login`    | Public | —      | Authenticate credentials and issue a new access token         | [`UserLoginDto`](04_DTOs.md#userlogindto)       | `200 OK` + `AuthTokenReadDto` or `400 Bad Request` or `401 Unauthorized`                                 |
| GET    | `/auth/me`       | JWT    | —      | Return the authenticated user profile derived from JWT claims | —                                               | `200 OK` + [`MeReadDto`](04_DTOs.md#mereaddto) or `401 Unauthorized`                                     |


## Health

### `/health`

| Method | Endpoint  | Auth   | Policy | Description                         | Request Body | Response                                      |
|--------|-----------|--------|--------|-------------------------------------|--------------|-----------------------------------------------|
| GET    | `/health` | Public | —      | Basic health probe for the API node | —            | `200 OK` (simple health payload or `OK` text) |


## [Users](01_Domain_Model.md#user)

### `/users`

| Method | Endpoint              | Auth | Policy             | Description                                              | Request Body                                                        | Response                                                                                                                                      |
|--------|-----------------------|------|--------------------|----------------------------------------------------------|----------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/users`              | JWT  | SystemAdminPolicy  | List all users (admin view)                             | —                                                                    | `200 OK` + [`UserReadDto`](04_DTOs.md#userreaddto) list or `401 Unauthorized` or `403 Forbidden`                                             |
| GET    | `/users/{userId}`     | JWT  | SystemAdminPolicy  | Get details for a specific user by ID                   | —                                                                    | `200 OK` + `UserReadDto` (with `ETag`) or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                           |
| GET    | `/users/by-email`     | JWT  | SystemAdminPolicy  | Look up a user by normalized email                      | Query: `email`                                                       | `200 OK` + `UserReadDto` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                                         |
| PATCH  | `/users/{userId}/rename` | JWT | — (authenticated) | Rename a user (optimistic concurrency via `If-Match`)   | [`UserRenameDto`](04_DTOs.md#userrenamedto)                         | `200 OK` + `UserReadDto` (with new `ETag`) or `400 Bad Request` or `401 Unauthorized` or `404 Not Found` or `409 Conflict` or `412/428`     |
| PATCH  | `/users/{userId}/role`   | JWT | SystemAdminPolicy | Change the role of a user                               | [`UserChangeRoleDto`](04_DTOs.md#userchangeroledto)                 | `200 OK` + `UserReadDto` or `400 Bad Request` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` or `412/428`    |
| DELETE | `/users/{userId}`     | JWT  | SystemAdminPolicy  | Delete a user using optimistic concurrency              | — (requires `If-Match` header)                                      | `204 No Content` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` or `412 Precondition Failed` or `428`        |


## [Projects](01_Domain_Model.md#project)

### `/projects`

| Method | Endpoint            | Auth | Policy            | Description                                                       | Request Body                                                        | Response                                                                                                                                 |
|--------|---------------------|------|-------------------|-------------------------------------------------------------------|----------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects`         | JWT  | —                 | List projects visible to the authenticated user                   | Optional filter query (`name`, pagination, etc.)                     | `200 OK` + [`ProjectReadDto`](04_DTOs.md#projectreaddto) list                                                                           |
| GET    | `/projects/{projectId}`    | JWT  | ProjectReaderPolicy | Get project by ID (requires project-level access)                 | —                                                                    | `200 OK` + `ProjectReadDto` (with `ETag`) or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                   |
| GET    | `/projects/me`      | JWT  | —                 | List projects where the authenticated user is a member/owner      | —                                                                    | `200 OK` + `ProjectReadDto` list or `401 Unauthorized`                                                                                  |
| GET    | `/projects/users/{userId}` | JWT | SystemAdminPolicy | List projects for a specific user (admin-wide view)              | —                                                                    | `200 OK` + `ProjectReadDto` list or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                           |
| POST   | `/projects`         | JWT  | —                 | Create a new project owned by the authenticated user              | [`ProjectCreateDto`](04_DTOs.md#projectcreatedto)                   | `201 Created` + `Location` header + `ProjectReadDto` or `400 Bad Request` or `401 Unauthorized` or `409 Conflict`                       |
| PATCH  | `/projects/{projectId}/rename` | JWT | ProjectAdminPolicy | Rename a project (admins/owners only, with `If-Match`)           | [`ProjectRenameDto`](04_DTOs.md#projectrenamedto)                   | `200 OK` + `ProjectReadDto` (with new `ETag`) or `400 Bad Request` or `401 Unauthorized` or `403 Forbidden` or `404` or `409` or `412/428` |
| DELETE | `/projects/{projectId}`    | JWT  | ProjectOwnerPolicy | Delete a project and its related resources (owner-only, `If-Match`) | — (requires `If-Match` header)                                      | `204 No Content` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` or `412 Precondition Failed` or `428`   |


## [Project Members](01_Domain_Model.md#projectmember)

### `/projects/{projectId}/members`

| Method | Endpoint                               | Auth | Policy              | Description                                                                | Request Body                                                                  | Response                                                                                                                                 |
|--------|----------------------------------------|------|---------------------|----------------------------------------------------------------------------|-------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/members`        | JWT  | ProjectReaderPolicy | List members of a project (optionally including soft-removed users)       | Query: `includeRemoved`                                                       | `200 OK` + [`ProjectMemberReadDto`](04_DTOs.md#projectmemberreaddto) list or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`   |
| GET    | `/projects/{projectId}/members/{userId}` | JWT | ProjectReaderPolicy | Get a specific membership entry within a project                          | —                                                                             | `200 OK` + `ProjectMemberReadDto` (with `ETag`) or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                             |
| GET    | `/projects/{projectId}/members/{userId}/role` | JWT | ProjectReaderPolicy | Get the effective project role for a given user                           | —                                                                             | `200 OK` + [`ProjectMemberRoleReadDto`](04_DTOs.md#projectmemberrolereaddto) or `401` or `403` or `404`                                 |
| POST   | `/projects/{projectId}/members`        | JWT  | ProjectAdminPolicy  | Add a user to the project (admin-only, creates membership)                | [`ProjectMemberCreateDto`](04_DTOs.md#projectmembercreatedto)                | `201 Created` + `Location` header + `ProjectMemberReadDto` (with `ETag`) or `400` or `401` or `403` or `404` or `409`                    |
| PATCH  | `/projects/{projectId}/members/{userId}/role` | JWT | ProjectAdminPolicy  | Change the role of a project member (with `If-Match`)                     | [`ProjectMemberChangeRoleDto`](04_DTOs.md#projectmemberchangeroledto)        | `200 OK` + `ProjectMemberReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                             |
| PATCH  | `/projects/{projectId}/members/{userId}/remove` | JWT | ProjectAdminPolicy | Soft-remove a member from the project (with `If-Match`)                   | — (requires `If-Match` header)                                               | `200 OK` + `ProjectMemberReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                             |
| PATCH  | `/projects/{projectId}/members/{userId}/restore` | JWT | ProjectAdminPolicy | Restore a previously removed project member (with `If-Match`)             | — (requires `If-Match` header)                                               | `200 OK` + `ProjectMemberReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                             |


## [Lanes](01_Domain_Model.md#lane)

### `/projects/{projectId}/lanes`

| Method | Endpoint                             | Auth | Policy              | Description                                              | Request Body                                               | Response                                                                                                                                 |
|--------|--------------------------------------|------|---------------------|----------------------------------------------------------|------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes`        | JWT  | ProjectReaderPolicy | List lanes for a project                                 | —                                                          | `200 OK` + [`LaneReadDto`](04_DTOs.md#lanereaddto) list or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                     |
| GET    | `/projects/{projectId}/lanes/{laneId}` | JWT | ProjectReaderPolicy | Get a single lane                                       | —                                                          | `200 OK` + `LaneReadDto` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                                    |
| POST   | `/projects/{projectId}/lanes`        | JWT  | ProjectAdminPolicy  | Create a new lane within a project                       | [`LaneCreateDto`](04_DTOs.md#lanecreatedto)               | `201 Created` + `Location` header + `LaneReadDto` or `400 Bad Request` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` |
| PUT    | `/projects/{projectId}/lanes/{laneId}/rename` | JWT | ProjectAdminPolicy | Rename a lane (with `If-Match`)                          | [`LaneRenameDto`](04_DTOs.md#lanerenamedto)               | `200 OK` + `LaneReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                                     |
| PUT    | `/projects/{projectId}/lanes/{laneId}/reorder` | JWT | ProjectAdminPolicy | Change the display order of a lane within the project    | [`LaneReorderDto`](04_DTOs.md#lanereorderdto)             | `200 OK` + `LaneReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                                     |
| DELETE | `/projects/{projectId}/lanes/{laneId}` | JWT | ProjectAdminPolicy | Delete a lane and its columns/tasks (with `If-Match`)    | — (requires `If-Match` header)                            | `204 No Content` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` or `412/428`                             |


## [Columns](01_Domain_Model.md#column)

### `/projects/{projectId}/lanes/{laneId}/columns`

| Method | Endpoint                                                 | Auth | Policy              | Description                                                  | Request Body                                                   | Response                                                                                                                                 |
|--------|----------------------------------------------------------|------|---------------------|--------------------------------------------------------------|----------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns`           | JWT  | ProjectReaderPolicy | List columns within a lane                                  | —                                                              | `200 OK` + [`ColumnReadDto`](04_DTOs.md#columnreaddto) list or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                 |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}` | JWT | ProjectReaderPolicy | Get a single column                                         | —                                                              | `200 OK` + `ColumnReadDto` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                                  |
| POST   | `/projects/{projectId}/lanes/{laneId}/columns`           | JWT  | ProjectAdminPolicy  | Create a new column                                         | [`ColumnCreateDto`](04_DTOs.md#columncreatedto)               | `201 Created` + `Location` header + `ColumnReadDto` or `400` or `401` or `403` or `404` or `409`                                        |
| PUT    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename` | JWT | ProjectAdminPolicy | Rename a column (with `If-Match`)                           | [`ColumnRenameDto`](04_DTOs.md#columnrenamedto)               | `200 OK` + `ColumnReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                                    |
| PUT    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder` | JWT | ProjectAdminPolicy | Change column order within the lane (with `If-Match`)       | [`ColumnReorderDto`](04_DTOs.md#columnreorderdto)             | `200 OK` + `ColumnReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                                    |
| DELETE | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}` | JWT | ProjectAdminPolicy | Delete a column and its tasks (with `If-Match`)             | — (requires `If-Match` header)                                | `204 No Content` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` or `412/428`                             |


## [Task Items](01_Domain_Model.md#taskitem)

### `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks`

| Method | Endpoint                                                               | Auth | Policy               | Description                                                           | Request Body                                                       | Response                                                                                                                                 |
|--------|------------------------------------------------------------------------|------|----------------------|-----------------------------------------------------------------------|--------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks`        | JWT  | ProjectReaderPolicy  | List tasks within a column                                           | —                                                                  | `200 OK` + [`TaskItemReadDto`](04_DTOs.md#taskitemreaddto) list or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`            |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}` | JWT | ProjectReaderPolicy  | Get a single task item                                               | —                                                                  | `200 OK` + `TaskItemReadDto` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                               |
| POST   | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks`        | JWT  | ProjectMemberPolicy  | Create a new task in the given column                                | [`TaskItemCreateDto`](04_DTOs.md#taskitemcreatedto)               | `201 Created` + `Location` header + `TaskItemReadDto` (with `ETag`) or `400` or `401` or `403` or `404` or `409`                        |
| PATCH  | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit` | JWT | ProjectMemberPolicy | Edit task fields (title, description, due date, etc., with `If-Match`) | [`TaskItemEditDto`](04_DTOs.md#taskitemeditdto)                   | `200 OK` + `TaskItemReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                                 |
| PUT    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/move` | JWT | ProjectMemberPolicy | Move a task (e.g. between columns or reorder within a column)       | [`TaskItemMoveDto`](04_DTOs.md#taskitemmovedto)                   | `200 OK` + `TaskItemReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                                 |
| DELETE | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}` | JWT | ProjectMemberPolicy | Delete a task (with `If-Match`)                                     | — (requires `If-Match` header)                                    | `204 No Content` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` or `412/428`                             |


## [Task Notes](01_Domain_Model.md#tasknote)

### `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes`

| Method | Endpoint                                                                                 | Auth | Policy               | Description                                                             | Request Body                                                       | Response                                                                                                                                    |
|--------|------------------------------------------------------------------------------------------|------|----------------------|-------------------------------------------------------------------------|--------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes`          | JWT  | ProjectReaderPolicy  | List notes for a task                                                  | —                                                                  | `200 OK` + [`TaskNoteReadDto`](04_DTOs.md#tasknotereaddto) list or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`               |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}` | JWT  | ProjectReaderPolicy  | Get a single task note                                                 | —                                                                  | `200 OK` + `TaskNoteReadDto` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                                  |
| POST   | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes`          | JWT  | ProjectMemberPolicy  | Add a new note to the task                                             | [`TaskNoteCreateDto`](04_DTOs.md#tasknotecreatedto)               | `201 Created` + `Location` header + `TaskNoteReadDto` (with `ETag`) or `400` or `401` or `403` or `404` or `409`                           |
| PATCH  | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit` | JWT | ProjectMemberPolicy | Edit an existing note (with `If-Match`)                               | [`TaskNoteEditDto`](04_DTOs.md#tasknoteeditdto)                   | `200 OK` + `TaskNoteReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                                    |
| DELETE | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}` | JWT  | ProjectMemberPolicy  | Delete a note (with `If-Match`)                                       | — (requires `If-Match` header)                                    | `204 No Content` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` or `412/428`                                |


## [Task Assignments](01_Domain_Model.md#taskassignment)

### `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments`

| Method | Endpoint                                                                                         | Auth | Policy               | Description                                                         | Request Body                                                                 | Response                                                                                                                                 |
|--------|--------------------------------------------------------------------------------------------------|------|----------------------|---------------------------------------------------------------------|-------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments`            | JWT  | ProjectReaderPolicy  | List assignments for a task                                        | —                                                                             | `200 OK` + [`TaskAssignmentReadDto`](04_DTOs.md#taskassignmentreaddto) list or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}`   | JWT  | ProjectReaderPolicy  | Get assignment of a specific user for the task                     | —                                                                             | `200 OK` + `TaskAssignmentReadDto` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                         |
| POST   | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments`            | JWT  | ProjectMemberPolicy  | Add an assignment (user + role) to the task                        | [`TaskAssignmentCreateDto`](04_DTOs.md#taskassignmentcreatedto)             | `201 Created` + `Location` header + `TaskAssignmentReadDto` or `400` or `401` or `403` or `404` or `409`                               |
| PATCH  | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}/role` | JWT | ProjectMemberPolicy | Change the role of an assignment (with `If-Match`)                 | [`TaskAssignmentChangeRoleDto`](04_DTOs.md#taskassignmentchangeroledto)     | `200 OK` + `TaskAssignmentReadDto` or `400` or `401` or `403` or `404` or `409` or `412/428`                                           |
| DELETE | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}`   | JWT  | ProjectMemberPolicy  | Remove an assignment (with `If-Match`)                             | — (may require `If-Match` header depending on implementation)               | `204 No Content` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found` or `409 Conflict` or `412/428`                             |


## [Task Activities](01_Domain_Model.md#taskactivity)

### `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities`

| Method | Endpoint                                                                                           | Auth | Policy               | Description                                                   | Request Body | Response                                                                                                                                |
|--------|----------------------------------------------------------------------------------------------------|------|----------------------|---------------------------------------------------------------|--------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities`               | JWT  | ProjectReaderPolicy  | List activity log entries for a task                         | —            | `200 OK` + [`TaskActivityReadDto`](04_DTOs.md#taskactivityreaddto) list or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`   |
| GET    | `/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities/{activityId}`  | JWT  | ProjectReaderPolicy  | Get a specific task activity entry                           | —            | `200 OK` + `TaskActivityReadDto` or `401 Unauthorized` or `403 Forbidden` or `404 Not Found`                                          |


## Me & User-centric views

These endpoints provide views scoped to the **current authenticated user** or to specific users across projects.

### `/auth/me`

Covered above in [Authentication](#authentication).

### `/members/me/count`

| Method | Endpoint             | Auth | Policy | Description                                                   | Request Body | Response                                                                                                             |
|--------|----------------------|------|--------|---------------------------------------------------------------|--------------|----------------------------------------------------------------------------------------------------------------------|
| GET    | `/members/me/count`  | JWT  | —      | Count active projects where the authenticated user is a member | —            | `200 OK` + [`ProjectMemberCountReadDto`](04_DTOs.md#projectmembercountreaddto) or `401 Unauthorized`                |

### `/members/{userId}/count`

| Method | Endpoint                   | Auth | Policy            | Description                                          | Request Body | Response                                                                                                             |
|--------|----------------------------|------|-------------------|------------------------------------------------------|--------------|----------------------------------------------------------------------------------------------------------------------|
| GET    | `/members/{userId}/count`  | JWT  | SystemAdminPolicy | Count active projects for a specified user (admin)  | —            | `200 OK` + `ProjectMemberCountReadDto` or `401 Unauthorized` or `403 Forbidden`                                     |

### `/assignments/me`

| Method | Endpoint             | Auth | Policy | Description                                              | Request Body | Response                                                                                                  |
|--------|----------------------|------|--------|----------------------------------------------------------|--------------|-----------------------------------------------------------------------------------------------------------|
| GET    | `/assignments/me`   | JWT  | —      | List task assignments for the authenticated user across projects | —    | `200 OK` + `TaskAssignmentReadDto` list or `401 Unauthorized`                                           |

### `/assignments/users/{userId}`

| Method | Endpoint                         | Auth | Policy            | Description                                      | Request Body | Response                                                                                                  |
|--------|----------------------------------|------|-------------------|--------------------------------------------------|--------------|-----------------------------------------------------------------------------------------------------------|
| GET    | `/assignments/users/{userId}`   | JWT  | SystemAdminPolicy | List task assignments for a specific user        | —            | `200 OK` + `TaskAssignmentReadDto` list or `401 Unauthorized` or `403 Forbidden`                         |

### `/notes/me`

| Method | Endpoint        | Auth | Policy | Description                                          | Request Body | Response                                                                                         |
|--------|-----------------|------|--------|------------------------------------------------------|--------------|--------------------------------------------------------------------------------------------------|
| GET    | `/notes/me`     | JWT  | —      | List notes authored by the authenticated user        | —            | `200 OK` + `TaskNoteReadDto` list or `401 Unauthorized`                                         |

### `/notes/users/{userId}`

| Method | Endpoint                    | Auth | Policy            | Description                                      | Request Body | Response                                                                                         |
|--------|-----------------------------|------|-------------------|--------------------------------------------------|--------------|--------------------------------------------------------------------------------------------------|
| GET    | `/notes/users/{userId}`    | JWT  | SystemAdminPolicy | List notes authored by the specified user       | —            | `200 OK` + `TaskNoteReadDto` list or `401 Unauthorized` or `403 Forbidden`                      |

### `/activities/me`

| Method | Endpoint           | Auth | Policy | Description                                              | Request Body | Response                                                                                         |
|--------|--------------------|------|--------|----------------------------------------------------------|--------------|--------------------------------------------------------------------------------------------------|
| GET    | `/activities/me`   | JWT  | —      | List task activities performed by the authenticated user | —            | `200 OK` + `TaskActivityReadDto` list or `401 Unauthorized`                                     |

### `/activities/users/{userId}`

| Method | Endpoint                         | Auth | Policy            | Description                                             | Request Body | Response                                                                                         |
|--------|----------------------------------|------|-------------------|---------------------------------------------------------|--------------|--------------------------------------------------------------------------------------------------|
| GET    | `/activities/users/{userId}`    | JWT  | SystemAdminPolicy | List task activities performed by a specific user      | —            | `200 OK` + `TaskActivityReadDto` list or `401 Unauthorized` or `403 Forbidden`                  |