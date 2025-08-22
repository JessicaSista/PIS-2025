# Guía de Contribución

## Ramas

- **main** → solo código estable, es lo que se muestra por ej. si nos piden ver avance
- **develop** → integra cambios antes de pasar a `main`
- **feature/\*** → una por funcionalidad. Ej: `feature/dashboard-editor`

## Pull Requests

- Todo va a `develop` (no se mergea directo a `main`)
- Requiere **2 aprobaciones** de personas distintas del autor
- Debe pasar el pipeline **CI** (compilación y tests)
- Se recomienda borrar la rama `feature/*` tras el merge

## Commits

Commits claros, como por ejemplo:

- `feat: agrega editor de widgets` <!--el feat se refiere a agregar una feature>
- `fix: corrige null en reporte` <!--el fix refiere a corregir un bug o algo>
- `refactor: extraer lógica de validación a función separada` <!--el refactor refiere a cambios en el código que no corrigen bugs ni agregan features>
- `test: agrega test para componente de login`

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
