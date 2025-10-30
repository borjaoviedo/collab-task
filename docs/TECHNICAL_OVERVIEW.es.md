# Descripción Técnica — CollabTask v1.0.0

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./TECHNICAL_OVERVIEW.md)

Este documento proporciona una descripción detallada de la **arquitectura del backend de CollabTask**, sus patrones de diseño y principios internos.

Complementa el archivo [README.es.md](../README.es.md), centrándose en los **aspectos técnicos y arquitectónicos** del sistema.

---

## 1. Estilo Arquitectónico

**CollabTask** sigue los principios de la **Arquitectura Limpia (Clean Architecture)**, garantizando:
- Separación clara de responsabilidades entre las capas **Dominio**, **Aplicación**, **Infraestructura** y **API/Presentación**.  
- Inversión de dependencias: las capas internas no hacen referencia a las externas.  
- Alta capacidad de prueba, mantenibilidad y aislamiento de las reglas de negocio.

### Responsabilidades por Capa

| Capa | Descripción |
|------|--------------|
| **Dominio** | Lógica de negocio principal: entidades, objetos de valor, invariantes y reglas de dominio. |
| **Aplicación** | Orquesta los casos de uso, maneja validaciones y gestiona transacciones mediante `IUnitOfWork`. |
| **Infraestructura** | Persistencia (EF Core), interceptores, repositorios, migraciones e integraciones externas. |
| **API/Presentación** | Expone endpoints REST mínimos y hubs SignalR. Maneja errores, filtros y documentación OpenAPI. |

Dirección de dependencias:

```
API → Aplicación → Dominio
API → Infraestructura (solo para inyección de dependencias)
```

---

## 2. Patrones Fundamentales

### 2.1 Diseño Dirigido por el Dominio (DDD)
Las entidades y objetos de valor representan el núcleo del negocio.  
Todas las invariantes y reglas se aplican en constructores o fábricas estáticas (por ejemplo, `User.Create()`, `TaskItem.Create()`).

**Objetos de Valor** incluyen `Email`, `UserName`, `ProjectName`, `LaneName`, `ColumnName`, `TaskTitle`, `TaskDescription`, `NoteContent` y más.

Todas las entidades de dominio utilizan **tokens de concurrencia optimista** (`RowVersion`) y contienen **campos de auditoría** (`CreatedAt`, `UpdatedAt`).

---

### 2.2 Unit of Work (UoW)

Introducido en la versión v1.0.0, el patrón **Unit of Work** centraliza el control de la persistencia:

```csharp
public interface IUnitOfWork
{
    Task<DomainMutation> SaveAsync(CancellationToken ct = default);
}
```

- Los servicios de aplicación ya no llaman directamente a `DbContext.SaveChangesAsync()`.
- Los repositorios realizan las operaciones, y el UoW confirma los cambios de forma atómica.
- Garantiza resultados consistentes (`PrecheckStatus`) y manejo de concurrencia.
- Asegura coherencia transaccional y simplifica las pruebas.

---

### 2.3 DomainMutation y PrecheckStatus

Para unificar los resultados de las mutaciones y el manejo de concurrencia, los repositorios devuelven tipos estandarizados:

- `DomainMutation` → Envuelve resultados (NoOp, NotFound, Updated, Created, Deleted, Conflict).  
- `PrecheckStatus` → Representa validaciones previas a la operación (NotFound, NoOp, Conflict, Ready).

Estos tipos simplifican el mapeo de resultados a respuestas HTTP en la capa API.

---

### 2.4 Control de Concurrencia

El backend aplica **concurrencia optimista** utilizando versiones de fila (`RowVersion`) de EF Core y precondiciones HTTP:

| Mecanismo | Descripción |
|------------|-------------|
| `RowVersion` | Matriz de bytes actualizada en cada modificación. |
| `ETag` | Versión codificada expuesta mediante cabeceras HTTP. |
| `If-Match` | Cabecera requerida para operaciones de actualización/eliminación. |
| `RequireIfMatch` | Filtro que garantiza la presencia de la precondición. |
| `RejectIfMatch` | Filtro usado para endpoints que no deben incluir precondiciones (p. ej., creación). |

Respuestas HTTP:
- `412 Precondition Failed` → Desajuste de versión (`RowVersion`).  
- `428 Precondition Required` → Falta la cabecera `If-Match`.  
- `409 Conflict` → Conflicto lógico o de dominio.

---

### 2.5 Registro Automático de Actividades

Cada modificación en una tarea genera automáticamente una entrada de `TaskActivity` que representa acciones como:
- Creación, edición o movimiento de tareas.
- Cambios en propietario o co-propietario.
- Creación, edición, movimiento o eliminación de notas.

El registro se realiza en la capa de Aplicación y se persiste de forma atómica junto con la entidad principal.

---

### 2.6 Comunicación en Tiempo Real

Las actualizaciones en tiempo real se implementan mediante **SignalR**:
- Hub: `/hubs/board`
- Grupo: `project:{projectId}`
- Los eventos se emiten a través de `BoardNotifier`.
- Formato del contrato:
  ```json
  { "type": "task.updated", "projectId": "guid", "payload": { ... } }
  ```

El comportamiento en tiempo real está completamente desacoplado mediante notificaciones del mediador desde los servicios de escritura.

---

## 3. Modelo de Autorización

La autorización en **CollabTask** se aplica tanto a nivel **de sistema** como **de proyecto**.

### 3.1 Roles del Sistema
| Rol | Descripción |
|------|-------------|
| **SystemAdmin** | Administrador global con acceso total a todos los proyectos. |
| **User** | Usuario autenticado por defecto. |

### 3.2 Roles de Proyecto
| Rol | Capacidades |
|------|--------------|
| **ProjectOwner** | Permisos completos; puede eliminar el proyecto, gestionar miembros y modificar cualquier elemento del tablero. |
| **ProjectAdmin** | Puede invitar o eliminar miembros (excepto al propietario) y gestionar carriles, columnas y tareas. |
| **ProjectMember** | Puede crear, editar y mover tareas y notas. |
| **ProjectReader** | Acceso de solo lectura a todos los datos del tablero. |

### 3.3 Mecanismo de Autorización
Las políticas se registran mediante `AddProjectAuthorization()`:
```csharp
services.AddProjectAuthorization();
```
Políticas disponibles:
- `ProjectOwner`
- `ProjectAdmin`
- `ProjectMember`
- `ProjectReader`

Cada endpoint define explícitamente la política requerida:
```csharp
group.MapPut("/{taskId}", UpdateTask)
     .RequireAuthorization(ProjectPolicies.ProjectMember);
```

Los claims se extraen del token JWT y la membresía de proyecto se valida mediante `ProjectMemberReadService`.

---

## 4. Capa de Persistencia

La persistencia se implementa con **Entity Framework Core 8**.

### Características
- `AppDbContext` configurado con tokens de concurrencia, constraints y comportamientos específicos por proveedor.
- SQL Server: constraints CHECK e índices filtrados.
- SQLite (modo test): convertidores y emulación de `RowVersion`.
- Auditoría mediante `AuditingSaveChangesInterceptor` y `IDateTimeProvider`.

### Migraciones
Cada versión puede incluir una o más migraciones bajo `Infrastructure/Data/Migrations/`.

Migraciones finales en v1.0.0:
- `Rename_TaskNote_AuthorId_To_UserId`
- `InfraSchemaFinalization`

---

## 5. Estrategia de Pruebas

- **Unitarias:** para las capas Dominio y Aplicación.  
- **Integración:** validación de persistencia, concurrencia y endpoints.  
- **Tiempo real:** serialización y emisión de eventos del hub.  
- Cobertura mínima ≥ 75% impuesta en CI.

Las pruebas están organizadas por feature con ayudantes reutilizables en `TestHelpers`.

---

## 6. Pipeline CI/CD

Automatizado con GitHub Actions:

1. **Compila y prueba** el backend usando .NET 8 SDK.  
2. **Ejecuta pruebas unitarias y de integración** con control de cobertura.  
3. **Construye la imagen Docker** del backend.  

---

## 7. Descripción de la API

Todos los endpoints se definen con Minimal APIs siguiendo una estructura jerárquica:

```
/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}
/projects/{projectId}/members
/auth/login
/auth/register
```

Cada módulo (Tasks, Notes, Columns, etc.) incluye:
- DTOs de entrada/salida.  
- Mapeadores.  
- Validadores (FluentValidation).  
- Filtros de endpoint para concurrencia y autorización.  

La documentación OpenAPI (`openapi.json`) se genera automáticamente y se mantiene versionada.

---

## 8. Estructura de Carpetas (Backend)

El repositorio se organiza siguiendo las capas de Arquitectura Limpia. Los nombres de las carpetas pueden variar ligeramente según el módulo, pero la estructura principal es:

```
/src/
 ├─ Api/
 │   ├─ Endpoints/
 │   ├─ Errors/
 │   ├─ Filters/
 │   └─ Realtime/
 ├─ Application/
 │   └─ Entity/
 │       ├─ Abstractions/
 │       ├─ DTOs/
 │       ├─ Mapping/
 │       ├─ Services/
 │       └─ Validation/
 ├─ Domain/
 │   ├─ Common/
 │   ├─ Entities/
 │   ├─ ValueObjects/
 │   └─ Enums/
 └─ Infrastructure/
     ├─ Common/
     └─ Data/
         ├─ Configurations/
         ├─ Initialization/
         ├─ Interceptors/
         ├─ Repositories/
         └─ Seeders/
```

---

## 9. Seguridad y Autenticación

- Autenticación basada en JWT mediante `JwtTokenService`.  
- Contraseñas cifradas con PBKDF2 (`Pbkdf2PasswordHasher`).  
- El token incluye identificador de usuario, correo electrónico y roles.  
- Usuarios por defecto sembrados en modo desarrollo (`DevSeeder`).  

---

## 10. Resumen

**CollabTask v1.0.0** consolida:
- Una API REST backend completa para la gestión colaborativa de tareas.  
- Modelo de dominio robusto y consistencia transaccional.  
- Colaboración en tiempo real mediante SignalR.  
- Documentación y pruebas completas.  
- Arquitectura de producción basada en Clean Architecture y principios DDD.  

---

> **CollabTask** — Limpio, concurrente y colaborativo por diseño.
