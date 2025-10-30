# Registro de Cambios

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./CHANGELOG.md)

Todos los cambios relevantes de este proyecto se documentan en este archivo.

El formato sigue las directrices de [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/),
y este proyecto se adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-31

### Añadido
- **Documentación**
  - Se añadió **documentación XML completa** en todo el backend, garantizando **cobertura total del código** y **claridad en la API**.
- **Concurrencia y Contratos**
  - Se añadió **manejo estricto** para las cabeceras `If-Match` y `ETag` en todos los endpoints.
  - Se implementaron los filtros **`RequireIfMatch()`** y **`RejectIfMatch()`** para aplicar **concurrencia optimista**.
  - Se añadieron respuestas **`428 Precondition Required`** a OpenAPI para clarificar el uso de precondiciones.
- **OpenAPI**
  - Se actualizaron los **resúmenes y descripciones** en `openapi.json` para todos los endpoints.
  - Se añadió **documentación detallada** indicando roles de miembro/admin y comportamiento de concurrencia.

### Modificado
- **Domain**
  - Se mejoraron las **validaciones** en los objetos de valor (`UserName`, `ProjectName`, etc.) aplicando reglas más estrictas mediante guard clauses.
  - Se ampliaron los **invariantes de dominio** para asegurar que exista **exactamente un propietario activo** por proyecto, validado mediante un índice filtrado único.
  - Se mejoró la consistencia de los **tokens de concurrencia** y el comportamiento de auditoría en las entidades de dominio.
  - Se añadieron resúmenes XML a todas las **entidades, objetos de valor, enumeraciones y eventos de dominio**.

- **Application**
  - Se introdujo el patrón **Unit of Work (UoW)** mediante la abstracción `IUnitOfWork` para coordinar **transacciones atómicas** y unificar los resultados de persistencia.
  - Los servicios de aplicación ahora delegan las confirmaciones de persistencia a **`IUnitOfWork.SaveAsync()`** en lugar de usar directamente **`DbContext.SaveChangesAsync()`**.
  - Se reorganizaron los espacios de nombres de **`TaskItemChange`** dentro de `Application.TaskItems.Changes`.
  - Se reemplazaron los resultados de **`DomainMutation`** por el nuevo tipo **`PrecheckStatus`** en las operaciones de repositorio.
  - Se reescribieron las interfaces de repositorios y servicios para usar **objetos de valor de dominio** (`TaskTitle`, `TaskDescription`, etc.).
  - Se mejoró la integración del **interceptor de auditoría** (`AuditingSaveChangesInterceptor`) mediante **`IDateTimeProvider`**.
  - Se estandarizó la **documentación XML** en todas las interfaces y abstracciones de servicio.

- **Infrastructure**
  - Se añadió la implementación de **`UnitOfWork`** en `Infrastructure.Data.UnitOfWork`, traduciendo los resultados de persistencia de EF Core en `DomainMutation`.
  - Se amplió la **inyección de dependencias** para registrar `IUnitOfWork` y se refactorizó **`DependencyInjection.cs`** con una separación más clara de responsabilidades (**DbContext**, interceptores, repositorios, servicios).
  - Se movió **`DbInitHostedService`** de `Infrastructure/Initialization` a `Infrastructure/Data/Initialization` para mantener consistencia estructural.
  - Se mejoró **`AppDbContext`** con configuraciones específicas por proveedor:
    - **SQL Server:** se aplicaron **CHECK constraints** e **índices únicos filtrados** (ej. regla de propietario activo).
    - **SQLite:** se añadieron convertidores y emulación de `rowversion`.
  - Se actualizaron los **interceptores de EF Core** y la lógica de auditoría para un registro de fechas más preciso.
  - Se añadieron nuevas migraciones **`Rename_TaskNote_AuthorId_To_UserId`** y **`InfraSchemaFinalization`** (limpieza final del esquema).

- **API**
  - Se normalizaron los **resúmenes y descripciones de endpoints** para cumplir las convenciones REST y de concurrencia.
  - Se actualizaron todos los endpoints de **creación/edición/eliminación** para indicar el nivel de autorización (**solo miembro**, **solo admin**) y los requisitos de `ETag`.
  - Se renombraron los campos **`operationId`** en OpenAPI para mantener consistencia:
    - `Tasks_Get` → **`Tasks_Get_ById`**
    - `TaskNotes_Get` → **`TaskNotes_Get_ById`**
    - `TaskNotes_ListMine` → **`TaskNotes_Get_Mine`**
    - `TaskNotes_ListByUser` → **`TaskNotes_Get_ByUser`**
  - Se unificaron los **contratos de respuesta de concurrencia** (`409 Conflict`, `412 PreconditionFailed`, `428 PreconditionRequired`).

- **Testing**
  - Gran **refactorización de pruebas unitarias e integradas**:
    - Se añadieron módulos **`TestHelpers.Api.*`** para operaciones reutilizables (**Auth**, **Projects**, **ProjectMembers**, etc.).
    - Se incorporó el uso de **UoW** en las pruebas para verificar persistencia transaccional entre repositorios.
    - Se reemplazó código repetido por **helpers centralizados**, mejorando legibilidad y mantenibilidad.
  - Se ajustaron las referencias de proyecto (`TestHelpers.csproj` ahora referencia `Api.csproj` para los helpers de endpoints).
  - Se ampliaron las pruebas para cubrir **validación de concurrencia** (ej. `Create_With_IfMatch_Header_Returns_400`).

- **Contracts**
  - Se reescribió y normalizó completamente **`openapi.json`** con nuevos resúmenes, operation IDs y documentación de cabeceras mejorada.

### Corregido
- Se corrigieron múltiples inconsistencias entre el uso de **ETag/If-Match** y el comportamiento de concurrencia de los repositorios.
- Se corrigió la propagación de **RowVersion** en operaciones de actualización y eliminación en todas las entidades del tablero.

### Eliminado
- Se eliminaron los archivos `.gitkeep`.

### Notas
- Esta versión completa la fase de refactorización, documentación y preparación para la publicación.
- El backend está completamente documentado, alineado con **Clean Architecture**, el patrón **Unit of Work** y los estándares de **concurrencia optimista**.
- **Etiqueta creada:** `v1.0.0`

## [0.4.0] - 2025-10-17

### Añadido
- **Backend / Realtime**
  - Integración de **SignalR** para actualizaciones del tablero en tiempo real.
  - Se añadió **BoardHub** con gestión de grupos por `project:{id}`.
  - Se introdujo el servicio **BoardNotifier** para difundir eventos a los clientes.
  - Se implementaron nuevos modelos y controladores de eventos en tiempo real:
    - `TaskItemCreated`, `TaskItemUpdated`, `TaskItemMoved`, `TaskItemDeleted`
    - `TaskAssignmentCreated`, `TaskAssignmentUpdated`, `TaskAssignmentRemoved`
    - `TaskNoteCreated`, `TaskNoteUpdated`, `TaskNoteDeleted`
  - Se añadieron manejadores dedicados: `TaskItemChangedHandler`, `TaskNoteChangedHandler`, `TaskAssignmentChangedHandler`.
  - Todos los eventos comparten un contrato unificado `{ type, projectId, payload }` para serialización consistente.
  - Endpoint `/hubs/board` expuesto mediante la integración de SignalR en la capa API.
  - Ampliado el conjunto de pruebas:
    - `BoardEventSerializationTests`, `BoardNotifierTests` y todas las pruebas de handlers (`TaskItem`, `TaskNote`, `TaskAssignment`).

### Modificado
- **Application / Write Services**
  - Refactorización de todos los servicios de escritura del tablero para integrar **publicación de eventos basada en Mediator** después de las confirmaciones de persistencia:
    - **TaskItemWriteService** → emite `TaskItemCreated`, `TaskItemUpdated`, `TaskItemMoved`, `TaskItemDeleted`.
    - **TaskNoteWriteService** → emite `TaskNoteCreated`, `TaskNoteUpdated`, `TaskNoteDeleted`.
    - **TaskAssignmentWriteService** → emite `TaskAssignmentCreated`, `TaskAssignmentUpdated`, `TaskAssignmentRemoved`.
  - Las firmas de métodos ahora incluyen `projectId` para garantizar un alcance preciso de los eventos.
- **API / Composition Root**
  - Se añadió registro `.AddSignalR()` y configuración de dependencias para los componentes de tiempo real.
  - `WebApplicationExtensions.MapApiLayer()` expone el endpoint `/hubs/board`.
- **Testing & CI**
  - Se elevó el umbral de cobertura del **60% al 75%**, mantenido en todas las suites de prueba.

### Notas
- Esta versión completa el hito del backend en tiempo real.
- Próxima fase: refactorización, documentación y optimización antes de la versión pública (`v1.0.0`).

## [0.3.0] - 2025-10-16

### Añadido
- **Backend / Kanban**
  - Entidades de dominio: `Lane`, `Column`, `TaskItem`, `TaskNote`, `TaskAssignment`, `TaskActivity`.
  - Objetos de valor: `LaneName`, `ColumnName`, `TaskTitle`, `TaskDescription`, `NoteContent`, `ActivityPayload`.
  - Enumeración `TaskActivityType` representando operaciones de creación, edición, movimiento, asignación de propietario o co-propietario y notas.
  - Configuraciones EF Core y repositorios para todas las entidades.
  - Endpoints Minimal API para CRUD completo y flujos de reordenamiento/movimiento.
  - **Registro automático de actividades (`TaskActivity`)** desde los servicios de escritura (`TaskItem`, `TaskAssignment`, `TaskNote`).
  - Tokens de concurrencia (`RowVersion`) y ordenación (`Order`, `SortKey`).
  - Campos de auditoría (`CreatedAt`, `UpdatedAt`, `DueDate` opcional).

### Modificado
- Autorización conectada a políticas de roles de proyecto en todos los endpoints.
- DTOs, mapeadores y validadores sincronizados con invariantes de dominio.
- **Frontend eliminado**; el proyecto ahora es exclusivamente backend.

### Migraciones
- `ProjectBoardSchemaUpdate` introduciendo el esquema completo del tablero.
- `ProjectBoard_AssignmentsRowVersion_ProviderChecks` mejorando el manejo de RowVersion.

### Pruebas
- Se añadieron y ampliaron pruebas para los flujos principales del tablero y el registro automático de actividades.
- Cobertura mínima ≥60% mantenida.

### Notas
- Versión solo backend.
- Etiqueta creada: `v0.3.0`.

## [0.2.0] - 2025-10-08

### Añadido
- **Backend**
  - Gestión completa de proyectos y membresías (CRUD).
  - Entidades: `Project`, `ProjectMember` con jerarquía de roles (`Owner`, `Admin`, `Member`, `Reader`).
  - Tipo `DomainMutation` introducido para resultados consistentes en repositorios.
  - Políticas de autorización por rol de proyecto (`ProjectOwner`, `ProjectAdmin`, `ProjectMember`, `ProjectReader`).
  - Servicios: `ProjectMemberService`, `ProjectMembershipReader` y repositorios asociados.
  - Registro de políticas mediante `AddProjectAuthorization()` en `AuthorizationExtensions`.
  - Pruebas unitarias e integradas para:
    - Manejadores de autorización basados en roles.
    - Lectura de membresías y actualización de roles.
    - Contratos de repositorio y persistencia.
  - Refactorización de infraestructura para coherencia entre repositorios y servicios.

- **Frontend**
  - Expansión del área de **gestión de proyectos** con nuevas vistas que visualizan características del backend mediante una interfaz limpia y mínima.
  - Introducción de **ProjectsPage**, **ProjectBoardPage**, **ProjectMembersPage**, **ProjectSettingsPage** y **UsersPage**.
  - Integración con la capa de autenticación y cliente API (`apiFetch`) con propagación de tokens y cierre automático en 401.

### Modificado
- **Frontend**
  - **Decisión:** el frontend actúa como capa delgada de visualización para el backend.
  - **Pruebas eliminadas:** se eliminaron todas las pruebas del frontend para enfocar el esfuerzo en el backend.

- **CI/CD**
  - Actualización de workflows para **omitir pruebas del frontend** y mantener el control de cobertura solo en backend.

- **Backend**
  - Inyección de dependencias unificada para las capas Application e Infrastructure.
  - Firmas de repositorios actualizadas para devolver `DomainMutation`.
  - CI y suites de prueba actualizadas acorde a la nueva estructura.

### Notas
- Se consolidó la autenticación de v0.1.0 con la gestión completa de proyectos y membresías.
- El frontend es funcional para operaciones de proyectos y deliberadamente liviano.

## [0.1.0] - 2025-10-03

### Añadido
- **Backend**
  - Endpoints de autenticación: `POST /auth/register`, `POST /auth/login`, `GET /auth/me`.
  - Hashing de contraseñas con PBKDF2 y emisión/validación de tokens JWT.
  - Swagger con soporte JWT (solo en desarrollo).
  - Seeder de infraestructura para entorno de desarrollo.
  - Configuración Docker para dev y prod con `compose.yaml`.
  - Workflows CI para compilación, pruebas (unitarias + integradas) y control de cobertura.
  - Scripts unificados (`run.js`, `dev.*`, `prod.*`, `test.*`) para flujos de desarrollo.

- **Frontend**
  - Configuración base: Vite + React + TypeScript + Tailwind CSS con estructura basada en features.
  - Generación de cliente OpenAPI (`npm run gen:api`) y validación de contratos (`npm run check:contract`).
  - Almacenamiento de sesión con persistencia de tokens y cierre automático en 401.
  - Rutas protegidas y guards.
  - Páginas:
    - Página de inicio mínima.
    - Formularios de inicio de sesión y registro con validación.
    - Página protegida `/me` obteniendo el perfil del usuario desde la API.
  - Build multietapa Docker e infraestructura (`compose.dev.yaml`, `compose.prod.yaml`) para web.
  - Job CI para compilación, verificación de tipos, generación de cliente API y validación de contratos.
  - Pruebas básicas de componentes y flujo de inicio de sesión con cobertura ≥60%.

### Notas
- Primer hito funcional con integración backend + frontend.
- Etiqueta creada: `v0.1.0`.
