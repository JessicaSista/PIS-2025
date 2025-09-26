# API de Datasets - Documentación

## Descripción General

El sistema de datasets permite a los usuarios crear y gestionar colecciones reutilizables de entidades que provienen de las APIs de SONDA. El sistema es modular y se adapta según el módulo de SONDA que se esté utilizando. Los datasets pueden ser creados manualmente por el usuario o generados automáticamente para visualizaciones de entidades sueltas.

## Arquitectura Modular

El sistema está diseñado para ser extensible y soportar diferentes módulos de SONDA:

### Módulos Soportados

- **IM (Infrastructure Management)**: Maneja devices, sources, sensores y grupos
- **AM (Asset Management)**: *Próximamente* - Manejará activos y recursos
- **UM (User Management)**: *Próximamente* - Manejará usuarios y roles

### Características Principales

#### Tipos de Datasets

1. **Dataset Manual (`es_dataset = 'S'`)**: Creado explícitamente por el usuario
2. **Dataset Interno (`es_dataset = 'N'`)**: Creado automáticamente para visualizaciones de entidades sueltas

#### Entidades por Módulo

**Módulo IM (Infrastructure Management):**
- **Devices**: Dispositivos individuales o grupos de dispositivos
- **Sources**: Fuentes de datos
- **Groups**: Grupos de dispositivos
- **Sensors**: Sensores específicos

**Módulo AM (Asset Management) - *Próximamente*:**
- **Assets**: Activos del sistema
- **Resources**: Recursos disponibles
- **Categories**: Categorías de activos

**Módulo UM (User Management) - *Próximamente*:**
- **Users**: Usuarios del sistema
- **Roles**: Roles de usuario
- **Permissions**: Permisos del sistema

## Endpoints de la API

### 1. Crear Dataset

```http
POST /api/dataset
Authorization: Bearer {token}
Content-Type: application/json

{
  "nombre": "Mi Dataset de Sensores",
  "descripcion": "Dataset para monitoreo de temperatura",
  "tipoEntidad": "device",
  "modulo": "IM",
  "grupoDevice": "grupo_temperatura",
  "idSource": 123,
  "idSensor": 456,
  "idDevices": [1, 2, 3, 4],
  "esDataset": "S"
}
```

**Respuesta:**
```json
{
  "id": 1,
  "nombre": "Mi Dataset de Sensores",
  "descripcion": "Dataset para monitoreo de temperatura",
  "esDataset": "S",
  "idUsuario": 1,
  "grupoDevice": "grupo_temperatura",
  "idSource": 123,
  "idGroup": null,
  "idSensor": 456,
  "tipoEntidad": "device",
  "modulo": "IM",
  "fechaCreacion": "2025-01-27T10:30:00Z",
  "fechaModificacion": "2025-01-27T10:30:00Z",
  "recordCount": 4,
  "devices": [
    {
      "id": 1,
      "name": "Device 1",
      "grupoDevice": "grupo_temperatura"
    }
  ]
}
```

### 2. Obtener Dataset por ID

```http
GET /api/dataset/{id}
Authorization: Bearer {token}
```

### 3. Listar Datasets

```http
GET /api/dataset?entityType=device&searchText=temperatura&page=1&pageSize=10&orderBy=nombre&orderDescending=false
Authorization: Bearer {token}
```

**Parámetros de consulta:**
- `entityType`: Filtrar por tipo de entidad
- `searchText`: Buscar en nombre y descripción
- `page`: Número de página (default: 1)
- `pageSize`: Tamaño de página (default: 10)
- `orderBy`: Campo para ordenar (nombre, fechaCreacion, fechaModificacion)
- `orderDescending`: Orden descendente (default: false)

### 4. Actualizar Dataset

```http
PUT /api/dataset/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": 1,
  "nombre": "Dataset Actualizado",
  "descripcion": "Nueva descripción",
  "grupoDevice": "nuevo_grupo",
  "idSource": 456,
  "idSensor": 789,
  "idDevices": [5, 6, 7]
}
```

### 5. Eliminar Dataset

```http
DELETE /api/dataset/{id}
Authorization: Bearer {token}
```

### 6. Validar Miembros del Dataset

```http
POST /api/dataset/validate
Authorization: Bearer {token}
Content-Type: application/json

{
  "tipoEntidad": "device",
  "idDevices": [1, 2, 3],
  "idSource": 123,
  "idSensor": 456
}
```

**Respuesta:**
```json
{
  "isValid": true,
  "errors": [],
  "invalidDeviceIds": [],
  "invalidSourceIds": [],
  "invalidGroupIds": [],
  "invalidSensorIds": []
}
```

### 7. Crear Dataset Interno

```http
POST /api/dataset/internal?tipoEntidad=device&entityId=123&sensorId=456
Authorization: Bearer {token}
```

## Reglas de Negocio

### Validaciones

1. **Nombre único por usuario**: No se pueden crear dos datasets con el mismo nombre para el mismo usuario
2. **Validación de entidades**: Los IDs de dispositivos, fuentes y grupos se validan contra las APIs de SONDA
3. **Sensor obligatorio**: Siempre se debe especificar un sensor
4. **Source o Group obligatorio**: Al menos uno de estos campos debe estar presente

### Comportamiento de Datasets

#### Dataset Manual (`es_dataset = 'S'`)
- El usuario selecciona dispositivos específicos
- Si no se seleccionan dispositivos, se obtienen todos los dispositivos que cumplan con el grupo, source y sensor
- Se procesa la lógica de grupos de dispositivos

#### Dataset Interno (`es_dataset = 'N'`)
- Se crea automáticamente para visualizaciones de entidades sueltas
- Contiene una sola entidad (device, source, group o sensor)
- Se lista directamente sin procesamiento adicional

## Códigos de Error

- **400 Bad Request**: Datos de entrada inválidos
- **401 Unauthorized**: Token de autenticación inválido o faltante
- **403 Forbidden**: Usuario sin permisos para la operación
- **404 Not Found**: Dataset no encontrado
- **409 Conflict**: Nombre de dataset duplicado
- **500 Internal Server Error**: Error interno del servidor

## Permisos Requeridos

- **Ver Datasets**: Permite listar y ver detalles de datasets
- **Gestionar Datasets**: Permite crear, actualizar y eliminar datasets

## Extensibilidad del Sistema

### Agregar Nuevos Módulos

Para agregar soporte para un nuevo módulo de SONDA (ej. AM, UM), seguir estos pasos:

1. **Crear el Validador del Módulo**:
   ```csharp
   public class DatasetAMValidator : IDatasetModuleValidator
   {
       public string ModuleName => "AM";
       public List<string> SupportedEntityTypes => new List<string> { "asset", "resource", "category" };
       
       // Implementar métodos de validación específicos del módulo AM
   }
   ```

2. **Registrar el Validador en Program.cs**:
   ```csharp
   builder.Services.AddScoped<IDatasetModuleValidator, DatasetAMValidator>();
   ```

3. **Actualizar DTOs si es necesario**:
   - Agregar nuevos campos específicos del módulo
   - Actualizar validaciones según las reglas del módulo

### Ventajas de la Arquitectura Modular

- **Separación de Responsabilidades**: Cada módulo maneja sus propias entidades
- **Fácil Extensión**: Agregar nuevos módulos sin afectar los existentes
- **Validación Específica**: Cada módulo valida según sus propias reglas
- **Mantenimiento Simplificado**: Cambios en un módulo no afectan otros

## Consideraciones de Implementación

1. **Migración de Base de Datos**: Se requiere ejecutar la migración para crear las tablas `Datasets` y `DeviceGrupos`
2. **Validación contra SONDA**: Los endpoints de validación requieren credenciales válidas de SONDA
3. **Paginación**: Todos los listados soportan paginación para manejar grandes volúmenes de datos
4. **Auditoría**: Se registran timestamps de creación y modificación para cada dataset
5. **Modularidad**: El sistema está preparado para crecer con nuevos módulos de SONDA
