# Domain Model Overview

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./01_Domain_Model.es.md)


## Table of Contents
- [Concurrency & Identity](#concurrency--identity)
- [Entities](#entities)
  - [User](#user)
  - [Project](#project)
  - [ProjectMember](#projectmember)
  - [Column](#column)
  - [Lane](#lane)
  - [TaskItem](#taskitem)
  - [TaskNote](#tasknote)
  - [TaskAssignment](#taskassignment)
  - [TaskActivity](#taskactivity)
- [Value Objects](#value-objects)
  - [UserName](#username)
  - [Email](#email)
  - [ProjectName](#projectname)
  - [ProjectSlug](#projectslug)
  - [LaneName](#lanename)
  - [ColumnName](#columnname)
  - [TaskTitle](#tasktitle)
  - [TaskDescription](#taskdescription)
  - [NoteContent](#notecontent)
  - [ActivityPayload](#activitypayload)
- [Enums](#enums)
  - [UserRole](#userrole)
  - [ProjectRole](#projectrole)
  - [TaskRole](#taskrole)
  - [TaskActivityType](#taskactivitytype)
- [Technical Enums](#technical-enums)
  - [DomainMutation](#domainmutation)
  - [MutationKind](#mutationkind)
  - [PrecheckStatus](#precheckstatus)
- [Relationships summary](#relationships-summary)

---------------------------------------------------------------------------------

This document defines the canonical **domain model** for the **CollabTask backend**.  
It describes the core entities, value objects, and enums that represent business concepts within the system.

> **Notes**
> - All timestamps are in **UTC** and use `DateTimeOffset`.
> - Relationships are shown using navigation properties (`ICollection<T>` for 1:N).


## Concurrency & Identity

Most entities include a `RowVersion` property (binary concurrency token) used for optimistic concurrency control.  
In the API layer, this value is encoded as a `byte[]` and mapped to the HTTP `ETag` header.  
Clients must supply this value via `If-Match` when performing update or delete operations.


## Entities
### User
| Property           | Type                                           | Notes                                                         |
|--------------------|------------------------------------------------|---------------------------------------------------------------|
| Id                 | `Guid`                                         | Primary key                                                   |
| Name               | [`UserName`](#username)                        | Value object representing the user's name                     |
| Email              | [`Email`](#email)                              | Value object representing the user's normalized email address |
| PasswordHash       | `byte[]`                                       | Hashed password value                                         |
| PasswordSalt       | `byte[]`                                       | Salt used when hashing the password                           |
| Role               | [`UserRole`](#userrole)                        | User role. See `UserRole`                                     |
| ProjectMemberships | [`ICollection<ProjectMember>`](#projectmember) | Navigation property for the user project memberships          |
| CreatedAt          | `DateTimeOffset`                               | Set automatically by the server when the user is created      |
| UpdatedAt          | `DateTimeOffset`                               | Set automatically by the server when the user is updated      |
| RowVersion         | `byte[]`                                       | Optimistic concurrency token                                  |

### Project
| Property   | Type                                           | Notes                                                       |
|------------|------------------------------------------------|-------------------------------------------------------------|
| Id         | `Guid`                                         | Primary key                                                 |
| OwnerId    | `Guid`                                         | Foreign key referencing the user who owns the project       |
| Name       | [`ProjectName`](#projectname)                  | Value object representing the project name                  |
| Slug       | [`ProjectSlug`](#projectslug)                  | Value object representing the project slug                  |
| Members    | [`ICollection<ProjectMember>`](#projectmember) | Navigation property for the project memberships             |
| CreatedAt  | `DateTimeOffset`                               | Set automatically by the server when the project is created |
| UpdatedAt  | `DateTimeOffset`                               | Set automatically by the server when the project is updated |
| RowVersion | `byte[]`                                       | Optimistic concurrency token                                |

### ProjectMember
| Property   | Type                          | Notes                                                                     |
|------------|-------------------------------|---------------------------------------------------------------------------|
| ProjectId  | `Guid`                        | Foreign key referencing the parent Project                                |
| UserId     | `Guid`                        | Foreign key referencing the parent User                                   |
| Role       | [`ProjectRole`](#projectrole) | User role in the project. See `ProjectRole`                               |
| Project    | [`Project`](#project)         | Navigation property for the project                                       |
| User       | [`User`](#user)               | Navigation property for the user                                          |
| JoinedAt   | `DateTimeOffset`              | Set automatically by the server when the user joins the project           |
| RemovedAt  | `DateTimeOffset?`             | Set automatically by the server when the user is removed from the project |
| RowVersion | `byte[]`                      | Optimistic concurrency token                                              |

### Column
| Property   | Type                        | Notes                                                                                                    |
|------------|-----------------------------|----------------------------------------------------------------------------------------------------------|
| Id         | `Guid`                      | Primary key                                                                                              |
| LaneId     | `Guid`                      | Foreign key referencing the parent lane                                                                  |
| ProjectId  | `Guid`                      | Foreign key referencing the parent project                                                               |
| Name       | [`ColumnName`](#columnname) | Value object representing the column name                                                                |
| Order      | `int`                       | Defines the display order of the column within its lane. Must satisfy `Order ≥ 0` and be unique per lane |
| RowVersion | `byte[]`                    | Optimistic concurrency token                                                                             |

### Lane
| Property   | Type                    | Notes                                                                                                        |
|------------|-------------------------|--------------------------------------------------------------------------------------------------------------|
| Id         | `Guid`                  | Primary key                                                                                                  |
| ProjectId  | `Guid`                  | Foreign key referencing the parent project                                                                   |
| Name       | [`LaneName`](#lanename) | Value object representing the lane name                                                                      |
| Order      | `int`                   | Defines the display order of the lane within its project. Must satisfy `Order ≥ 0` and be unique per project |
| RowVersion | `byte[]`                | Optimistic concurrency token                                                                                 |

### TaskItem
| Property    | Type                                  | Notes                                                                                                        |
|-------------|---------------------------------------|--------------------------------------------------------------------------------------------------------------|
| Id          | `Guid`                                | Primary key                                                                                                  |
| ColumnId    | `Guid`                                | Foreign key referencing the parent column                                                                    |
| LaneId      | `Guid`                                | Foreign key referencing the parent lane                                                                      |
| ProjectId   | `Guid`                                | Foreign key referencing the parent project                                                                   |
| Title       | [`TaskTitle`](#tasktitle)             | Value object representing the task title                                                                     |
| Description | [`TaskDescription`](#taskdescription) | Value object representing the task description                                                               |
| SortKey     | `decimal`                             | Defines the display order of the task within its column. Must satisfy `SortKey ≥ 0` and be unique per column |
| DueDate     | `DateTimeOffset?`                     | Optional deadline for the task. Can be null                                                                  |
| CreatedAt   | `DateTimeOffset`                      | Set automatically by the server when the task is created                                                     |
| UpdatedAt   | `DateTimeOffset`                      | Set automatically by the server when the task is updated                                                     |
| RowVersion  | `byte[]`                              | Optimistic concurrency token                                                                                 |

### TaskNote
| Property   | Type                          | Notes                                                    |
|------------|-------------------------------|----------------------------------------------------------|
| Id         | `Guid`                        | Primary key                                              |
| TaskId     | `Guid`                        | Foreign key referencing the parent task                  |
| UserId     | `Guid`                        | Foreign key referencing the parent user (author)         |
| Content    | [`NoteContent`](#notecontent) | Value object representing the note content               |
| CreatedAt  | `DateTimeOffset`              | Set automatically by the server when the note is created |
| UpdatedAt  | `DateTimeOffset`              | Set automatically by the server when the note is updated |
| RowVersion | `byte[]`                      | Optimistic concurrency token                             |

### TaskAssignment
| Property   | Type                    | Notes                                              |
|------------|-------------------------|----------------------------------------------------|
| TaskId     | `Guid`                  | Foreign key referencing the parent task            |
| UserId     | `Guid`                  | Foreign key referencing the parent user (assigned) |
| Role       | [`TaskRole`](#taskrole) | User role in the task. See `TaskRole`              |
| RowVersion | `byte[]`                | Optimistic concurrency token                       |

### TaskActivity
| Property   | Type                                    | Notes                                                        |
|------------|-----------------------------------------|--------------------------------------------------------------|
| Id         | `Guid`                                  | Primary key                                                  |
| TaskId     | `Guid`                                  | Foreign key referencing the parent task                      |
| ActorId    | `Guid`                                  | Foreign key referencing the parent actor (user)              |
| Type       | [`TaskActivityType`](#taskactivitytype) | Activity type. See `TaskActivityType`                        |
| Payload    | [`ActivityPayload`](#activitypayload)   | Value object representing the activity payload               |
| CreatedAt  | `DateTimeOffset`                        | Set automatically by the server when the activity is created |
| RowVersion | `byte[]`                                | Optimistic concurrency token                                 |


## Value Objects
### UserName
| Property | Type     | Notes                         |
|----------|----------|-------------------------------|
| Value    | `string` | Normalized name. Length 2–100 |

### Email
| Property | Type     | Notes                                  |
|----------|----------|----------------------------------------|
| Value    | `string` | Normalized email address. Length 2–256 |

### ProjectName
| Property | Type     | Notes                                   |
|----------|----------|-----------------------------------------|
| Value    | `string` | Normalized project name. Max length 100 |

### ProjectSlug
| Property | Type     | Notes                                                                  |
|----------|----------|------------------------------------------------------------------------|
| Value    | `string` | URL-friendly slug. Lowercase, unique per project scope. Max length 100 |

### LaneName
| Property | Type     | Notes                              |
|----------|----------|------------------------------------|
| Value    | `string` | Normalized lane name. Length 2–100 |

### ColumnName
| Property | Type     | Notes                                |
|----------|----------|--------------------------------------|
| Value    | `string` | Normalized column name. Length 2–100 |

### TaskTitle
| Property | Type     | Notes                               |
|----------|----------|-------------------------------------|
| Value    | `string` | Normalized task title. Length 2–100 |

### TaskDescription
| Property | Type     | Notes                                      |
|----------|----------|--------------------------------------------|
| Value    | `string` | Normalized task description. Length 2–2000 |

### NoteContent
| Property | Type     | Notes                                 |
|----------|----------|---------------------------------------|
| Value    | `string` | Normalized note content. Length 2–500 |

### ActivityPayload
| Property | Type     | Notes                                                              |
|----------|----------|--------------------------------------------------------------------|
| Value    | `string` | JSON-formatted activity payload. Must be valid UTF-8 and non-empty |


## Enums
### UserRole
Defines the global role of a user within the system and their permission level.  
`{ User, Admin }`

### ProjectRole
Defines the role of a user within a project and their level of access.  
`{ Reader, Member, Admin, Owner }`

### TaskRole
Defines the role of a user within a specific task.  
`{ CoOwner, Owner }`

### TaskActivityType
Defines the type of activity recorded for a task.  
`{ TaskCreated, TaskEdited, TaskMoved, AssignmentCreated, AssignmentRoleChanged, AssignmentRemoved, NoteAdded, NoteEdited, NoteRemoved }`


## Technical Enums
These enums are used internally to represent mutation outcomes, prechecks, and command semantics within the domain and application layers.

### DomainMutation
Represents the result of a domain operation or mutation.  
`{ NoOp, NotFound, Updated, Created, Deleted, Conflict }`

### MutationKind
Represents the intent or type of a domain mutation.  
`{ Create, Update, Delete }`

### PrecheckStatus
Represents the outcome of precondition checks before performing a mutation.  
`{ NotFound, NoOp, Conflict, Ready }`


## Relationships summary

- User N ─── N Project (via ProjectMember) 
- Project 1 ─── N Lane  
- Lane 1 ─── N Column  
- Column 1 ─── N TaskItem  
- TaskItem 1 ─── N TaskNote  
- TaskItem 1 ─── N TaskAssignment  
- TaskItem 1 ─── N TaskActivity  