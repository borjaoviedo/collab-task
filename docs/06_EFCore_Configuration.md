# EF Core Configuration

> 🇬🇧 This file is in English.  
> 🇪🇸 [Versión en español disponible aquí](./06_EFCore_Configuration.es.md)


## Table of Contents
- [Overview](#overview)
- [Conventions](#conventions)
- [Entity Configurations](#entity-configurations)
  - [UserConfiguration](#userconfiguration)
  - [ProjectConfiguration](#projectconfiguration)
  - [ProjectMemberConfiguration](#projectmemberconfiguration)
  - [LaneConfiguration](#laneconfiguration)
  - [ColumnConfiguration](#columnconfiguration)
  - [TaskItemConfiguration](#taskitemconfiguration)
  - [TaskNoteConfiguration](#tasknoteconfiguration)
  - [TaskAssignmentConfiguration](#taskassignmentconfiguration)
  - [TaskActivityConfiguration](#taskactivityconfiguration)
- [Value Conversions](#value-conversions)
- [Constraints and Indexes](#constraints-and-indexes)
- [Concurrency Tokens](#concurrency-tokens)
- [Relationships Summary](#relationships-summary)

---------------------------------------------------------------------------------

This document defines the **Entity Framework Core configurations** used in the **Infrastructure layer** of the CollabTask backend.  
It complements the [Domain Model](01_Domain_Model.md) by describing how entities and value objects are mapped to the relational database schema.

> **Notes**
> - Each entity has its own configuration class implementing `IEntityTypeConfiguration<T>`.
> - Configurations are located in the `Infrastructure.Data.Configurations` namespace.
> - Conventions are preferred over explicit configuration where possible.
> - All mappings are aligned with the domain model constraints and invariants.


## Overview

EF Core configurations define how domain entities and value objects are persisted, including:

- Table names and schema conventions.  
- Property column types and constraints.  
- Relationships (1:N, 1:1, N:N via join entities).  
- Concurrency tokens (`RowVersion`).  
- Value object conversions and normalization.


## Conventions

- Table names use pluralized PascalCase format (`Users`, `Projects`, `Tasks`, `Notes`, etc.).
- Primary keys:
  - Most entities use a single-column `Id` (`Guid`) primary key.
  - Join entities use composite keys:
    - `ProjectMember`: `{ ProjectId, UserId }`
    - `TaskAssignment`: `{ TaskId, UserId }`
- Foreign key names follow `<Parent>Id` convention (e.g. `OwnerId`, `ProjectId`, `TaskId`).
- `RowVersion` properties (where configured) are mapped as SQL `rowversion` columns and used for optimistic concurrency.
- Enum properties (`UserRole`, `ProjectRole`, `TaskRole`, `TaskActivityType`) are stored as strings using `HasConversion<string>()`:
  - `UserRole`, `ProjectRole`, `TaskRole` → `nvarchar(20)`
  - `TaskActivityType` → `nvarchar(40)`
- All timestamps (`CreatedAt`, `UpdatedAt`, `JoinedAt`, `DueDate`, etc.) are stored as `datetimeoffset` (UTC).
- Soft deletes are not implemented as a global EF pattern; historical markers (e.g. `RemovedAt`) are modeled explicitly where needed.


## Entity Configurations

### UserConfiguration

- Table: `Users`
- Key:
  - Primary key: `Id` (`Guid`, `ValueGeneratedNever`)
- Relationships:
  - 1 `User` → N `ProjectMember` via `User.ProjectMemberships` / `ProjectMember.UserId` (`OnDelete: Cascade`)
- Value objects:
  - `Email`: `ValueConverter<Email, string>` → `nvarchar(256)`
  - `UserName`: `ValueConverter<UserName, string>` → `nvarchar(100)`
- Properties:
  - `PasswordHash`: `varbinary(32)`
  - `PasswordSalt`: `varbinary(16)`
  - `Role`: `nvarchar(20)` enum as string
  - `RowVersion`: `rowversion`
- Indexes:
  - Unique on `Email`
  - Unique on `Name`


### ProjectConfiguration

- Table: `Projects`
- Key: `Id` (`Guid`, `ValueGeneratedNever`)
- Properties:
  - `OwnerId` required FK to `User`
  - `Name`: `ProjectName` → `nvarchar(100)`
  - `Slug`: `ProjectSlug` → `nvarchar(100)`
  - `RowVersion`: `rowversion`
- Relationships:
  - 1 `User` → N `Project`
  - 1 `Project` → N `ProjectMember`
- Indexes:
  - Unique on `{ OwnerId, Slug }`
  - Non-unique on `OwnerId`


### ProjectMemberConfiguration

- Table: `ProjectMembers`
- Key: `{ ProjectId, UserId }`
- Properties:
  - `Role`: `nvarchar(20)` enum as string
  - `JoinedAt`: `datetimeoffset`
  - `RemovedAt`: optional
  - `RowVersion`: `rowversion`
- Relationships:
  - 1 `Project` → N `ProjectMember`
  - 1 `User` → N `ProjectMember`
- Indexes:
  - On `UserId`
  - On `{ ProjectId, Role }`


### LaneConfiguration

- Table: `Lanes`
- Key: `Id`
- Properties:
  - `Name`: `LaneName` → `nvarchar(100)`
  - `Order`: `int`
  - `RowVersion`: `rowversion`
- Relationships:
  - 1 `Project` → N `Lane`
- Indexes:
  - Unique on `{ ProjectId, Name }`
  - Unique on `{ ProjectId, Order }`


### ColumnConfiguration

- Table: `Columns`
- Key: `Id`
- Properties:
  - `Name`: `ColumnName` → `nvarchar(100)`
  - `Order`: `int`
  - `RowVersion`: `rowversion`
- Relationships:
  - 1 `Lane` → N `Column`
- Indexes:
  - Unique on `{ LaneId, Name }`
  - Unique on `{ LaneId, Order }`


### TaskItemConfiguration

- Table: `Tasks`
- Key: `Id`
- Properties:
  - `Title`: `TaskTitle` → `nvarchar(100)`
  - `Description`: `TaskDescription` → `nvarchar(2000)`
  - `SortKey`: `decimal(18,6)`
  - `DueDate`: optional
  - `RowVersion`: `rowversion`
- Relationships:
  - 1 `Column` → N `TaskItem`
- Indexes:
  - On `{ ColumnId, SortKey }`
  - On `ProjectId`
  - On `LaneId`


### TaskNoteConfiguration

- Table: `Notes`
- Key: `Id`
- Properties:
  - `Content`: `NoteContent` → `nvarchar(500)`
  - `RowVersion`: `rowversion`
- Relationships:
  - 1 `TaskItem` → N `TaskNote`
  - 1 `User` → N `TaskNote`
- Indexes:
  - On `{ TaskId, CreatedAt }`
  - On `UserId`


### TaskAssignmentConfiguration

- Table: `Assignments`
- Key: `{ TaskId, UserId }`
- Properties:
  - `Role`: `nvarchar(20)`
  - `RowVersion`: `rowversion`
- Relationships:
  - 1 `TaskItem` → N `TaskAssignment`
  - 1 `User` → N `TaskAssignment`


### TaskActivityConfiguration

- Table: `TaskActivities`
- Key: `Id`
- Properties:
  - `Type`: `nvarchar(40)`
  - `Payload`: `nvarchar(max)`
  - `CreatedAt`: `datetimeoffset`
- Relationships:
  - 1 `TaskItem` → N `TaskActivity`
  - 1 `User` → N `TaskActivity`


## Value Conversions

| Value Object | Conversion Strategy | Notes |
|---------------|---------------------|--------|
| `Email` | `ValueConverter<Email, string>` | Normalized lowercase string |
| `UserName` | `ValueConverter<UserName, string>` | Normalized string |
| `ProjectName` | `ValueConverter<ProjectName, string>` | Normalized string |
| `ProjectSlug` | `ValueConverter<ProjectSlug, string>` | Lowercase unique slug |
| `LaneName` | `ValueConverter<LaneName, string>` | Normalized string |
| `ColumnName` | `ValueConverter<ColumnName, string>` | Normalized string |
| `TaskTitle` | `ValueConverter<TaskTitle, string>` | Normalized string |
| `TaskDescription` | `ValueConverter<TaskDescription, string>` | Normalized string |
| `NoteContent` | `ValueConverter<NoteContent, string>` | Normalized string |
| `ActivityPayload` | `ValueConverter<ActivityPayload, string>` | JSON-like text |


## Constraints and Indexes

- Single-column PK for aggregates, composite for join entities.
- All relationships use explicit FKs and proper delete behavior.
- Enum columns stored as strings with `CHECK` constraints implicitly ensured.
- Text normalized (lowercase, trimmed).
- Indexed lookups support uniqueness and filtering:
  - User: `Email`, `Name`
  - Project: `{ OwnerId, Slug }`
  - Board hierarchy: `{ ProjectId, Name }`, `{ LaneId, Order }`
  - Task order: `{ ColumnId, SortKey }`
  - Membership and assignments: indexed by user and project.
  - Notes and activities: `{ TaskId, CreatedAt }`.


## Concurrency Tokens

Entities using `RowVersion` for optimistic concurrency:

- `User`
- `Project`
- `Lane`
- `Column`
- `ProjectMember`
- `TaskItem`
- `TaskNote`
- `TaskAssignment`

```csharp
builder.Property(e => e.RowVersion).IsRowVersion();
```


## Relationships Summary

| Relationship | Multiplicity | Notes |
|---------------|--------------|--------|
| User → Project | 1:N | `Projects.OwnerId` FK to `Users.Id` |
| User → ProjectMember | 1:N | Restrict delete |
| Project → ProjectMember | 1:N | Cascade delete |
| Project → Lane | 1:N | Cascade delete |
| Lane → Column | 1:N | Cascade delete |
| Column → TaskItem | 1:N | Cascade delete |
| Project → TaskItem | 1:N | Indexed FK |
| Lane → TaskItem | 1:N | Indexed FK |
| TaskItem → TaskNote | 1:N | Cascade delete |
| User → TaskNote | 1:N | Restrict delete |
| TaskItem → TaskAssignment | 1:N | Cascade delete |
| User → TaskAssignment | 1:N | Restrict delete |
| TaskItem → TaskActivity | 1:N | Cascade delete |
| User → TaskActivity | 1:N | Restrict delete |
