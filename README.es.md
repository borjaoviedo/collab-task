# CollabTask

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./README.md)

**CollabTask** es un backend colaborativo de gestión de tareas desarrollado con **ASP.NET Core 8**, siguiendo los principios de **Arquitectura Limpia (Clean Architecture)**.

Proporciona una API de tablero Kanban en tiempo real que admite colaboración multiusuario, concurrencia optimista y un sólido modelado de dominio.

---

## Versión actual — v1.0.0

Backend está listo para su publicación.

- Documentación completa y comentarios XML en todas las capas.  
- Concurrencia optimista con soporte para `ETag` / `If-Match`.  
- Orquestación de persistencia basada en el patrón **Unit of Work (`IUnitOfWork`)**.  
- Separación limpia entre las capas **Domain**, **Application**, **Infrastructure** y **API**.  
- Conjunto de pruebas completo (unitarias + de integración, ≥75% de cobertura).  

Para explicaciones técnicas detalladas, consulta [TECHNICAL_OVERVIEW.es.md](docs/TECHNICAL_OVERVIEW.es.md).  
Para el historial de versiones, consulta [CHANGELOG.es.md](./CHANGELOG.es.md).

---

## Descripción de la arquitectura

**CollabTask** está estructurado en cuatro capas independientes:

| Capa | Responsabilidad |
|------|-----------------|
| **Dominio** | Entidades, Objetos de Valor, invariantes y reglas de negocio. |
| **Aplicación** | Casos de uso, validación y orquestación transaccional mediante `IUnitOfWork`. |
| **Infraestructura** | Persistencia con EF Core, repositorios, interceptores, migraciones y configuración de inyección de dependencias. |
| **API/Presentación** | Endpoints mínimos que exponen proyectos, tableros y tareas (REST + Realtime). |

Los límites claros entre capas permiten pruebas aisladas y un mantenimiento sencillo.  

---

## Funcionalidades clave

- **Proyectos y miembros** — Colaboración basada en proyectos con políticas de acceso por rol.  
- **Tablero Kanban** — Carriles, Columnas, Tareas, Notas, Asignaciones y Actividades.  
- **Actualizaciones en tiempo real** — Hub de SignalR (`/hubs/board`) para emisión de eventos por proyecto.  
- **Concurrencia optimista** — Aplicada mediante `RowVersion`, `ETag` y cabeceras `If-Match`.  
- **Registro automático de actividades** — Las acciones sobre tareas (crear, editar, mover, asignar, notas) se registran automáticamente.  
- **Modelo de dominio sólido** — Objetos de valor e invariantes que aseguran la coherencia de los datos.  
- **Arquitectura limpia** — Direccionalidad estricta de dependencias.  
- **Pruebas extensas** — Pruebas unitarias, de integración y de concurrencia con cobertura garantizada.  

---

## Desarrollo local

**Requisitos:**  
- .NET 8 SDK  
- Node.js ≥ 20  
- Docker Desktop

### Comandos
```bash
npm run dev [args]     # Ejecuta el entorno de desarrollo
npm run prod [args]    # Ejecuta el perfil de producción
```

**Argumentos comunes:**  
`rebuild | up | down | health | logs`

URL por defecto de la API: **http://localhost:8080**

---

## Pruebas

Ejecuta los conjuntos de pruebas mediante los scripts unificados:

```bash
npm run test:unit
npm run test:infra
npm run test:all
```

- Las pruebas unitarias cubren la lógica de dominio y aplicación.  
- Las pruebas de integración validan la persistencia, concurrencia y comportamiento de los endpoints.  

---

## Integración continua

El pipeline de GitHub Actions garantiza:
- Compilación y ejecución de pruebas con control de cobertura (≥75%).  
- Verificación de compilación de la imagen de contenedor.  

---

## Estructura del proyecto

```
.github/        → Workflows de CI
/api/           → Backend ASP.NET Core (Domain, Application, Infrastructure, API)
/infra/         → Configuración de Docker Compose e infraestructura
/scripts/       → Scripts unificados para ejecución y pruebas
```

---

## Licencia

Este proyecto está bajo la licencia **MIT**.  
Consulta el archivo [LICENSE](./LICENSE) para más detalles.

---

## Documentación relacionada

- [CHANGELOG.es.md](./CHANGELOG.es.md) — Historial completo de versiones.  
- [TECHNICAL_OVERVIEW.es.md](docs/TECHNICAL_OVERVIEW.es.md) — Arquitectura, patrones y modelo de autorización.  

---

> **CollabTask** v1.0.0 — backend listo para publicación pública, documentado y optimizado para el mantenimiento.
