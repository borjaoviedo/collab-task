# Authorization Policies

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./02_Authorization_Policies.es.md)


## Table of Contents
- [ProjectReaderPolicy](#projectreaderpolicy)
- [ProjectMemberPolicy](#projectmemberpolicy)
- [ProjectAdminPolicy](#projectadminpolicy)
- [ProjectOwnerPolicy](#projectownerpolicy)
- [SystemAdminPolicy](#systemadminpolicy)

---------------------------------------------------------------------------------

This document defines the **authorization policies** used across the **CollabTask** backend.  
Each policy specifies which project roles can access or modify resources, ensuring consistent and secure access control within the API.

> **Notes**
> - Policies are evaluated after authentication and project role resolution.  
> - Each policy is applied declaratively through endpoint attributes in the API layer.  
> - Policies are hierarchical: higher roles (e.g., `Owner`) implicitly satisfy lower-level requirements (e.g., `Admin`, `Member`, `Reader`).

| Policy Name             | Applies To                  | Permissions / Allowed Actions                                                                                 | Example Endpoints                                                    |
|-------------------------|-----------------------------|---------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------|
| **ProjectReaderPolicy** | `Reader` role and above     | Can view project details, lanes, columns, tasks, and notes. No modification permissions                       | `GET /projects/{projectId}/lanes`                                    |
| **ProjectMemberPolicy** | `Member` role and above     | Can create and edit tasks or notes within the project. Cannot modify project configuration or memberships     | `POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks` |
| **ProjectAdminPolicy**  | `Admin` role and above      | Can manage columns, lanes, and task assignments. Can edit project metadata. No deletion of entire projects    | `PATCH /projects/{projectId}/columns/{columnId}`                     |
| **ProjectOwnerPolicy**  | `Owner` role                | Full control over the project and all contained resources, including membership and deletion operations       | `DELETE /projects/{projectId}`                                       |
| **SystemAdminPolicy**   | `SystemAdmin` platform role | Global administrative access across all projects and endpoints. Can manage users, roles, and any project data | `GET /admin/users`                                                   |


## Additional Details

- **ProjectReaderPolicy**  
  Intended for users with read-only access to a project.  
  Grants visibility over all lanes, columns, tasks, notes, and activities without allowing modifications.

- **ProjectMemberPolicy**  
  Intended for regular project collaborators.  
  Allows creation, editing, and commenting on tasks within assigned projects but prevents structural or administrative changes.

- **ProjectAdminPolicy**  
  Intended for project administrators.  
  Extends `Member` privileges with permissions to modify project structure (lanes, columns) and manage task assignments or metadata.  
  Does not grant ownership-level permissions such as project deletion or member role escalation.

- **ProjectOwnerPolicy**  
  Dynamically assigned to the project owner.  
  Grants unrestricted access to all project operations, including member management, role updates, and project deletion.  
  The highest project-scoped authorization level.

- **SystemAdminPolicy**  
  Reserved for platform-wide administrators.  
  Provides unrestricted access to all API routes, user management operations, and system-wide maintenance endpoints.
