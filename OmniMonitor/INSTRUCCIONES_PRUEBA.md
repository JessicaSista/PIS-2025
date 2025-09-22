# Instrucciones para Probar el Sistema de Roles y Permisos

## 🚀 Cómo Probar el Sistema

### 1. **Ejecutar la Aplicación**
```bash
cd OmniMonitor/Server
dotnet run
```

### 2. **Acceder a las Páginas de Prueba**
- Abre tu navegador y ve a: `https://localhost:7000` (o el puerto que uses)
- En el menú lateral, busca la sección "Sistema de Permisos"
- Haz clic en "Login" para iniciar sesión
- Haz clic en "Prueba Permisos" para probar el sistema

### 3. **Usuarios de Prueba Disponibles**

#### 👑 **Administrador**
- **Usuario:** `admin`
- **Contraseña:** `admin123`
- **Rol:** Administrador
- **Permisos:** Todos los permisos del sistema

#### 👤 **Visitante**
- **Usuario:** `visitante`
- **Contraseña:** `visitante123`
- **Rol:** Visitante
- **Permisos:** Solo permisos de lectura

### 4. **Funcionalidades de Prueba**

#### **Página de Login (`/login`)**
- Formulario simple para iniciar sesión
- Validación de credenciales
- Redirección automática a la página de prueba

#### **Página de Prueba (`/test-permissions`)**
- **Información del Usuario:** Muestra usuario, ID y roles
- **Permisos del Usuario:** Lista todos los permisos asignados
- **Verificar Permisos Específicos:** Botones para probar permisos individuales
- **Verificar Roles:** Botones para probar roles específicos
- **Pruebas de Endpoints:** Botones para probar endpoints protegidos

### 5. **Permisos Disponibles**

| Permiso | Descripción | Administrador | Visitante |
|---------|-------------|---------------|-----------|
| Ver Usuarios | Ver lista de usuarios | ✅ | ✅ |
| Crear Usuarios | Crear nuevos usuarios | ✅ | ❌ |
| Editar Usuarios | Editar usuarios existentes | ✅ | ❌ |
| Eliminar Usuarios | Eliminar usuarios | ✅ | ❌ |
| Ver Sensores | Ver datos de sensores | ✅ | ✅ |
| Configurar Sensores | Configurar sensores | ✅ | ❌ |
| Ver Empleados | Ver lista de empleados | ✅ | ✅ |
| Gestionar Empleados | Crear/editar/eliminar empleados | ✅ | ❌ |
| Ver Items | Ver lista de items | ✅ | ✅ |
| Gestionar Items | Crear/editar/eliminar items | ✅ | ❌ |

### 6. **Endpoints de Prueba**

#### **Endpoints Protegidos por Permisos:**
- `GET /api/employee` - Requiere permiso "Ver Empleados"
- `POST /api/employee` - Requiere permiso "Gestionar Empleados"
- `GET /api/authorization/roles` - Requiere permiso "Ver Usuarios"

#### **Endpoints de Autorización:**
- `GET /api/authorization/roles` - Lista todos los roles
- `GET /api/authorization/permissions` - Lista todos los permisos
- `GET /api/authorization/users/{id}/roles` - Roles de un usuario
- `GET /api/authorization/users/{id}/permissions` - Permisos de un usuario
- `GET /api/authorization/users/{id}/has-permission` - Verificar permiso específico
- `GET /api/authorization/users/{id}/has-role` - Verificar rol específico

### 7. **Cómo Probar Diferentes Escenarios**

#### **Escenario 1: Usuario Administrador**
1. Inicia sesión con `admin` / `admin123`
2. Ve a "Prueba Permisos"
3. Haz clic en "Verificar" para cada permiso
4. Todos deberían mostrar "SÍ" (verde)
5. Prueba los endpoints protegidos - deberían funcionar

#### **Escenario 2: Usuario Visitante**
1. Inicia sesión con `visitante` / `visitante123`
2. Ve a "Prueba Permisos"
3. Haz clic en "Verificar" para cada permiso
4. Solo los permisos de lectura deberían mostrar "SÍ"
5. Los endpoints de escritura deberían fallar

#### **Escenario 3: Sin Autenticación**
1. Ve directamente a `/test-permissions` sin iniciar sesión
2. Debería mostrar mensaje de "No hay usuario autenticado"

### 8. **Verificar en el Backend**

Puedes probar los endpoints directamente con herramientas como Postman o curl:

```bash
# Verificar permiso de un usuario
curl "https://localhost:7000/api/authorization/users/1/has-permission?permissionName=Ver%20Usuarios"

# Obtener roles de un usuario
curl "https://localhost:7000/api/authorization/users/1/roles"

# Probar endpoint protegido (debería fallar sin autenticación)
curl "https://localhost:7000/api/employee"
```

### 9. **Troubleshooting**

#### **Si no funciona el login:**
- Verifica que la base de datos esté actualizada
- Ejecuta las migraciones: `dotnet ef database update`
- Revisa los logs del servidor

#### **Si los permisos no se verifican correctamente:**
- Verifica que los usuarios tengan roles asignados
- Revisa que los roles tengan permisos asignados
- Comprueba los logs del servidor para errores

#### **Si los endpoints fallan:**
- Verifica que el usuario esté autenticado
- Comprueba que el usuario tenga el permiso requerido
- Revisa los logs del servidor

### 10. **Estructura del Sistema**

```
Backend (Solo)
├── Entidades: User, Role, Permission, UserRole, RolePermission
├── Servicios: AuthorizationService, AuthService
├── Atributos: RequirePermissionAttribute, RequireRoleAttribute
├── Controladores: AuthController, AuthorizationController, EmployeeController
└── Context: ApplicationDbContext con datos iniciales

Frontend (Solo para Pruebas)
├── Páginas: Login.razor, TestPermissions.razor
└── Navegación: Enlaces en NavMenu.razor
```

¡El sistema está listo para probar! 🎉
