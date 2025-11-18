# CollabTask

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./README.md)

**CollabTask** es un backend colaborativo de gestión de tareas desarrollado con **ASP.NET Core 8** siguiendo los principios de **Arquitectura Limpia (Clean Architecture)**.

Proporciona una API Kanban en tiempo real que permite la colaboración multiusuario, control de concurrencia optimista y un modelo de dominio sólido.  
Versión estable actual: **v1.0.2**


## Funcionalidades Principales

- **Proyectos y Miembros** — Colaboración basada en proyectos con políticas de acceso por rol.  
- **Tablero Kanban** — Carriles, Columnas, Tareas, Notas, Asignaciones y Actividades.  
- **Actualizaciones en Tiempo Real** — Hub de SignalR (`/hubs/board`) para emisión de eventos por proyecto.  
- **Concurrencia Optimista** — Gestionada mediante `RowVersion`, `ETag` y cabeceras `If-Match`.  
- **Registro Automático de Actividades** — Las actividades de tarea (creación, edición, movimiento, propiedad, notas) se registran automáticamente.  
- **Modelo de Dominio Fuerte** — Objetos de valor e invariantes que garantizan la consistencia de los datos.  
- **Arquitectura Limpia** — Capas estrictamente separadas y direccionalidad controlada de dependencias.  
- **Testing Extensivo** — Pruebas unitarias, de integración y de concurrencia con cobertura mínima garantizada.  
- **Documentación Completa** — Archivos técnicos 01–06 y visión general técnica bilingüe.  


## Visión de la Arquitectura

**CollabTask** se estructura en cuatro capas independientes:

| Capa | Responsabilidad |
|------|------------------|
| **Dominio** | Entidades, Objetos de Valor, invariantes y reglas de negocio. |
| **Aplicación** | Casos de uso, validaciones y orquestación transaccional mediante `IUnitOfWork`. |
| **Infraestructura** | Persistencia con EF Core, repositorios, interceptores, migraciones e inyección de dependencias. |
| **API** | Endpoints REST mínimos que exponen proyectos, tableros y tareas (REST + Realtime). |

Los límites limpios permiten pruebas aisladas y alta mantenibilidad.  


## Documentación Técnica

Toda la documentación se encuentra en la carpeta `/docs`:

| Archivo | Descripción |
|----------|--------------|
| [01_Domain_Model.md](docs/01_Domain_Model.es.md) | Entidades de dominio, relaciones y objetos de valor. |
| [02_Authorization_Policies.md](docs/02_Authorization_Policies.es.md) | Políticas de autorización a nivel de sistema y proyecto. |
| [03_API_Endpoints.md](docs/03_API_Endpoints.es.md) | Endpoints REST y sus contratos HTTP. |
| [04_DTOs.md](docs/04_DTOs.es.md) | Objetos de transferencia de datos (entrada/salida). |
| [05_Application_Services_and_Repositories.md](docs/05_Application_Services_and_Repositories.es.md) | Servicios de aplicación e interacción con repositorios. |
| [06_EFCore_Configuration.md](docs/06_EFCore_Configuration.es.md) | Configuración de EF Core, constraints y control de concurrencia. |
| [TECHNICAL_OVERVIEW.md](docs/TECHNICAL_OVERVIEW.es.md) | Descripción técnica y arquitectónica principal. |

Todos los documentos están disponibles en inglés y español.  


## Desarrollo Local

**Requisitos:**  
- .NET 8 SDK  
- Node.js ≥ 20  
- Docker Desktop

### Comandos
```bash
npm run dev [args]     # Ejecutar entorno de desarrollo
npm run prod [args]    # Ejecutar perfil de producción
```

**Argumentos comunes:**  
`rebuild | up | down | health | logs`

URL por defecto de la API: **http://localhost:8080**


## Testing

Ejecuta las suites de tests mediante los scripts unificados:

```bash
npm run test:unit
npm run test:integration
npm run test:sqlserver
npm run test:all
```

- **Unit tests** validan las reglas del dominio y la lógica de aplicación.  
- **Integration tests** verifican la persistencia, la concurrencia y el comportamiento de extremo a extremo.  
- **SQL Server tests** garantizan la compatibilidad con la base de datos y la ejecución correcta a nivel de SQL Server usando Testcontainers.  

## Integración Continua

El pipeline de **GitHub Actions** garantiza:
- Ejecución de build y tests con cobertura mínima (≥75%).  
- Verificación de construcción de imagen de contenedor.  
- Comprobación de consistencia y formato de documentación.  


## Estructura del Proyecto

```
.github/        → Workflows de CI
/api/           → Backend ASP.NET Core (Domain, Application, Infrastructure, API)
/docs/          → Documentación técnica (bilingüe 01–06 + Technical Overview)
/infra/         → Configuraciones de Docker Compose e infraestructura
/scripts/       → Scripts unificados de ejecución y testing
```


## Licencia

Este proyecto está licenciado bajo **MIT License**.  
Consulta el archivo [LICENSE](./LICENSE) para más detalles.  


## Documentación Relacionada

- [CHANGELOG.md](./CHANGELOG.md) — Historial de versiones.  
- [docs/TECHNICAL_OVERVIEW.md](docs/TECHNICAL_OVERVIEW.es.md) — Arquitectura y diseño del sistema.  
- [docs/01_Domain_Model.md](docs/01_Domain_Model.es.md) — Referencia del modelo de dominio.  
- [docs/02_Authorization_Policies.md](docs/02_Authorization_Policies.es.md) — Modelo de control de acceso por rol.  
- [docs/03_API_Endpoints.md](docs/03_API_Endpoints.es.md) — Definición de endpoints REST.  
- [docs/04_DTOs.md](docs/04_DTOs.es.md) — Especificaciones de DTOs.  
- [docs/05_Application_Services_and_Repositories.md](docs/05_Application_Services_and_Repositories.es.md) — Lógica de aplicación y persistencia.  
- [docs/06_EFCore_Configuration.md](docs/06_EFCore_Configuration.es.md) — Mapeos y constraints de EF Core.  