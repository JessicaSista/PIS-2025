# Guía de Contribución

## Ramas

- **main** → solo código estable, es lo que se muestra si nos piden ver avance.
- **develop** → integra cambios antes de pasar a `main`.
- **feature/*** → una por funcionalidad, **el nombre debe incluir el ticket**. Ejemplos:
  - `feature/PROY-123-dashboard-editor`
  - `feature/PROY-456-login-bugfix`

> Esto permite relacionar automáticamente el trabajo con Jira y mantener trazabilidad.

## Pull Requests

- Todo va a `develop` (no se mergea directo a `main`).
- Requiere **2 aprobaciones** de personas distintas del autor.
- Debe pasar el pipeline **CI** (compilación y tests).
- Cada PR debe indicar el ticket asociado en el título o descripción.
- Se recomienda borrar la rama `feature/*` tras el merge.

## Commits

Commits claros, como por ejemplo:

- `feat(PROY-123): agrega editor de widgets`  <!-- feat = nueva feature -->
- `fix(PROY-456): corrige null en reporte`    <!-- fix = bugfix -->
- `refactor(PROY-789): extraer lógica de validación a función separada`
- `test(PROY-123): agrega test para componente de login`
- `perf(PROY-321): optimiza consulta de reportes`
- `style(PROY-123): formatea código según estándar de proyecto`

## Tests

- Ubicar tests en `/tests`.
- Correrlos localmente antes de abrir PR (ahorra tiempo de los reviewers y del equipo).

## CI/CD

- El pipeline **CI** corre en cada push/PR a `develop` y `main`.
- Debe compilar sin errores.
- Los tests son obligatorios cuando existan.

## Estilo y revisiones

- Preferir PRs chicos y específicos.
- Responder comentarios en los PR y pedir re-review si cambiaste algo relevante.
