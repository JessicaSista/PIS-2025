# Tests del Módulo EM (Emergency Management)

Este conjunto de tests está diseñado para verificar que el backend del módulo EM entrega 4 endpoints simples que devuelvan los datos correctamente, validando que:

1. **Validación**: Si un parámetro es inválido, responde 400 con un mensaje entendible
2. **Permisos**: Usuarios sin permiso reciben 403; no se exponen datos de otros tenants/ubicaciones

## 📁 Estructura de Tests

### 1. `DatasetEMControllerTests.cs`
**Tests Unitarios del Controller**
- ✅ Verificación de todos los endpoints HTTP (POST, GET, PUT, DELETE)
- ✅ Validaciones de entrada (400 Bad Request)
- ✅ Manejo de errores de servicio (500 Internal Server Error)
- ✅ Respuestas correctas (200, 201, 204, 404)

**Endpoints Testeados:**
- `POST /api/DatasetEM` - Crear dataset
- `GET /api/DatasetEM/user/{username}` - Obtener todos los datasets
- `GET /api/DatasetEM/{datasetId}/{username}` - Obtener dataset por ID
- `PUT /api/DatasetEM/{datasetId}` - Actualizar dataset
- `DELETE /api/DatasetEM/{datasetId}/{username}` - Eliminar dataset

### 2. `DatasetEMServiceTests.cs`
**Tests Unitarios del Service**
- ✅ Lógica de negocio y validaciones
- ✅ Búsqueda dinámica para datasets formales (Is_Dataset = "S")
- ✅ Manejo de datasets individuales (Is_Dataset = "N")
- ✅ Integración con SondaEMService (mock)
- ✅ Operaciones CRUD con base de datos (in-memory)

**Funcionalidades Testeadas:**
- Creación de datasets con diferentes tipos de contenido (Alerts, Events, Extensions, Resources)
- Validación de nombres únicos por usuario
- Búsqueda dinámica que llama a la API externa
- Filtrado correcto por usuario

### 3. `SondaEMServiceTests.cs`
**Tests Unitarios del Service de API Externa**
- ✅ Comunicación correcta con la API externa EM
- ✅ Manejo de errores HTTP (400, 401, 403, 404, 500)
- ✅ Autenticación y autorización
- ✅ Serialización/deserialización de datos
- ✅ Validación de parámetros de consulta

**APIs Testeadas:**
- `GetAlerts()` - Obtener alertas con filtros
- `GetAlertById()` - Obtener alerta específica
- `GetEvents()` - Obtener eventos
- `GetEventById()` - Obtener evento específico
- `GetExtensions()` - Obtener extensiones
- `GetExtensionById()` - Obtener extensión específica
- `GetResourceById()` - Obtener recurso específico
- `GetEventTypes()` - Obtener tipos de eventos

### 4. `DatasetEMIntegrationTests.cs`
**Tests de Integración Completos**
- ✅ Flujo completo: Controller → Service → SondaService → API Externa
- ✅ Validaciones en cada capa
- ✅ Manejo de errores a lo largo del pipeline
- ✅ Comportamiento real del sistema

**Flujos Testeados:**
- Crear dataset individual con entidades específicas
- Crear dataset formal con búsqueda dinámica
- Actualizar datasets con cambio de tipo de contenido
- Eliminar datasets
- Manejar errores de API externa

### 5. `DatasetEMAuthorizationTests.cs`
**Tests de Autorización y Seguridad**
- ✅ Control de permisos (403 Forbidden)
- ✅ Separación de datos por usuario/tenant
- ✅ Validaciones de parámetros (400 Bad Request)
- ✅ Manejo de errores de servicio (500 Internal Server Error)

**Casos de Seguridad Testeados:**
- Usuarios sin permisos correctos no pueden acceder
- Usuarios solo ven/modifican sus propios datasets
- Parámetros inválidos retornan errores claros
- Errores internos se manejan apropiadamente

## 🚀 Cómo Ejecutar los Tests

### Prerrequisitos
```bash
# Asegúrate de tener las dependencias necesarias
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
```

### Ejecutar Tests Individuales
```bash
# Tests del Controller
dotnet test --filter "DatasetEMControllerTests"

# Tests del Service
dotnet test --filter "DatasetEMServiceTests"

# Tests del SondaService
dotnet test --filter "SondaEMServiceTests"

# Tests de Integración
dotnet test --filter "DatasetEMIntegrationTests"

# Tests de Autorización
dotnet test --filter "DatasetEMAuthorizationTests"
```

### Ejecutar Todos los Tests del Módulo EM
```bash
dotnet test --filter "DatasetEM"
```

### Ejecutar con Cobertura
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Verificaciones Realizadas

### ✅ Validaciones de Entrada (400 Bad Request)
- Parámetros nulos o vacíos
- IDs inválidos (≤ 0)
- Nombres duplicados
- Tipos de datos incorrectos
- Requests malformados

### ✅ Control de Permisos (403 Forbidden)
- Atributos `[RequirePermission]` en endpoints
- Separación de datos por usuario
- No exposición de datos de otros tenants

### ✅ Respuestas Correctas
- **200 OK**: Datos recuperados correctamente
- **201 Created**: Dataset creado exitosamente
- **204 No Content**: Dataset eliminado
- **404 Not Found**: Recurso no encontrado
- **400 Bad Request**: Parámetros inválidos
- **500 Internal Server Error**: Errores del sistema

### ✅ Integración con API Externa
- Autenticación correcta con tokens
- Manejo de respuestas de la API EM
- Filtrado y búsqueda dinámica
- Manejo de errores de conectividad

## 🎯 Casos de Test Específicos

### Datasets Formales (Is_Dataset = "S")
```csharp
// Búsqueda dinámica que consulta la API externa
var dataset = new DatasetEM {
    Is_Dataset = "S",
    AlertState = "Active",
    Id_Alert = 1
};
// Al obtener el dataset, debe llamar a GetAlerts() y filtrar
```

### Datasets Individuales (Is_Dataset = "N")
```csharp
// Entidades específicas seleccionadas
var dataset = new DatasetEM {
    Is_Dataset = "N",
    ContentType = "1", // Alerts
    DatasetAlerts = { new DatasetAlert { Id_alert = 1 } }
};
```

### Tipos de Contenido
- **ContentType = "0"**: Dataset formal
- **ContentType = "1"**: Alerts
- **ContentType = "2"**: Events  
- **ContentType = "3"**: Extensions
- **ContentType = "4"**: Resources

## 🔍 Debugging y Troubleshooting

### Ver Logs de Tests
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Tests Específicos por Nombre
```bash
dotnet test --filter "FullMethodName~CreateDataset_ValidRequest_Returns201Created"
```

### Verificar Cobertura de Código
```bash
# Generar reporte de cobertura
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Ver reporte (requiere reportgenerator)
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./TestResults/Coverage" -reporttypes:Html
```

## 📋 Checklist de Verificación

Antes de considerar que el módulo EM está correctamente testeado, verifica que:

- [ ] Todos los endpoints HTTP funcionan correctamente
- [ ] Las validaciones de entrada retornan 400 con mensajes claros
- [ ] Los permisos se verifican y usuarios sin permiso reciben 403
- [ ] Los datos están separados por usuario (no se exponen datos de otros)
- [ ] La búsqueda dinámica funciona para datasets formales
- [ ] La selección específica funciona para datasets individuales
- [ ] Los errores de la API externa se manejan apropiadamente
- [ ] Las operaciones CRUD completas funcionan
- [ ] Los tests de integración pasan sin errores
- [ ] La cobertura de código es alta (>80%)

## 🚨 Notas Importantes

1. **Base de Datos**: Los tests usan `UseInMemoryDatabase()` para evitar dependencias externas
2. **API Externa**: Se usa Moq para simular las respuestas de la API EM
3. **Autenticación**: Se simula con `ClaimsPrincipal` mock
4. **Permisos**: Los atributos `[RequirePermission]` se prueban indirectamente

Los tests están listos para ejecutar y verificar que el módulo EM cumple con todos los requisitos funcionales y de seguridad.