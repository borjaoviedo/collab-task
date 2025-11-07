# Configuración de EF Core

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./06_EFCore_Configuration.md)


## Tabla de Contenidos
- [Visión general](#visión-general)
- [Convenciones](#convenciones)
- [Configuraciones de entidades](#configuraciones-de-entidades)
  - [UserConfiguration](#userconfiguration)
  - [ProjectConfiguration](#projectconfiguration)
  - [ProjectMemberConfiguration](#projectmemberconfiguration)
  - [LaneConfiguration](#laneconfiguration)
  - [ColumnConfiguration](#columnconfiguration)
  - [TaskItemConfiguration](#taskitemconfiguration)
  - [TaskNoteConfiguration](#tasknoteconfiguration)
  - [TaskAssignmentConfiguration](#taskassignmentconfiguration)
  - [TaskActivityConfiguration](#taskactivityconfiguration)
- [Conversiones de valores](#conversiones-de-valores)
- [Restricciones e índices](#restricciones-e-índices)
- [Tokens de concurrencia](#tokens-de-concurrencia)
- [Resumen de relaciones](#resumen-de-relaciones)

---------------------------------------------------------------------------------

Este documento define las **configuraciones de Entity Framework Core** utilizadas en la **capa de Infraestructura** del backend de CollabTask.  
Complementa el [modelo de dominio](01_Domain_Model.es.md) describiendo cómo se mapean las entidades y los objetos de valor al esquema relacional de la base de datos.

> **Notas**
> - Cada entidad tiene su propia clase de configuración que implementa `IEntityTypeConfiguration<T>`.
> - Las configuraciones se encuentran en el *namespace* `Infrastructure.Data.Configurations`.
> - Siempre que es posible se prefieren las convenciones a la configuración explícita.
> - Todos los mapeos están alineados con las restricciones e invariantes del modelo de dominio.


## Visión general

Las configuraciones de EF Core definen cómo se persisten las entidades de dominio y los objetos de valor, incluyendo:

- Nombres de tablas y convenciones de esquema.  
- Tipos de columna de propiedades y restricciones.  
- Relaciones (1:N, 1:1, N:N mediante entidades de unión).  
- Tokens de concurrencia (`RowVersion`).  
- Conversiones de objetos de valor y normalización.


## Convenciones

- Los nombres de tabla usan formato PascalCase en plural (`Users`, `Projects`, `Tasks`, `Notes`, etc.).
- Claves primarias:
  - La mayoría de entidades usan una clave primaria de una sola columna `Id` (`Guid`).
  - Las entidades de unión usan claves compuestas:
    - `ProjectMember`: `{ ProjectId, UserId }`
    - `TaskAssignment`: `{ TaskId, UserId }`
- Los nombres de claves externas siguen la convención `<Parent>Id` (por ejemplo `OwnerId`, `ProjectId`, `TaskId`).
- Las propiedades `RowVersion` (cuando están configuradas) se mapean como columnas SQL `rowversion` y se usan para concurrencia optimista.
- Las propiedades *enum* (`UserRole`, `ProjectRole`, `TaskRole`, `TaskActivityType`) se almacenan como cadenas usando `HasConversion<string>()`:
  - `UserRole`, `ProjectRole`, `TaskRole` → `nvarchar(20)`
  - `TaskActivityType` → `nvarchar(40)`
- Todos los timestamps (`CreatedAt`, `UpdatedAt`, `JoinedAt`, `DueDate`, etc.) se almacenan como `datetimeoffset` (UTC).
- No se implementan *soft deletes* como un patrón global de EF; los marcadores históricos (por ejemplo `RemovedAt`) se modelan explícitamente donde es necesario.


## Configuraciones de entidades

### UserConfiguration

- Tabla: `Users`
- Clave:
  - Clave primaria: `Id` (`Guid`, `ValueGeneratedNever`)
- Relaciones:
  - 1 `User` → N `ProjectMember` vía `User.ProjectMemberships` / `ProjectMember.UserId` (`OnDelete: Cascade`)
- Objetos de valor:
  - `Email`: `ValueConverter<Email, string>` → `nvarchar(256)`
  - `UserName`: `ValueConverter<UserName, string>` → `nvarchar(100)`
- Propiedades:
  - `PasswordHash`: `varbinary(32)`
  - `PasswordSalt`: `varbinary(16)`
  - `Role`: `nvarchar(20)` *enum* como *string*
  - `RowVersion`: `rowversion`
- Índices:
  - Único en `Email`
  - Único en `Name`


### ProjectConfiguration

- Tabla: `Projects`
- Clave: `Id` (`Guid`, `ValueGeneratedNever`)
- Propiedades:
  - `OwnerId` FK requerida a `User`
  - `Name`: `ProjectName` → `nvarchar(100)`
  - `Slug`: `ProjectSlug` → `nvarchar(100)`
  - `RowVersion`: `rowversion`
- Relaciones:
  - 1 `User` → N `Project`
  - 1 `Project` → N `ProjectMember`
- Índices:
  - Único en `{ OwnerId, Slug }`
  - No único en `OwnerId`


### ProjectMemberConfiguration

- Tabla: `ProjectMembers`
- Clave: `{ ProjectId, UserId }`
- Propiedades:
  - `Role`: `nvarchar(20)` *enum* como *string*
  - `JoinedAt`: `datetimeoffset`
  - `RemovedAt`: opcional
  - `RowVersion`: `rowversion`
- Relaciones:
  - 1 `Project` → N `ProjectMember`
  - 1 `User` → N `ProjectMember`
- Índices:
  - En `UserId`
  - En `{ ProjectId, Role }`


### LaneConfiguration

- Tabla: `Lanes`
- Clave: `Id`
- Propiedades:
  - `Name`: `LaneName` → `nvarchar(100)`
  - `Order`: `int`
  - `RowVersion`: `rowversion`
- Relaciones:
  - 1 `Project` → N `Lane`
- Índices:
  - Único en `{ ProjectId, Name }`
  - Único en `{ ProjectId, Order }`


### ColumnConfiguration

- Tabla: `Columns`
- Clave: `Id`
- Propiedades:
  - `Name`: `ColumnName` → `nvarchar(100)`
  - `Order`: `int`
  - `RowVersion`: `rowversion`
- Relaciones:
  - 1 `Lane` → N `Column`
- Índices:
  - Único en `{ LaneId, Name }`
  - Único en `{ LaneId, Order }`


### TaskItemConfiguration

- Tabla: `Tasks`
- Clave: `Id`
- Propiedades:
  - `Title`: `TaskTitle` → `nvarchar(100)`
  - `Description`: `TaskDescription` → `nvarchar(2000)`
  - `SortKey`: `decimal(18,6)`
  - `DueDate`: opcional
  - `RowVersion`: `rowversion`
- Relaciones:
  - 1 `Column` → N `TaskItem`
- Índices:
  - En `{ ColumnId, SortKey }`
  - En `ProjectId`
  - En `LaneId`


### TaskNoteConfiguration

- Tabla: `Notes`
- Clave: `Id`
- Propiedades:
  - `Content`: `NoteContent` → `nvarchar(500)`
  - `RowVersion`: `rowversion`
- Relaciones:
  - 1 `TaskItem` → N `TaskNote`
  - 1 `User` → N `TaskNote`
- Índices:
  - En `{ TaskId, CreatedAt }`
  - En `UserId`


### TaskAssignmentConfiguration

- Tabla: `Assignments`
- Clave: `{ TaskId, UserId }`
- Propiedades:
  - `Role`: `nvarchar(20)`
  - `RowVersion`: `rowversion`
- Relaciones:
  - 1 `TaskItem` → N `TaskAssignment`
  - 1 `User` → N `TaskAssignment`


### TaskActivityConfiguration

- Tabla: `TaskActivities`
- Clave: `Id`
- Propiedades:
  - `Type`: `nvarchar(40)`
  - `Payload`: `nvarchar(max)`
  - `CreatedAt`: `datetimeoffset`
- Relaciones:
  - 1 `TaskItem` → N `TaskActivity`
  - 1 `User` → N `TaskActivity`


## Conversiones de valores

| Objeto de valor      | Estrategia de conversión                 | Notas                        |
|----------------------|------------------------------------------|------------------------------|
| `Email`              | `ValueConverter<Email, string>`         | *String* normalizado a minúsculas |
| `UserName`           | `ValueConverter<UserName, string>`      | *String* normalizado           |
| `ProjectName`        | `ValueConverter<ProjectName, string>`   | *String* normalizado           |
| `ProjectSlug`        | `ValueConverter<ProjectSlug, string>`   | *Slug* único en minúsculas   |
| `LaneName`           | `ValueConverter<LaneName, string>`      | *String* normalizado           |
| `ColumnName`         | `ValueConverter<ColumnName, string>`    | *String* normalizado           |
| `TaskTitle`          | `ValueConverter<TaskTitle, string>`     | *String* normalizado           |
| `TaskDescription`    | `ValueConverter<TaskDescription, string>` | *String* normalizado         |
| `NoteContent`        | `ValueConverter<NoteContent, string>`   | *String* normalizado           |
| `ActivityPayload`    | `ValueConverter<ActivityPayload, string>` | Texto con formato tipo JSON |


## Restricciones e índices

- PK de una sola columna para agregados, compuestas para entidades de unión.
- Todas las relaciones usan FKs explícitas y un comportamiento de eliminación adecuado.
- Columnas *enum* almacenadas como *strings*, con restricciones `CHECK` garantizadas de forma implícita.
- Texto normalizado (minúsculas, recortado).
- Las búsquedas indexadas soportan unicidad y filtrado:
  - User: `Email`, `Name`
  - Project: `{ OwnerId, Slug }`
  - Jerarquía de panel: `{ ProjectId, Name }`, `{ LaneId, Order }`
  - Orden de tareas: `{ ColumnId, SortKey }`
  - Membresías y asignaciones: indexadas por usuario y proyecto.
  - Notes y activities: `{ TaskId, CreatedAt }`.


## Tokens de concurrencia

Entidades que usan `RowVersion` para concurrencia optimista:

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


## Resumen de relaciones

| Relación              | Multiplicidad | Notas                                                |
|-----------------------|---------------|------------------------------------------------------|
| User → Project        | 1:N           | FK `Projects.OwnerId` a `Users.Id`                  |
| User → ProjectMember  | 1:N           | Eliminación restringida                              |
| Project → ProjectMember | 1:N         | Eliminación en cascada                               |
| Project → Lane        | 1:N           | Eliminación en cascada                               |
| Lane → Column         | 1:N           | Eliminación en cascada                               |
| Column → TaskItem     | 1:N           | Eliminación en cascada                               |
| Project → TaskItem    | 1:N           | FK indexada                                          |
| Lane → TaskItem       | 1:N           | FK indexada                                          |
| TaskItem → TaskNote   | 1:N           | Eliminación en cascada                               |
| User → TaskNote       | 1:N           | Eliminación restringida                              |
| TaskItem → TaskAssignment | 1:N       | Eliminación en cascada                               |
| User → TaskAssignment | 1:N           | Eliminación restringida                              |
| TaskItem → TaskActivity | 1:N         | Eliminación en cascada                               |
| User → TaskActivity   | 1:N           | Eliminación restringida                              |
