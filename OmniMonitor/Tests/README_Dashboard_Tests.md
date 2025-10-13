# Documentación de Endpoints de Dashboards

## Descripción General

El sistema de dashboards permite a los usuarios crear dashboards personalizables eligiendo visualizaciones existentes como tarjetas y configurando sus propiedades básicas de estilo (título, layout, tamaños, densidad).

**Importante**: Las tarjetas de un dashboard son visualizaciones existentes del sistema. Cada tarjeta hace referencia a una `Visualizacion` por su `IdVisualizacion`.

## Endpoints Disponibles

### 1. POST /api/dashboards - Crear Dashboard

Crea un nuevo dashboard personalizable.

#### Request Body
```json
{
  "nombre": "Mi Dashboard Personalizado",
  "descripcion": "Dashboard para monitoreo de KPIs",
  "tema": "dark",
  "layout": {
    "tarjetas": [
      {
        "cardId": 1,
        "posicionX": 0,
        "posicionY": 0,
        "ancho": 6,
        "alto": 4,
        "props": {
          "titulo": "Gráfico de Ventas",
          "tipoGrafico": "line",
          "color": "#3498db"
        }
      },
      {
        "cardId": 2,
        "posicionX": 6,
        "posicionY": 0,
        "ancho": 6,
        "alto": 4,
        "props": {
          "titulo": "Métricas de Usuario",
          "mostrarPorcentaje": true
        }
      }
    ],
    "configuracion": {
      "columnas": 12,
      "densidad": "normal",
      "tema": "dark"
    }
  }
}
```

#### Response (201 Created)
```json
{
  "idDashboard": 1,
  "username": "admin",
  "nombre": "Mi Dashboard Personalizado",
  "descripcion": "Dashboard para monitoreo de KPIs",
  "grupoVisualizacion": null,
  "jsonDiseno": "{\"tarjetas\":[...],\"configuracion\":{...}}",
  "fechaCreacion": "2024-01-15T10:30:00Z",
  "fechaModificacion": "2024-01-15T10:30:00Z",
  "layout": {
    "tarjetas": [...],
    "configuracion": {...}
  },
  "tarjetas": [
    {
      "idGrupoVisualizacion": 1,
      "cardId": 1,
      "posicionX": 0,
      "posicionY": 0,
      "ancho": 6,
      "alto": 4,
      "propsConfiguracion": "{\"titulo\":\"Gráfico de Ventas\",\"tipoGrafico\":\"line\",\"color\":\"#3498db\"}",
      "fechaAgregado": "2024-01-15T10:30:00Z",
      "visualizacion": {
        "idVisualizacion": 1,
        "nombre": "Ventas Mensuales",
        "fechaDesde": "2024-01-01T00:00:00Z",
        "fechaHasta": "2024-01-31T23:59:59Z",
        "jsonDesign": "..."
      }
    }
  ]
}
```

#### Errores Comunes
- **400 Bad Request**: Datos de entrada inválidos
  ```json
  {
    "message": "El nombre del dashboard es obligatorio"
  }
  ```
- **400 Bad Request**: Dashboard duplicado
  ```json
  {
    "message": "Ya existe un dashboard con el nombre 'Mi Dashboard' para el usuario 'admin'."
  }
  ```
- **400 Bad Request**: CardIds inválidos
  ```json
  {
    "message": "Uno o más cardIds no existen en el sistema."
  }
  ```

### 2. GET /api/dashboards/{id} - Obtener Dashboard por ID

Obtiene un dashboard específico con su layout completo.

#### Response (200 OK)
```json
{
  "idDashboard": 1,
  "username": "admin",
  "nombre": "Mi Dashboard Personalizado",
  "descripcion": "Dashboard para monitoreo de KPIs",
  "grupoVisualizacion": null,
  "jsonDiseno": "...",
  "fechaCreacion": "2024-01-15T10:30:00Z",
  "fechaModificacion": "2024-01-15T10:30:00Z",
  "layout": {...},
  "tarjetas": [...]
}
```

#### Errores Comunes
- **404 Not Found**: Dashboard no encontrado
  ```json
  {
    "message": "No se encontró el dashboard con ID 999 para el usuario admin."
  }
  ```

### 3. GET /api/dashboards - Obtener todos los Dashboards

Obtiene la lista de todos los dashboards del usuario autenticado.

#### Response (200 OK)
```json
[
  {
    "idDashboard": 1,
    "nombre": "Mi Dashboard Personalizado",
    "descripcion": "Dashboard para monitoreo de KPIs",
    "fechaCreacion": "2024-01-15T10:30:00Z",
    "fechaModificacion": "2024-01-15T10:30:00Z",
    "cantidadTarjetas": 2
  },
  {
    "idDashboard": 2,
    "nombre": "Dashboard de Producción",
    "descripcion": "Monitoreo de métricas de producción",
    "fechaCreacion": "2024-01-14T15:20:00Z",
    "fechaModificacion": "2024-01-14T15:20:00Z",
    "cantidadTarjetas": 3
  }
]
```

### 4. POST /api/dashboards/validate-cards - Validar CardIds

Valida que una lista de cardIds existan en el sistema.

#### Request Body
```json
[1, 2, 3, 4]
```

#### Response (200 OK) - Válidos
```json
{
  "isValid": true,
  "message": "Todos los cardIds son válidos"
}
```

#### Response (200 OK) - Inválidos
```json
{
  "isValid": false,
  "message": "Algunos cardIds no existen en el sistema"
}
```

#### Errores Comunes
- **400 Bad Request**: Lista vacía
  ```json
  {
    "message": "La lista de cardIds no puede estar vacía"
  }
  ```

## Validaciones Implementadas

### Validaciones de Layout
- **Superposiciones**: El sistema valida que las tarjetas no se superpongan
- **Rangos de posición**: 
  - Posición X: 0-12
  - Posición Y: 0-100
  - Ancho: 1-12
  - Alto: 1-20
- **CardIds**: Todos los cardIds deben referenciar visualizaciones existentes (IdVisualizacion)

### Validaciones de Datos
- **Nombre**: Obligatorio, máximo 100 caracteres
- **Descripción**: Opcional, máximo 500 caracteres
- **Tema**: Opcional, máximo 50 caracteres

## Permisos Requeridos

- **Crear Dashboards**: `[RequirePermission("Crear Dashboards")]`
- **Ver Dashboards**: `[RequirePermission("Ver Dashboards")]`
- **Editar Dashboards**: `[RequirePermission("Editar Dashboards")]` (futuro)
- **Eliminar Dashboards**: `[RequirePermission("Eliminar Dashboards")]` (futuro)

## Relación Dashboard - Visualizaciones

Un dashboard es un contenedor que organiza visualizaciones existentes en un layout personalizable. Cada tarjeta en el dashboard es una referencia a una `Visualizacion` existente en el sistema.

**Flujo típico:**
1. El usuario crea visualizaciones individuales (gráficos, tablas, etc.)
2. El usuario crea un dashboard y selecciona qué visualizaciones incluir
3. El usuario configura el layout (posición, tamaño) de cada visualización en el dashboard
4. El dashboard se guarda con referencias a las visualizaciones y su configuración de layout

## Casos de Uso Comunes

### 1. Crear Dashboard Básico
```bash
curl -X POST "https://api.omnimonitor.com/api/dashboards" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "nombre": "Dashboard Básico",
    "descripcion": "Mi primer dashboard"
  }'
```

### 2. Crear Dashboard con Visualizaciones
```bash
curl -X POST "https://api.omnimonitor.com/api/dashboards" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "nombre": "Dashboard Completo",
    "descripcion": "Dashboard con múltiples visualizaciones",
    "layout": {
      "tarjetas": [
        {
          "cardId": 1,
          "posicionX": 0,
          "posicionY": 0,
          "ancho": 6,
          "alto": 4
        }
      ]
    }
  }'
```

### 3. Validar IdVisualizacion Antes de Crear
```bash
curl -X POST "https://api.omnimonitor.com/api/dashboards/validate-cards" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '[1, 2, 3]'
```

## Notas Técnicas

- Los dashboards son específicos por usuario (username)
- El layout se almacena como JSON en la base de datos
- Las tarjetas pueden tener propiedades personalizadas (props)
- El sistema valida automáticamente superposiciones y rangos
- Los cardIds deben referenciar visualizaciones existentes (IdVisualizacion)
- Las fechas se manejan en UTC
