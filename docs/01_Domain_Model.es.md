# Modelo de Dominio  

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./01_Domain_Model.md)


## Tabla de Contenidos
- [Control de Concurrencia e Identidad](#control-de-concurrencia-e-identidad)
- [Entidades](#entidades)
  - [User](#user)
  - [Project](#project)
  - [ProjectMember](#projectmember)
  - [Lane](#lane)
  - [Column](#column)
  - [TaskItem](#taskitem)
  - [TaskNote](#tasknote)
  - [TaskAssignment](#taskassignment)
  - [TaskActivity](#taskactivity)
- [Objetos de Valor](#objetos-de-valor)
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
- [Enumerados](#enumerados)
  - [UserRole](#userrole)
  - [ProjectRole](#projectrole)
  - [TaskRole](#taskrole)
  - [TaskActivityType](#taskactivitytype)
- [Enumerados Técnicos](#enumerados-técnicos)
  - [DomainMutation](#domainmutation)
  - [MutationKind](#mutationkind)
  - [PrecheckStatus](#precheckstatus)
- [Resumen de Relaciones](#resumen-de-relaciones)

---------------------------------------------------------------------------------

Este documento define el **modelo de dominio canónico** del backend de **CollabTask**.  
Describe las entidades principales, los objetos de valor y las enumeraciones que representan los conceptos de negocio dentro del sistema.

> **Notas**
> - Todas las marcas de tiempo están en **UTC** y usan `DateTimeOffset`.  
> - Las relaciones se representan mediante propiedades de navegación (`ICollection<T>` para relaciones 1:N).


## Control de Concurrencia e Identidad
La mayoría de las entidades incluyen una propiedad `RowVersion` (token binario de concurrencia) utilizada para el control de concurrencia optimista.  
En la capa de API, este valor se codifica como un `byte[]` y se expone a través del encabezado HTTP **ETag**.  
Los clientes deben incluir este valor en el encabezado **If-Match** al realizar operaciones de actualización o eliminación.


## Entidades

### User

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| Id | Guid | Clave primaria |
| Name | [UserName](#username) | Objeto de valor que representa el nombre del usuario |
| Email | [Email](#email) | Objeto de valor que representa el correo electrónico normalizado del usuario |
| PasswordHash | byte[] | Hash de la contraseña |
| PasswordSalt | byte[] | Sal utilizada en el hash de la contraseña |
| Role | [UserRole](#userrole) | Rol global del usuario en el sistema |
| ProjectMemberships | [ICollection<ProjectMember>](#projectmember) | Propiedad de navegación hacia las membresías de proyectos |
| CreatedAt | DateTimeOffset | Establecido automáticamente al crear el usuario |
| UpdatedAt | DateTimeOffset | Establecido automáticamente al actualizar el usuario |
| RowVersion | byte[] | Token de concurrencia optimista |


### Project

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| Id | Guid | Clave primaria |
| OwnerId | Guid | Clave foránea al usuario propietario del proyecto |
| Name | [ProjectName](#projectname) | Objeto de valor que representa el nombre del proyecto |
| Slug | [ProjectSlug](#projectslug) | Objeto de valor que representa el *slug* del proyecto |
| Members | [ICollection<ProjectMember>](#projectmember) | Propiedad de navegación hacia las membresías del proyecto |
| CreatedAt | DateTimeOffset | Establecido automáticamente al crear el proyecto |
| UpdatedAt | DateTimeOffset | Establecido automáticamente al actualizar el proyecto |
| RowVersion | byte[] | Token de concurrencia optimista |


### ProjectMember

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| ProjectId | Guid | Clave foránea al proyecto |
| UserId | Guid | Clave foránea al usuario |
| Role | [ProjectRole](#projectrole) | Rol del usuario dentro del proyecto |
| Project | [Project](#project) | Propiedad de navegación hacia el proyecto |
| User | [User](#user) | Propiedad de navegación hacia el usuario |
| JoinedAt | DateTimeOffset | Fecha en la que el usuario se une al proyecto |
| RemovedAt | DateTimeOffset? | Fecha en la que el usuario es eliminado del proyecto |
| RowVersion | byte[] | Token de concurrencia optimista |


### Lane

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| Id | Guid | Clave primaria |
| ProjectId | Guid | Clave foránea al proyecto |
| Name | [LaneName](#lanename) | Objeto de valor que representa el nombre de la línea |
| Order | int | Orden de visualización dentro del proyecto (≥ 0 y único por proyecto) |
| RowVersion | byte[] | Token de concurrencia optimista |


### Column

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| Id | Guid | Clave primaria |
| LaneId | Guid | Clave foránea a la línea |
| ProjectId | Guid | Clave foránea al proyecto |
| Name | [ColumnName](#columnname) | Objeto de valor que representa el nombre de la columna |
| Order | int | Orden de visualización dentro de la línea (≥ 0 y único por línea) |
| RowVersion | byte[] | Token de concurrencia optimista |


### TaskItem

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| Id | Guid | Clave primaria |
| ColumnId | Guid | Clave foránea a la columna |
| LaneId | Guid | Clave foránea a la línea |
| ProjectId | Guid | Clave foránea al proyecto |
| Title | [TaskTitle](#tasktitle) | Objeto de valor que representa el título de la tarea |
| Description | [TaskDescription](#taskdescription) | Objeto de valor que representa la descripción de la tarea |
| SortKey | decimal | Orden de visualización dentro de la columna (≥ 0 y único por columna) |
| DueDate | DateTimeOffset? | Fecha límite opcional |
| CreatedAt | DateTimeOffset | Establecido automáticamente al crear la tarea |
| UpdatedAt | DateTimeOffset | Establecido automáticamente al actualizar la tarea |
| RowVersion | byte[] | Token de concurrencia optimista |


### TaskNote

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| Id | Guid | Clave primaria |
| TaskId | Guid | Clave foránea a la tarea |
| UserId | Guid | Clave foránea al usuario (autor) |
| Content | [NoteContent](#notecontent) | Objeto de valor que representa el contenido de la nota |
| CreatedAt | DateTimeOffset | Establecido automáticamente al crear la nota |
| UpdatedAt | DateTimeOffset | Establecido automáticamente al actualizar la nota |
| RowVersion | byte[] | Token de concurrencia optimista |


### TaskAssignment

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| TaskId | Guid | Clave foránea a la tarea |
| UserId | Guid | Clave foránea al usuario asignado |
| Role | [TaskRole](#taskrole) | Rol del usuario en la tarea |
| RowVersion | byte[] | Token de concurrencia optimista |


### TaskActivity

| Propiedad | Tipo | Descripción |
|------------|------|--------------|
| Id | Guid | Clave primaria |
| TaskId | Guid | Clave foránea a la tarea |
| ActorId | Guid | Clave foránea al usuario que realiza la acción |
| Type | [TaskActivityType](#taskactivitytype) | Tipo de actividad registrada |
| Payload | [ActivityPayload](#activitypayload) | Objeto de valor con los datos del evento en formato JSON |
| CreatedAt | DateTimeOffset | Establecido automáticamente al crear la actividad |
| RowVersion | byte[] | Token de concurrencia optimista |


## Objetos de Valor

| Nombre | Tipo | Descripción |
|---------|------|--------------|
| **UserName** | string | Nombre normalizado. Longitud 2–100 |
| **Email** | string | Dirección de correo normalizada. Longitud 2–256 |
| **ProjectName** | string | Nombre normalizado del proyecto. Máx. 100 caracteres |
| **ProjectSlug** | string | *Slug* único y en minúsculas. Máx. 100 caracteres |
| **LaneName** | string | Nombre normalizado de la línea. Longitud 2–100 |
| **ColumnName** | string | Nombre normalizado de la columna. Longitud 2–100 |
| **TaskTitle** | string | Título normalizado de la tarea. Longitud 2–100 |
| **TaskDescription** | string | Descripción normalizada. Longitud 2–2000 |
| **NoteContent** | string | Contenido normalizado. Longitud 2–500 |
| **ActivityPayload** | string | Cuerpo JSON válido en UTF-8 y no vacío |


## Enumerados

### UserRole  
Define el rol global del usuario en el sistema:  
`{ User, Admin }`

### ProjectRole  
Define el rol de un usuario dentro de un proyecto:  
`{ Reader, Member, Admin, Owner }`

### TaskRole  
Define el rol de un usuario dentro de una tarea específica:  
`{ CoOwner, Owner }`

### TaskActivityType  
Define el tipo de actividad registrada en una tarea:  
`{ TaskCreated, TaskEdited, TaskMoved, AssignmentCreated, AssignmentRoleChanged, AssignmentRemoved, NoteAdded, NoteEdited, NoteRemoved }`


## Enumerados Técnicos

### DomainMutation  
Representa el resultado de una operación de dominio:  
`{ NoOp, NotFound, Updated, Created, Deleted, Conflict }`

### MutationKind  
Representa la intención o tipo de mutación:  
`{ Create, Update, Delete }`

### PrecheckStatus  
Representa el resultado de las comprobaciones previas antes de realizar una mutación:  
`{ NotFound, NoOp, Conflict, Ready }`


## Resumen de Relaciones
- User N ─── N Project (a través de ProjectMember)  
- Project 1 ─── N Lane  
- Lane 1 ─── N Column  
- Column 1 ─── N TaskItem  
- TaskItem 1 ─── N TaskNote  
- TaskItem 1 ─── N TaskAssignment  
- TaskItem 1 ─── N TaskActivity  