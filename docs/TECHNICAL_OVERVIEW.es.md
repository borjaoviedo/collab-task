# Descripción Técnica — CollabTask

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./TECHNICAL_OVERVIEW.md)

## Tabla de Contenidos
- [Visión Arquitectónica](#1-visión-arquitectónica)
- [Diagrama Global de Arquitectura](#2-diagrama-global-de-arquitectura)
- [Conceptos Técnicos Fundamentales](#3-conceptos-técnicos-fundamentales)
- [Mapa de Documentación](#4-mapa-de-documentación)
- [Entorno Técnico y Herramientas](#5-entorno-técnico-y-herramientas)
- [Calidad y Mantenimiento](#6-calidad-y-mantenimiento)
- [Resumen](#7-resumen)

---------------------------------------------------------------------------------

Este documento ofrece una **visión técnica y arquitectónica** del backend de CollabTask.  
Sirve como **referencia técnica raíz** para toda la documentación del directorio `/docs`.


## 1. Visión Arquitectónica

**CollabTask** se basa en los principios de **Arquitectura Limpia (Clean Architecture)** y **Diseño Dirigido por el Dominio (DDD)**.  
El objetivo es aislar la lógica de negocio de las dependencias técnicas, favoreciendo la mantenibilidad, escalabilidad y capacidad de prueba.

### Principios Arquitectónicos Clave
- **Inversión de dependencias:** las capas externas dependen de abstracciones de las internas.  
- **Alta cohesión y bajo acoplamiento:** cada capa tiene una responsabilidad clara.  
- **Seguridad transaccional y de concurrencia:** control optimista con `RowVersion` + `ETag`.  
- **Consistencia transversal:** las reglas de negocio se aplican desde el dominio y los servicios de aplicación.


## 2. Diagrama Global de Arquitectura

Representación vertical del flujo de dependencias:

```
+------------------------------------------------------+
|                     Capa API                         |
|------------------------------------------------------|
| • Endpoints REST mínimos (Projects, Tasks, Notes)    |
| • Filtros: RequireIfMatch / RejectIfMatch            |
| • Políticas de autorización (ProjectOwner, Member…)  |
| • Hub SignalR: /hubs/board                           |
| • OpenAPI / Gestión de errores / Validación DTOs     |
+----------------------------↓-------------------------+
|                 Capa de Aplicación                   |
|------------------------------------------------------|
| • Servicios de casos de uso (CreateTask, MoveTask…)  |
| • Unit of Work (IUnitOfWork.SaveAsync)               |
| • Mapeo y validación (FluentValidation)              |
| • Resultados PrecheckStatus / DomainMutation         |
| • BoardNotifier para actualizaciones en tiempo real  |
+----------------------------↓-------------------------+
|                    Capa de Dominio                   |
|------------------------------------------------------|
| • Entidades (User, Project, Lane, Column, Task…)     |
| • Objetos de valor (Email, UserName, ProjectName…)   |
| • Invariantes y reglas de negocio                    |
| • Tokens de concurrencia RowVersion                  |
| • Eventos de dominio y campos de auditoría           |
+----------------------------↓-------------------------+
|                 Capa de Infraestructura              |
|------------------------------------------------------|
| • Persistencia con EF Core 8 (AppDbContext)          |
| • Repositorios y configuraciones                     |
| • AuditingSaveChangesInterceptor                     |
| • Migraciones y Seeders                              |
| • Integración con SQL Server y SQLite (tests)        |
+------------------------------------------------------+
```

Dirección de dependencias:

```
API → Aplicación → Dominio
API → Infraestructura (solo para inyección de dependencias)
```


## 3. Conceptos Técnicos Fundamentales

### Diseño Dirigido por el Dominio (DDD)
Las entidades y objetos de valor representan el núcleo del negocio.  
Las reglas se aplican al construir las entidades o mediante fábricas estáticas.

### Unit of Work
Centraliza la persistencia para garantizar operaciones atómicas:
```csharp
Task<DomainMutation> SaveAsync(MutationKind kind, CancellationToken ct = default);
```

### DomainMutation y PrecheckStatus
Estandarizan los resultados de las operaciones y permiten mapearlos directamente a códigos HTTP.

### Concurrencia Optimista
- `RowVersion` gestionado por EF Core.  
- `ETag` expuesto por HTTP.  
- `If-Match` requerido en actualizaciones/eliminaciones.  
- Respuestas estándar: `412`, `428`.

### Colaboración en Tiempo Real
SignalR difunde eventos por grupo de proyecto:
```json
{ "type": "task.updated", "projectId": "guid", "payload": { ... } }
```


## 4. Mapa de Documentación

| Archivo | Propósito |
|----------|------------|
| **01_Domain_Model.md** | Define entidades, relaciones y objetos de valor. |
| **02_Authorization_Policies.md** | Describe la autorización a nivel de sistema y proyecto. |
| **03_API_Endpoints.md** | Enumera los endpoints REST y sus contratos. |
| **04_DTOs.md** | Detalla las estructuras de datos de entrada/salida. |
| **05_Application_Services_and_Repositories.md** | Explica la interacción entre casos de uso y persistencia. |
| **06_EFCore_Configuration.md** | Documenta la configuración EF Core, constraints y concurrencia. |

Estos seis documentos amplían la información técnica del presente resumen general.


## 5. Entorno Técnico y Herramientas

| Área | Tecnología |
|------|-------------|
| **Framework** | .NET 8 |
| **ORM** | Entity Framework Core 8 |
| **Base de datos** | SQL Server (dev/prod), SQLite (tests) |
| **Tiempo real** | SignalR |
| **Testing** | xUnit + Testcontainers |
| **CI/CD** | GitHub Actions |
| **Contenerización** | Docker + Docker Compose |
| **Autenticación** | JWT Bearer (contraseñas con PBKDF2) |


## 6. Calidad y Mantenimiento

- **Cobertura de pruebas ≥ 75%** verificada en CI.  
- **Código alineado con principios SOLID y Clean Architecture.**  
- **Auditoría** mediante timestamps e interceptores.  
- **Modelo de ramas:** feature → PR → merge → tag release.  
- **Esquema OpenAPI** versionado en cada release.  


## 7. Resumen

**CollabTask v1.0.2** ofrece:
- Un backend modular y mantenible para la gestión colaborativa de tareas.  
- Separación limpia entre Dominio, Aplicación, Infraestructura y API.  
- Concurrencia optimista y colaboración en tiempo real.  
- Documentación técnica coherente y completa.  