# Políticas de Autorización  

> 🇪🇸 Este archivo está en español.  
> 🇬🇧 [English version available here](./02_Authorization_Policies.md)


## Tabla de Contenidos
- [ProjectReaderPolicy](#projectreaderpolicy)
- [ProjectMemberPolicy](#projectmemberpolicy)
- [ProjectAdminPolicy](#projectadminpolicy)
- [ProjectOwnerPolicy](#projectownerpolicy)
- [SystemAdminPolicy](#systemadminpolicy)

---------------------------------------------------------------------------------

Este documento define las **políticas de autorización** utilizadas en el backend de **CollabTask**.  
Cada política especifica qué roles de proyecto pueden acceder o modificar recursos, garantizando un control de acceso coherente y seguro dentro de la API.

> **Notas**
> - Las políticas se evalúan después de la autenticación y la resolución del rol del proyecto.  
> - Cada política se aplica de forma declarativa mediante atributos en los endpoints de la capa API.  
> - Las políticas son jerárquicas: los roles superiores (por ejemplo, `Owner`) satisfacen implícitamente los requisitos de roles inferiores (por ejemplo, `Admin`, `Member`, `Reader`).

| Nombre de la Política    | Se Aplica a                  | Permisos / Acciones Permitidas                                                                                      | Ejemplo de Endpoints                                                  |
|---------------------------|------------------------------|---------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| **ProjectReaderPolicy**   | Rol `Reader` y superiores     | Puede ver detalles del proyecto, líneas, columnas, tareas y notas. No tiene permisos de modificación                | `GET /projects/{projectId}/lanes`                                    |
| **ProjectMemberPolicy**   | Rol `Member` y superiores     | Puede crear y editar tareas o notas dentro del proyecto. No puede modificar configuración o membresías del proyecto | `POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks` |
| **ProjectAdminPolicy**    | Rol `Admin` y superiores      | Puede gestionar columnas, líneas y asignaciones de tareas. Puede editar metadatos del proyecto. No puede eliminar proyectos completos | `PATCH /projects/{projectId}/columns/{columnId}`                     |
| **ProjectOwnerPolicy**    | Rol `Owner`                  | Control total sobre el proyecto y todos sus recursos, incluyendo membresías y operaciones de eliminación             | `DELETE /projects/{projectId}`                                       |
| **SystemAdminPolicy**     | Rol `SystemAdmin` de la plataforma | Acceso administrativo global a todos los proyectos y endpoints. Puede gestionar usuarios, roles y datos de cualquier proyecto | `GET /admin/users`                                                   |


## Detalles Adicionales

### ProjectReaderPolicy  
Destinada a usuarios con acceso de solo lectura a un proyecto.  
Permite visualizar todas las líneas, columnas, tareas, notas y actividades sin permitir modificaciones.

### ProjectMemberPolicy  
Destinada a colaboradores regulares del proyecto.  
Permite crear, editar y comentar tareas dentro de los proyectos asignados, pero no realizar cambios estructurales o administrativos.

### ProjectAdminPolicy  
Destinada a administradores del proyecto.  
Extiende los privilegios de `Member` con permisos para modificar la estructura del proyecto (líneas, columnas) y gestionar asignaciones o metadatos de tareas.  
No otorga permisos de propietario, como eliminar proyectos o escalar roles de miembros.

### ProjectOwnerPolicy  
Asignada dinámicamente al propietario del proyecto.  
Otorga acceso sin restricciones a todas las operaciones del proyecto, incluyendo gestión de miembros, actualización de roles y eliminación del proyecto.  
Es el nivel más alto de autorización dentro del ámbito de un proyecto.

### SystemAdminPolicy  
Reservada para administradores de toda la plataforma.  
Proporciona acceso sin restricciones a todas las rutas de la API, operaciones de gestión de usuarios y endpoints de mantenimiento del sistema.
