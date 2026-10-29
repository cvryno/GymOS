# GymOS

GymOS es un sistema web de administración para gimnasios. En su estado actual es un MVP funcional orientado al personal administrativo: un frontend Angular consume una API ASP.NET Core, que guarda la información con Entity Framework Core y SQL Server.

> **Estado:** desarrollo/MVP. Aún requiere configuración, validación operativa y controles adicionales antes de usarse con datos reales o desplegarse en producción.

## Funciones disponibles

- Inicio de sesión administrativo con JWT.
- Gestión de socios, planes y membresías.
- Facturas y registro manual de pagos. Una membresía pendiente no se activa hasta registrar su pago completo.
- Registro y consulta de accesos de socios.
- Gestión de clases, instructores, inscripciones y lista de espera.
- Inventario de equipos y mantenimiento.
- Panel de resumen y calendario del gimnasio.
- Manejo de fechas con la zona horaria `America/Guayaquil`.
- Migraciones de base de datos aplicadas al iniciar la API cuando se usa SQL Server.
- Integración opcional preparada para Google y WhatsApp; necesita credenciales reales para funcionar.

## Tecnologías

- **Frontend:** Angular y TypeScript.
- **Backend:** .NET 10, ASP.NET Core y C#.
- **Persistencia:** Entity Framework Core y SQL Server.
- **Tareas programadas:** Quartz.NET.
- **Pruebas automatizadas:** xUnit.

## Requisitos

- .NET SDK 10.
- Node.js y npm compatibles con las dependencias del frontend (el proyecto declara npm `11.13.0`).
- SQL Server local o remoto accesible. La configuración inicial usa una instancia local con autenticación integrada de Windows.

## Configuración y ejecución local

### 1. Preparar la base de datos

Revisa `src/GymOS.API/appsettings.json` y ajusta `ConnectionStrings:DefaultConnection` si SQL Server no está en `localhost` o requiere otro método de autenticación. La base predeterminada se llama `GymOSDb`.

Al arrancar en modo Development, la API aplica las migraciones pendientes. El primer inicio necesita que el servidor SQL esté encendido y que el usuario tenga permisos para crear o actualizar la base.

### 2. Iniciar la API

Desde la carpeta raíz del repositorio, en PowerShell:

```powershell
dotnet restore GymOS.sln
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/GymOS.API/GymOS.API.csproj --urls http://localhost:5000
```

La API queda en `http://localhost:5000` y Swagger, disponible en Development, en `http://localhost:5000/swagger`.

La configuración del frontend apunta a `http://127.0.0.1:5000/api`. Si cambias el puerto o el host de la API, actualiza `src/GymOS.Frontend/src/environments/environment.ts` y verifica que el origen del frontend esté permitido por CORS.

### 3. Iniciar el frontend

Abre otra terminal:

```powershell
Set-Location src/GymOS.Frontend
npm ci
npm run start -- --host 127.0.0.1 --port 4200
```

Abre `http://127.0.0.1:4200` en el navegador. La API debe seguir ejecutándose en la otra terminal.

### Alternativa: base en memoria para desarrollo

Para probar la API sin SQL Server, puede seleccionarse el proveedor en memoria con la variable de entorno `Database__UseSqlServer=false` en la terminal donde se inicia la API. Los datos son temporales y se pierden al detener el proceso; no es una opción para producción.

## Acceso de demostración

En la configuración de desarrollo con SQL Server, la migración inicial incluye una cuenta administrativa de demostración:

- **Correo:** `admin@gymos.com`
- **Contraseña:** `Admin@1234`

Úsala únicamente en una instalación local de prueba. Cámbiala o elimina la cuenta antes de exponer el sistema a otros usuarios.

## Comprobaciones del proyecto

Desde la raíz:

```powershell
dotnet build GymOS.sln
dotnet test tests/GymOS.Tests/GymOS.Tests.csproj
```

Para compilar el frontend:

```powershell
Set-Location src/GymOS.Frontend
npm run build
```

## Estructura

```text
src/
  GymOS.API/             API, configuración y endpoints
  GymOS.Application/     Casos de uso y servicios de aplicación
  GymOS.Domain/          Entidades y reglas del dominio
  GymOS.Infrastructure/  Persistencia, migraciones e integraciones
  GymOS.Frontend/        Aplicación web Angular
tests/
  GymOS.Tests/           Pruebas automatizadas del backend
```

## Qué falta para acercarlo a un sistema comercial de cadena

Este repositorio todavía no debe considerarse un reemplazo de una plataforma como Smart Fit. Las brechas principales son:

1. **Cobros reales:** integrar una pasarela de pago, conciliación, reembolsos, renovaciones recurrentes y manejo seguro de eventos de pago.
2. **Autoservicio del socio:** portal o aplicación móvil para registro, reservas, pagos, estado de cuenta y actualización de datos.
3. **Control de acceso físico:** integración con torniquetes o lectores, reglas para membresías vencidas y operación ante cortes de red.
4. **Operación multi-sede:** sucursales, permisos por sede, transferencias de socios y reportes consolidados.
5. **Administración y seguridad:** roles y permisos granulares, recuperación de contraseña, auditoría de acciones, políticas de sesión y gestión de secretos.
6. **Producción confiable:** despliegue automatizado, HTTPS, copias de seguridad con restauración probada, monitoreo, alertas y registro centralizado.
7. **Calidad y cumplimiento:** más pruebas de integración y de interfaz, pruebas de carga, políticas de privacidad y retención de datos adecuadas al país de operación.
8. **Configuración de integraciones:** sustituir los valores de ejemplo de Google y WhatsApp por secretos administrados fuera del repositorio y validar los flujos de punta a punta.

## Configuración antes de producción

- Configura una clave JWT secreta y robusta mediante `Jwt__Key`; no guardes secretos en el repositorio.
- Usa una cadena de conexión protegida, HTTPS y una política de CORS limitada a los dominios reales.
- Reemplaza las credenciales y valores de ejemplo de Google/WhatsApp o desactiva esas integraciones.
- Cambia las credenciales de demostración y define roles/permisos apropiados.
- Configura copias de seguridad, monitoreo y un proceso de despliegue y actualización de base de datos.

Consulta `src/GymOS.API/appsettings.json` y los archivos `appsettings.*.json` para los valores de configuración del entorno. Nunca publiques claves o tokens reales en Git.
